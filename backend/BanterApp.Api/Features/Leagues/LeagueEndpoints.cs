using System.Security.Cryptography;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Leagues;

public static class LeagueEndpoints
{
    public static IEndpointRouteBuilder MapLeagueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leagues").WithTags("Leagues");

        group.MapGet("/", GetMyLeagues);
        group.MapGet("/preview", GetLeaguePreview);
        group.MapPost("/create", CreateLeague)
            .RequireRateLimiting("write")
            .WithValidation<CreateLeagueRequest>();
        group.MapPost("/join", JoinLeague)
            .RequireRateLimiting("write")
            .WithValidation<JoinLeagueRequest>();
        group.MapGet("/standings", GetLeagueStandings);

        return app;
    }

    /// <summary>
    /// Creating a league requires an onboarded session — registered, or a guest
    /// who accepted terms (and therefore holds a session key). No signup needed.
    /// </summary>
    private static async Task<IResult> CreateLeague(
        CreateLeagueRequest request,
        AppDbContext db,
        IUserContext user,
        HttpContext http,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        var (customUsed, totalUsed) = await SystemLeagueService.CountMembershipsAsync(db, user, ct);
        if (customUsed >= League.MaxCustomLeaguesPerUser)
        {
            return Results.Conflict(new
            {
                error = $"You can only belong to {League.MaxCustomLeaguesPerUser} custom leagues " +
                        "(family, office, friends). Leave one to create another."
            });
        }

        if (totalUsed >= League.MaxTotalLeagueMemberships)
        {
            return Results.Conflict(new
            {
                error = $"League limit reached ({League.MaxTotalLeagueMemberships} max including Global and Country)."
            });
        }

        var leagueName = NormalizeLeagueName(request.Name);
        if (ProfanityFilter.ContainsProfanity(leagueName))
        {
            return Results.BadRequest(new { error = "League name contains language we can't allow on a family-friendly site." });
        }

        var displayName = LeagueDisplayNameResolver.EnsureUniqueInLeague(
            await LeagueDisplayNameResolver.ResolveAsync(db, user, ct),
            []);

        var league = new League
        {
            Id = Guid.NewGuid(),
            Name = leagueName,
            InviteCode = GenerateInviteCode(),
            CreatedByUserId = user.UserId,
            CreatedByAnonymousUserId = user.IsAuthenticated ? null : user.AnonymousUserId,
            MaxMembers = League.DefaultMaxMembers
        };

        db.Leagues.Add(league);
        db.LeagueMembers.Add(new LeagueMember
        {
            Id = Guid.NewGuid(),
            LeagueId = league.Id,
            UserId = user.UserId,
            AnonymousUserId = user.IsAuthenticated ? null : user.AnonymousUserId,
            DisplayName = displayName,
            IsAdmin = true
        });

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/leagues/standings?leagueId={league.Id}", new LeagueResponse(
            league.Id, league.Name, league.InviteCode, 1, league.MaxMembers, league.CreatedAt, displayName));
    }

    private static async Task<IResult> JoinLeague(
        JoinLeagueRequest request,
        AppDbContext db,
        IUserContext user,
        HttpContext http,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        var inviteCode = request.InviteCode.Trim().ToUpperInvariant();
        var league = await db.Leagues
            .FirstOrDefaultAsync(l => l.InviteCode == inviteCode, ct);

        if (league is null)
        {
            return Results.NotFound(new { error = "League not found — check the invite link." });
        }

        if (league.Kind != LeagueKind.Custom)
        {
            return Results.BadRequest(new { error = "System leagues (Global / Country) are joined automatically." });
        }

        var (customUsed, totalUsed) = await SystemLeagueService.CountMembershipsAsync(db, user, ct);
        if (customUsed >= League.MaxCustomLeaguesPerUser)
        {
            return Results.Conflict(new
            {
                error = $"You can only belong to {League.MaxCustomLeaguesPerUser} custom leagues. Leave one to join another."
            });
        }

        if (totalUsed >= League.MaxTotalLeagueMemberships)
        {
            return Results.Conflict(new
            {
                error = $"League limit reached ({League.MaxTotalLeagueMemberships} max including Global and Country)."
            });
        }

        var members = await db.LeagueMembers
            .Where(m => m.LeagueId == league.Id)
            .ToListAsync(ct);

        var alreadyMember = members.Any(m => user.IsAuthenticated
            ? m.UserId == user.UserId
            : m.AnonymousUserId == user.AnonymousUserId);

        if (alreadyMember)
        {
            return Results.Conflict(new { error = "You are already in this league." });
        }

        if (members.Count >= league.MaxMembers)
        {
            return Results.Conflict(new { error = $"This league is full ({league.MaxMembers} players max)." });
        }

        var displayName = LeagueDisplayNameResolver.EnsureUniqueInLeague(
            await LeagueDisplayNameResolver.ResolveAsync(db, user, ct),
            members.Select(m => m.DisplayName));

        db.LeagueMembers.Add(new LeagueMember
        {
            Id = Guid.NewGuid(),
            LeagueId = league.Id,
            UserId = user.UserId,
            AnonymousUserId = user.IsAuthenticated ? null : user.AnonymousUserId,
            DisplayName = displayName,
            IsAdmin = false
        });

        await db.SaveChangesAsync(ct);

        return Results.Ok(new LeagueResponse(
            league.Id, league.Name, league.InviteCode, members.Count + 1, league.MaxMembers, league.CreatedAt, displayName));
    }

    /// <summary>Leagues for the current session — global + country always; custom only if a member.</summary>
    private static async Task<IResult> GetMyLeagues(
        string? countryCode,
        HttpContext http,
        AppDbContext db,
        IUserContext user,
        TournamentBonusScoringService bonusScoring,
        CancellationToken ct)
    {
        var resolvedCountry = countryCode;
        if (string.IsNullOrWhiteSpace(resolvedCountry))
        {
            resolvedCountry = http.Request.Headers["X-Country-Code"].FirstOrDefault();
        }

        var normalizedCountry = SystemLeagueService.NormalizeCountryCode(resolvedCountry);
        await SystemLeagueService.EnsureSystemLeagueRowsAsync(db, normalizedCountry, ct);

        var hasSession = user.IsAuthenticated || user.IsAnonymous;
        if (hasSession)
        {
            await SystemLeagueService.EnsureSystemLeaguesAsync(db, user, normalizedCountry, ct);
        }

        await db.SaveChangesAsync(ct);

        if (!hasSession)
        {
            var guestLeagues = await BuildGuestSystemLeaguesAsync(db, normalizedCountry, ct);
            return Results.Ok(new MyLeaguesResponse(guestLeagues, SystemLeagueService.BuildLimits(0, 0)));
        }

        var memberships = await db.LeagueMembers
            .Where(m => user.IsAuthenticated
                ? m.UserId == user.UserId
                : m.AnonymousUserId == user.AnonymousUserId)
            .Include(m => m.League)
            .ToListAsync(ct);

        var (customUsed, totalUsed) = await SystemLeagueService.CountMembershipsAsync(db, user, ct);
        var limits = SystemLeagueService.BuildLimits(customUsed, totalUsed);

        if (memberships.Count == 0)
        {
            var fallback = await BuildGuestSystemLeaguesAsync(db, normalizedCountry, ct);
            return Results.Ok(new MyLeaguesResponse(fallback, limits));
        }

        var leagueIds = memberships.Select(m => m.LeagueId).ToList();
        var memberCounts = await db.LeagueMembers
            .Where(m => leagueIds.Contains(m.LeagueId))
            .GroupBy(m => m.LeagueId)
            .Select(g => new { LeagueId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var countMap = memberCounts.ToDictionary(x => x.LeagueId, x => x.Count);

        var matchPoints = await GetIdentityMatchPointsAsync(db, user, ct);
        var bonusPoints = await bonusScoring.GetBonusPointsAsync(db, user, ct);

        var response = memberships
            .OrderBy(m => KindSortOrder(m.League.Kind))
            .ThenByDescending(m => m.JoinedAt)
            .Select(m =>
            {
                var memberCount = countMap.GetValueOrDefault(m.LeagueId, 1);
                var leaguePoints = matchPoints;
                if (TournamentBonusScoringService.BonusPointsApplyToLeague(m.League, memberCount))
                {
                    leaguePoints += bonusPoints;
                }

                return new MyLeagueResponse(
                    m.League.Id,
                    m.League.Name,
                    m.League.InviteCode,
                    memberCount,
                    m.League.MaxMembers,
                    m.IsAdmin,
                    m.DisplayName,
                    leaguePoints,
                    m.League.CreatedAt,
                    m.League.Kind.ToString().ToLowerInvariant(),
                    m.League.CountryCode,
                    TournamentBonusScoringService.BonusPointsApplyToLeague(m.League, memberCount));
            })
            .ToList();

        return Results.Ok(new MyLeaguesResponse(response, limits));
    }

    private static async Task<List<MyLeagueResponse>> BuildGuestSystemLeaguesAsync(
        AppDbContext db,
        string countryCode,
        CancellationToken ct)
    {
        var global = await db.Leagues.FindAsync([League.GlobalLeagueId], ct);
        var country = await db.Leagues
            .FirstOrDefaultAsync(l => l.Kind == LeagueKind.Country && l.CountryCode == countryCode, ct);

        var leagueIds = new List<Guid>();
        if (global is not null)
        {
            leagueIds.Add(global.Id);
        }

        if (country is not null)
        {
            leagueIds.Add(country.Id);
        }

        var memberCounts = leagueIds.Count == 0
            ? []
            : await db.LeagueMembers
                .Where(m => leagueIds.Contains(m.LeagueId))
                .GroupBy(m => m.LeagueId)
                .Select(g => new { LeagueId = g.Key, Count = g.Count() })
                .ToListAsync(ct);
        var countMap = memberCounts.ToDictionary(x => x.LeagueId, x => x.Count);

        var response = new List<MyLeagueResponse>();
        if (global is not null)
        {
            response.Add(ToGuestLeagueResponse(global, countMap.GetValueOrDefault(global.Id, 0)));
        }

        if (country is not null)
        {
            response.Add(ToGuestLeagueResponse(country, countMap.GetValueOrDefault(country.Id, 0)));
        }

        return response;
    }

    private static MyLeagueResponse ToGuestLeagueResponse(League league, int memberCount) =>
        new(
            league.Id,
            league.Name,
            league.InviteCode,
            memberCount,
            league.MaxMembers,
            IsAdmin: false,
            MyDisplayName: string.Empty,
            MyPoints: 0,
            league.CreatedAt,
            league.Kind.ToString().ToLowerInvariant(),
            league.CountryCode,
            BonusPointsEnabled: false);

    private static int KindSortOrder(LeagueKind kind) => kind switch
    {
        LeagueKind.Global => 0,
        LeagueKind.Country => 1,
        _ => 2
    };

    /// <summary>Invite-link preview — custom leagues only; requires the secret invite code.</summary>
    private static async Task<IResult> GetLeaguePreview(
        string? inviteCode,
        AppDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
        {
            return Results.BadRequest(new { error = "inviteCode query parameter is required." });
        }

        var code = inviteCode.Trim().ToUpperInvariant();
        var league = await db.Leagues.FirstOrDefaultAsync(l => l.InviteCode == code, ct);
        if (league is null || league.Kind != LeagueKind.Custom)
        {
            return Results.NotFound(new { error = "League not found — check the invite link." });
        }

        var memberCount = await db.LeagueMembers.CountAsync(m => m.LeagueId == league.Id, ct);

        return Results.Ok(new LeaguePreviewResponse(
            league.Id, league.Name, league.InviteCode, memberCount, league.MaxMembers,
            memberCount >= league.MaxMembers));
    }

    private static async Task<IResult> GetLeagueStandings(
        Guid? leagueId,
        string? inviteCode,
        AppDbContext db,
        IUserContext user,
        TournamentBonusScoringService bonusScoring,
        CancellationToken ct)
    {
        League? league = null;
        if (leagueId.HasValue)
        {
            league = await db.Leagues.FindAsync([leagueId.Value], ct);
        }
        else if (!string.IsNullOrWhiteSpace(inviteCode))
        {
            league = await db.Leagues
                .FirstOrDefaultAsync(l => l.InviteCode == inviteCode.Trim().ToUpperInvariant(), ct);
        }

        if (league is null)
        {
            return Results.BadRequest(new { error = "Provide leagueId or inviteCode query parameter." });
        }

        var access = await LeagueAccessGuard.RequireCustomLeagueMemberAsync(db, league, user, ct);
        if (access is not null)
        {
            return access;
        }

        await bonusScoring.LockPicksIfNeededAsync(db, ct);
        await bonusScoring.RescoreAllPicksAsync(db, ct);

        var standings = await BuildStandingsAsync(db, league, bonusScoring, ct);
        return Results.Ok(new LeagueStandingsResponse(league.Id, league.Name, standings, league.Kind == LeagueKind.Custom));
    }

    /// <summary>Standings across both registered and guest members, by league display name.</summary>
    public static async Task<IReadOnlyList<LeagueStandingEntry>> BuildStandingsAsync(
        AppDbContext db,
        League league,
        TournamentBonusScoringService bonusScoring,
        CancellationToken ct)
    {
        var members = await db.LeagueMembers
            .Where(m => m.LeagueId == league.Id)
            .ToListAsync(ct);

        var includeBonus = TournamentBonusScoringService.BonusPointsApplyToLeague(league, members.Count);

        var userIds = members.Where(m => m.UserId.HasValue).Select(m => m.UserId!.Value).ToList();
        var anonIds = members.Where(m => m.AnonymousUserId.HasValue).Select(m => m.AnonymousUserId!.Value).ToList();

        var userPoints = await db.Predictions
            .Where(p => p.UserId.HasValue && userIds.Contains(p.UserId.Value))
            .GroupBy(p => p.UserId!.Value)
            .Select(g => new { Id = g.Key, Points = g.Sum(p => p.PointsAwarded), Count = g.Count() })
            .ToListAsync(ct);

        var anonPoints = await db.Predictions
            .Where(p => p.AnonymousUserId.HasValue && anonIds.Contains(p.AnonymousUserId.Value))
            .GroupBy(p => p.AnonymousUserId!.Value)
            .Select(g => new { Id = g.Key, Points = g.Sum(p => p.PointsAwarded), Count = g.Count() })
            .ToListAsync(ct);

        var userMap = userPoints.ToDictionary(x => x.Id);
        var anonMap = anonPoints.ToDictionary(x => x.Id);

        Dictionary<Guid, int> bonusMap = includeBonus
            ? await bonusScoring.GetBonusPointsByIdentityAsync(db, members, ct)
            : [];

        return members
            .Select(m =>
            {
                var stats = m.UserId.HasValue
                    ? userMap.GetValueOrDefault(m.UserId.Value)
                    : m.AnonymousUserId.HasValue
                        ? anonMap.GetValueOrDefault(m.AnonymousUserId.Value)
                        : null;

                var identityId = m.UserId ?? m.AnonymousUserId;
                var matchPoints = stats?.Points ?? 0;
                var bonus = identityId.HasValue && includeBonus
                    ? bonusMap.GetValueOrDefault(identityId.Value)
                    : 0;

                return new LeagueStandingEntry(
                    identityId,
                    m.DisplayName,
                    matchPoints + bonus,
                    stats?.Count ?? 0,
                    bonus);
            })
            .OrderByDescending(s => s.TotalPoints)
            .ThenBy(s => s.DisplayName)
            .ToList();
    }

    private static async Task<int> GetIdentityMatchPointsAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        var query = user.IsAuthenticated
            ? db.Predictions.Where(p => p.UserId == user.UserId)
            : db.Predictions.Where(p => p.AnonymousUserId == user.AnonymousUserId);

        return await query.SumAsync(p => p.PointsAwarded, ct);
    }

    private static string NormalizeLeagueName(string name)
    {
        var trimmed = string.Join(
            ' ',
            name.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return StringLimits.Truncate(trimmed, StringLimits.LeagueName) ?? trimmed;
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        return string.Create(8, bytes.ToArray(), (span, b) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = chars[b[i] % chars.Length];
            }
        });
    }
}
