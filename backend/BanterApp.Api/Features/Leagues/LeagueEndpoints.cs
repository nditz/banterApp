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

        group.MapPost("/create", CreateLeague)
            .RequireAuthorization()
            .WithValidation<CreateLeagueRequest>();
        group.MapPost("/join", JoinLeague)
            .RequireAuthorization()
            .WithValidation<JoinLeagueRequest>();
        group.MapGet("/standings", GetLeagueStandings);

        return app;
    }

    private static async Task<IResult> CreateLeague(
        CreateLeagueRequest request,
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        if (!user.UserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var userId = user.UserId.Value;
        var userEntity = await db.Users.FindAsync([userId], ct);
        if (userEntity is null)
        {
            userEntity = new User
            {
                Id = userId,
                Email = $"user-{userId:N}@banter.local",
                DisplayName = $"Player {userId.ToString()[..8]}"
            };
            db.Users.Add(userEntity);
        }

        var league = new League
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            InviteCode = GenerateInviteCode(),
            CreatedByUserId = userId
        };

        db.Leagues.Add(league);
        db.LeagueMembers.Add(new LeagueMember
        {
            LeagueId = league.Id,
            UserId = userId
        });

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/leagues/standings?leagueId={league.Id}", new LeagueResponse(
            league.Id, league.Name, league.InviteCode, league.CreatedByUserId, league.CreatedAt));
    }

    private static async Task<IResult> JoinLeague(
        JoinLeagueRequest request,
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        if (!user.UserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var userId = user.UserId.Value;
        var league = await db.Leagues
            .FirstOrDefaultAsync(l => l.InviteCode == request.InviteCode.Trim().ToUpperInvariant(), ct);

        if (league is null)
        {
            return Results.NotFound(new { error = "League not found." });
        }

        var alreadyMember = await db.LeagueMembers
            .AnyAsync(m => m.LeagueId == league.Id && m.UserId == userId, ct);

        if (alreadyMember)
        {
            return Results.Conflict(new { error = "Already a member of this league." });
        }

        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
        {
            db.Users.Add(new User
            {
                Id = userId,
                Email = $"user-{userId:N}@banter.local",
                DisplayName = $"Player {userId.ToString()[..8]}"
            });
        }

        db.LeagueMembers.Add(new LeagueMember { LeagueId = league.Id, UserId = userId });
        await db.SaveChangesAsync(ct);

        return Results.Ok(new LeagueResponse(league.Id, league.Name, league.InviteCode, league.CreatedByUserId, league.CreatedAt));
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

        var memberIds = await db.LeagueMembers
            .Where(m => m.LeagueId == league.Id)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        var standings = await db.Users
            .Where(u => memberIds.Contains(u.Id))
            .Select(u => new LeagueStandingEntry(
                u.Id,
                u.DisplayName,
                u.Predictions.Sum(p => p.PointsAwarded),
                u.Predictions.Count))
            .OrderByDescending(s => s.TotalPoints)
            .ThenBy(s => s.DisplayName)
            .ToListAsync(ct);

        return Results.Ok(new LeagueStandingsResponse(league.Id, league.Name, standings));
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
