using System.Security.Cryptography;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
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

        var league = new League
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
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
            DisplayName = request.DisplayName.Trim(),
            IsAdmin = true
        });

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/leagues/standings?leagueId={league.Id}", new LeagueResponse(
            league.Id, league.Name, league.InviteCode, 1, league.MaxMembers, league.CreatedAt));
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

        var displayName = request.DisplayName.Trim();
        if (members.Any(m => string.Equals(m.DisplayName, displayName, StringComparison.OrdinalIgnoreCase)))
        {
            return Results.Conflict(new { error = "That name is already taken in this league — pick another." });
        }

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
            league.Id, league.Name, league.InviteCode, members.Count + 1, league.MaxMembers, league.CreatedAt));
    }

    /// <summary>Leagues the current session (registered or guest) belongs to.</summary>
    private static async Task<IResult> GetMyLeagues(
        string? countryCode,
        HttpContext http,
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return Results.Ok(new MyLeaguesResponse([], SystemLeagueService.BuildLimits(0, 0)));
        }

        var resolvedCountry = countryCode;
        if (string.IsNullOrWhiteSpace(resolvedCountry))
        {
            resolvedCountry = http.Request.Headers["X-Country-Code"].FirstOrDefault();
        }

        await SystemLeagueService.EnsureSystemLeaguesAsync(db, user, resolvedCountry, ct);
        await db.SaveChangesAsync(ct);

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
            return Results.Ok(new MyLeaguesResponse([], limits));
        }

        var leagueIds = memberships.Select(m => m.LeagueId).ToList();
        var memberCounts = await db.LeagueMembers
            .Where(m => leagueIds.Contains(m.LeagueId))
            .GroupBy(m => m.LeagueId)
            .Select(g => new { LeagueId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var countMap = memberCounts.ToDictionary(x => x.LeagueId, x => x.Count);

        var myPoints = await GetIdentityPointsAsync(db, user, ct);

        var response = memberships
            .OrderBy(m => KindSortOrder(m.League.Kind))
            .ThenByDescending(m => m.JoinedAt)
            .Select(m => new MyLeagueResponse(
                m.League.Id,
                m.League.Name,
                m.League.InviteCode,
                countMap.GetValueOrDefault(m.LeagueId, 1),
                m.League.MaxMembers,
                m.IsAdmin,
                m.DisplayName,
                myPoints,
                m.League.CreatedAt,
                m.League.Kind.ToString().ToLowerInvariant(),
                m.League.CountryCode))
            .ToList();

        return Results.Ok(new MyLeaguesResponse(response, limits));
    }

    private static int KindSortOrder(LeagueKind kind) => kind switch
    {
        LeagueKind.Global => 0,
        LeagueKind.Country => 1,
        _ => 2
    };

    /// <summary>Public preview used by the invite-link landing page.</summary>
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
        if (league is null)
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

        var standings = await BuildStandingsAsync(db, league.Id, ct);
        return Results.Ok(new LeagueStandingsResponse(league.Id, league.Name, standings));
    }

    /// <summary>Standings across both registered and guest members, by league display name.</summary>
    public static async Task<IReadOnlyList<LeagueStandingEntry>> BuildStandingsAsync(
        AppDbContext db,
        Guid leagueId,
        CancellationToken ct)
    {
        var members = await db.LeagueMembers
            .Where(m => m.LeagueId == leagueId)
            .ToListAsync(ct);

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

        return members
            .Select(m =>
            {
                var stats = m.UserId.HasValue
                    ? userMap.GetValueOrDefault(m.UserId.Value)
                    : m.AnonymousUserId.HasValue
                        ? anonMap.GetValueOrDefault(m.AnonymousUserId.Value)
                        : null;

                return new LeagueStandingEntry(
                    m.UserId ?? m.AnonymousUserId,
                    m.DisplayName,
                    stats?.Points ?? 0,
                    stats?.Count ?? 0);
            })
            .OrderByDescending(s => s.TotalPoints)
            .ThenBy(s => s.DisplayName)
            .ToList();
    }

    private static async Task<int> GetIdentityPointsAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        var query = user.IsAuthenticated
            ? db.Predictions.Where(p => p.UserId == user.UserId)
            : db.Predictions.Where(p => p.AnonymousUserId == user.AnonymousUserId);

        return await query.SumAsync(p => p.PointsAwarded, ct);
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
