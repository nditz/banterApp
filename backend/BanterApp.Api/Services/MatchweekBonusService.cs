using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Services;

public sealed class MatchweekBonusService(AppDbContext db, ScoringService scoring)
{
    public async Task<int> AwardFinishedMatchweeksAsync(CancellationToken cancellationToken)
    {
        var weeks = await db.Matches
            .AsNoTracking()
            .Where(m => m.MatchweekNumber != null)
            .Select(m => m.MatchweekNumber!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var awarded = 0;
        foreach (var week in weeks)
        {
            awarded += await AwardWeekAsync(week, cancellationToken);
        }

        return awarded;
    }

    public async Task<Dictionary<Guid, int>> GetBonusPointsByIdentityAsync(
        IReadOnlyList<LeagueMember> members,
        CancellationToken cancellationToken)
    {
        var userIds = members.Where(m => m.UserId.HasValue).Select(m => m.UserId!.Value).ToList();
        var anonIds = members.Where(m => m.AnonymousUserId.HasValue).Select(m => m.AnonymousUserId!.Value).ToList();

        var userBonus = await db.MatchweekBonuses
            .Where(p => p.UserId.HasValue && userIds.Contains(p.UserId.Value))
            .GroupBy(p => p.UserId!.Value)
            .Select(g => new { Id = g.Key, Points = g.Sum(p => p.PointsAwarded) })
            .ToListAsync(cancellationToken);

        var anonBonus = await db.MatchweekBonuses
            .Where(p => p.AnonymousUserId.HasValue && anonIds.Contains(p.AnonymousUserId.Value))
            .GroupBy(p => p.AnonymousUserId!.Value)
            .Select(g => new { Id = g.Key, Points = g.Sum(p => p.PointsAwarded) })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<Guid, int>();
        foreach (var entry in userBonus.Concat(anonBonus))
        {
            map[entry.Id] = entry.Points;
        }

        return map;
    }

    private async Task<int> AwardWeekAsync(int matchweekNumber, CancellationToken cancellationToken)
    {
        var matches = await db.Matches
            .AsNoTracking()
            .Where(m => m.MatchweekNumber == matchweekNumber)
            .ToListAsync(cancellationToken);

        if (matches.Count == 0 || matches.Any(m => m.Status != "FT"))
        {
            return 0;
        }

        var matchIds = matches.Select(m => m.Id).ToList();
        var resultPicks = await db.Predictions
            .Where(p => matchIds.Contains(p.MatchId) && p.PredictionType == PredictionType.Result)
            .ToListAsync(cancellationToken);

        var groups = resultPicks.GroupBy(p => p.UserId ?? p.AnonymousUserId ?? Guid.Empty);
        var awarded = 0;

        foreach (var group in groups)
        {
            if (group.Key == Guid.Empty)
            {
                continue;
            }

            var picks = group.ToList();
            if (picks.Count < matches.Count)
            {
                continue;
            }

            var bonus = scoring.CalculatePerfectMatchweekBonus(picks, matches);
            if (bonus <= 0)
            {
                continue;
            }

            var sample = picks[0];
            var existing = await db.MatchweekBonuses.FirstOrDefaultAsync(
                b => b.MatchweekNumber == matchweekNumber &&
                     b.CompetitionSeasonId == PremierLeagueCatalog.SeasonId &&
                     (sample.UserId != null
                         ? b.UserId == sample.UserId
                         : b.AnonymousUserId == sample.AnonymousUserId),
                cancellationToken);

            if (existing is null)
            {
                db.MatchweekBonuses.Add(new MatchweekBonus
                {
                    Id = Guid.NewGuid(),
                    UserId = sample.UserId,
                    AnonymousUserId = sample.AnonymousUserId,
                    CompetitionSeasonId = PremierLeagueCatalog.SeasonId,
                    MatchweekNumber = matchweekNumber,
                    PointsAwarded = bonus
                });
                awarded++;
            }
            else if (existing.PointsAwarded != bonus)
            {
                existing.PointsAwarded = bonus;
                awarded++;
            }
        }

        if (awarded > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return awarded;
    }
}
