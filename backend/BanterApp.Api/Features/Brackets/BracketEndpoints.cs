using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Brackets;

public static class BracketEndpoints
{
    public static IEndpointRouteBuilder MapBracketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/brackets").WithTags("Brackets");

        group.MapGet("/template", GetBracketTemplate);
        group.MapGet("/mine", GetMyBracket).RequireRateLimiting("api");
        group.MapPut("/pick", SaveBracketPick)
            .RequireRateLimiting("write")
            .WithValidation<SaveBracketPickRequest>();

        return app;
    }

    private static async Task<IResult> GetBracketTemplate(AppDbContext db, CancellationToken ct)
    {
        var (groupMatches, matchMap) = await LoadBracketMatchesAsync(db, ct);
        var state = BuildState(groupMatches, matchMap, new Dictionary<string, string>());
        return Results.Ok(state);
    }

    /// <summary>Pick value used when a quick prediction was a draw (no single winner).</summary>
    public const string DrawPick = "DRAW";

    private static async Task<IResult> GetMyBracket(
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

        var picks = await LoadPicks(db, user, ct);
        var pickMap = picks.ToDictionary(p => p.SlotId, p => p.WinnerTeamCode, StringComparer.OrdinalIgnoreCase);
        var (groupMatches, matchMap) = await LoadBracketMatchesAsync(db, ct);
        await MergePredictionPicksAsync(db, user, groupMatches, pickMap, ct);
        return Results.Ok(BuildState(groupMatches, matchMap, pickMap, picks));
    }

    private static async Task<IResult> SaveBracketPick(
        SaveBracketPickRequest request,
        AppDbContext db,
        IUserContext user,
        HttpContext http,
        TurnstileService turnstile,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (!await turnstile.VerifyAsync(request.TurnstileToken, ip, ct))
        {
            return Results.BadRequest(new { error = "Human verification failed." });
        }

        var (groupMatches, matchMap) = await LoadBracketMatchesAsync(db, ct);
        if (!BracketEngine.TryGetSlot(request.SlotId, groupMatches, out var slot))
        {
            return Results.BadRequest(new { error = "Unknown bracket slot." });
        }

        if (!matchMap.TryGetValue(slot.MatchId, out var match))
        {
            return Results.NotFound(new { error = "Match not found." });
        }

        if (MatchLockService.IsLocked(match))
        {
            return Results.BadRequest(new { error = MatchLockService.LockReason(match) });
        }

        var existingPicks = await LoadPicks(db, user, ct);
        var pickMap = existingPicks.ToDictionary(p => p.SlotId, p => p.WinnerTeamCode, StringComparer.OrdinalIgnoreCase);
        await MergePredictionPicksAsync(db, user, groupMatches, pickMap, ct);

        var (teamA, teamB, ready) = BracketEngine.ResolveTeams(slot, matchMap, groupMatches, pickMap);
        if (!ready)
        {
            return Results.BadRequest(new { error = "Complete earlier round picks before this match." });
        }

        var winner = request.WinnerTeamCode.Trim().ToUpperInvariant();
        if (winner != teamA?.Code && winner != teamB?.Code)
        {
            return Results.BadRequest(new { error = "Winner must be one of the teams in this slot." });
        }

        var pick = existingPicks.FirstOrDefault(p =>
            string.Equals(p.SlotId, slot.SlotId, StringComparison.OrdinalIgnoreCase));

        if (pick is null)
        {
            pick = new BracketPick
            {
                Id = Guid.NewGuid(),
                UserId = user.IsAuthenticated ? user.UserId : null,
                AnonymousUserId = user.IsAnonymous ? user.AnonymousUserId : null,
                SlotId = slot.SlotId,
                MatchId = slot.MatchId,
                WinnerTeamCode = winner
            };
            db.BracketPicks.Add(pick);
        }
        else
        {
            if (pick.LockedAt is not null)
            {
                return Results.BadRequest(new { error = "This bracket pick is locked." });
            }

            pick.WinnerTeamCode = winner;
        }

        foreach (var downstreamId in BracketEngine.DownstreamSlotIds(slot.SlotId, groupMatches))
        {
            var downstream = existingPicks.FirstOrDefault(p =>
                string.Equals(p.SlotId, downstreamId, StringComparison.OrdinalIgnoreCase));
            if (downstream is not null)
            {
                db.BracketPicks.Remove(downstream);
            }
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(new BracketPickResponse(pick.SlotId, pick.MatchId, pick.WinnerTeamCode, pick.LockedAt));
    }

    private static async Task<(List<Match> GroupMatches, Dictionary<string, Match> MatchMap)> LoadBracketMatchesAsync(
        AppDbContext db,
        CancellationToken ct)
    {
        var knockoutIds = BracketTemplate.KnockoutSlots.Select(s => s.MatchId).Distinct().ToList();
        var groupMatches = await db.Matches
            .Where(m => m.Group != null && m.Group != "")
            .OrderBy(m => m.Group)
            .ThenBy(m => m.KickoffTime)
            .ToListAsync(ct);

        var knockoutMatches = await db.Matches
            .Where(m => knockoutIds.Contains(m.Id))
            .ToListAsync(ct);

        var matchMap = groupMatches
            .Concat(knockoutMatches)
            .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return (groupMatches, matchMap);
    }

    private static BracketStateResponse BuildState(
        IReadOnlyList<Match> groupMatches,
        IReadOnlyDictionary<string, Match> matchMap,
        IReadOnlyDictionary<string, string> pickMap,
        IReadOnlyList<BracketPick>? picks = null)
    {
        var slots = BracketEngine.GetAllSlots(groupMatches);
        var rounds = slots
            .GroupBy(s => s.RoundOrder)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var phase = g.Key == 0 ? "group" : "knockout";
                var label = g.Key switch
                {
                    0 => "Group stage",
                    1 => "Round of 16",
                    2 => "Quarter-finals",
                    3 => "Semi-finals",
                    4 => "Final",
                    _ => g.First().Round
                };
                var orderedSlots = g.Key == 0
                    ? g.OrderBy(s => s.Round).ThenBy(s => s.Position).ToList()
                    : g.OrderBy(s => s.Position).ToList();

                return new BracketRoundResponse(
                    label,
                    g.Key,
                    orderedSlots
                        .Select(slot => MapSlot(slot, matchMap, groupMatches, pickMap))
                        .ToList(),
                    phase);
            })
            .ToList();

        var standings = GroupStandingsService.ComputeStandings(groupMatches, pickMap)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(s => new GroupStandingResponse(
                    s.TeamCode,
                    s.TeamName,
                    s.Played,
                    s.Won,
                    s.Drawn,
                    s.Lost,
                    s.GoalsFor,
                    s.GoalsAgainst,
                    s.GoalDifference,
                    s.Points,
                    s.Rank)).ToList() as IReadOnlyList<GroupStandingResponse>,
                StringComparer.OrdinalIgnoreCase);

        var pickResponses = (picks ?? [])
            .Select(p => new BracketPickResponse(p.SlotId, p.MatchId, p.WinnerTeamCode, p.LockedAt))
            .ToList();

        return new BracketStateResponse(rounds, pickResponses, standings);
    }

    private static BracketSlotResponse MapSlot(
        BracketSlotDefinition slot,
        IReadOnlyDictionary<string, Match> matches,
        IReadOnlyList<Match> groupMatches,
        IReadOnlyDictionary<string, string> picks)
    {
        matches.TryGetValue(slot.MatchId, out var match);
        var (teamA, teamB, ready) = BracketEngine.ResolveTeams(slot, matches, groupMatches, picks);
        var locked = match is not null && MatchLockService.IsLocked(match);
        picks.TryGetValue(slot.SlotId, out var pickedWinner);

        return new BracketSlotResponse(
            slot.SlotId,
            slot.MatchId,
            slot.Round,
            slot.RoundOrder,
            slot.Position,
            slot.Kind.ToString(),
            teamA is null ? null : new BracketTeamResponse(teamA.Code, teamA.Name),
            teamB is null ? null : new BracketTeamResponse(teamB.Code, teamB.Name),
            ready,
            pickedWinner,
            locked,
            match?.KickoffTime,
            match?.Venue ?? string.Empty,
            slot.QualifierA is null ? null : $"{slot.QualifierA.Group}{slot.QualifierA.Rank}",
            slot.SourceSlotAId,
            slot.SourceSlotBId);
    }

    private static async Task<List<BracketPick>> LoadPicks(AppDbContext db, IUserContext user, CancellationToken ct)
    {
        var query = db.BracketPicks.AsQueryable();
        query = user.IsAuthenticated
            ? query.Where(p => p.UserId == user.UserId)
            : query.Where(p => p.AnonymousUserId == user.AnonymousUserId);

        return await query.ToListAsync(ct);
    }

    /// <summary>
    /// Quick match predictions double as bracket group-stage picks: every Result
    /// prediction the user made on a group match fills the matching bracket slot
    /// (explicit bracket picks always win over implied ones).
    /// </summary>
    private static async Task MergePredictionPicksAsync(
        AppDbContext db,
        IUserContext user,
        IReadOnlyList<Match> groupMatches,
        Dictionary<string, string> pickMap,
        CancellationToken ct)
    {
        if (groupMatches.Count == 0)
        {
            return;
        }

        var groupMatchIds = groupMatches.Select(m => m.Id).ToList();
        var query = db.Predictions
            .Where(p => p.PredictionType == PredictionType.Result && groupMatchIds.Contains(p.MatchId));
        query = user.IsAuthenticated
            ? query.Where(p => p.UserId == user.UserId)
            : query.Where(p => p.AnonymousUserId == user.AnonymousUserId);

        var predictions = await query.ToListAsync(ct);
        if (predictions.Count == 0)
        {
            return;
        }

        var matchById = groupMatches
            .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var prediction in predictions)
        {
            var slotId = BracketEngine.GroupSlotId(prediction.MatchId);
            if (pickMap.ContainsKey(slotId) ||
                !matchById.TryGetValue(prediction.MatchId, out var match))
            {
                continue;
            }

            var winner = ResolveImpliedWinner(prediction.PredictionValue, match);
            if (winner is not null)
            {
                pickMap[slotId] = winner;
            }
        }
    }

    private static string? ResolveImpliedWinner(string predictionValue, Match match) =>
        predictionValue.Trim().ToUpperInvariant() switch
        {
            "HOME" or "HOME WIN" or "H" or "1" => match.TeamACode,
            "AWAY" or "AWAY WIN" or "A" or "2" => match.TeamBCode,
            "DRAW" or "D" or "X" => DrawPick,
            _ => null
        };
}

public record BracketTemplateResponse(
    IReadOnlyList<BracketRoundResponse> Rounds,
    IReadOnlyDictionary<string, IReadOnlyList<GroupStandingResponse>> Standings);

public record BracketStateResponse(
    IReadOnlyList<BracketRoundResponse> Rounds,
    IReadOnlyList<BracketPickResponse> Picks,
    IReadOnlyDictionary<string, IReadOnlyList<GroupStandingResponse>> Standings);

public record BracketRoundResponse(
    string Label,
    int Order,
    IReadOnlyList<BracketSlotResponse> Slots,
    string Phase);

public record BracketSlotResponse(
    string SlotId,
    string MatchId,
    string Round,
    int RoundOrder,
    int Position,
    string Kind,
    BracketTeamResponse? TeamA,
    BracketTeamResponse? TeamB,
    bool Ready,
    string? PickedWinnerCode,
    bool IsLocked,
    DateTimeOffset? KickoffTime,
    string Venue,
    string? QualifierLabel,
    string? SourceSlotAId,
    string? SourceSlotBId);

public record BracketTeamResponse(string Code, string Name);

public record GroupStandingResponse(
    string TeamCode,
    string TeamName,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points,
    int Rank);

public record BracketPickResponse(
    string SlotId,
    string MatchId,
    string WinnerTeamCode,
    DateTimeOffset? LockedAt);

public record SaveBracketPickRequest(string SlotId, string WinnerTeamCode, string? TurnstileToken);

public sealed class SaveBracketPickValidator : AbstractValidator<SaveBracketPickRequest>
{
    public SaveBracketPickValidator()
    {
        RuleFor(x => x.SlotId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.WinnerTeamCode).NotEmpty().MaximumLength(8);
    }
}
