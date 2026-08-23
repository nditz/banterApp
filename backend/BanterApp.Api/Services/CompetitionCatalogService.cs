using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Data;

public sealed class CompetitionCatalogService(AppDbContext db)
{
    public async Task<CompetitionSeason> EnsureCurrentPremierLeagueAsync(CancellationToken cancellationToken)
    {
        var competition = await db.Competitions.FindAsync([PremierLeagueCatalog.CompetitionId], cancellationToken);
        if (competition is null)
        {
            competition = new Competition
            {
                Id = PremierLeagueCatalog.CompetitionId,
                Name = PremierLeagueCatalog.Name,
                Slug = PremierLeagueCatalog.Slug,
                Code = PremierLeagueCatalog.Code,
                CountryCode = PremierLeagueCatalog.CountryCode,
                Provider = PremierLeagueCatalog.Provider,
                ProviderCompetitionId = PremierLeagueCatalog.ProviderCompetitionId,
                IsActive = true,
                IsAvailableForPrediction = true,
                DisplayOrder = 1
            };
            db.Competitions.Add(competition);
        }
        else
        {
            competition.IsActive = true;
            competition.IsAvailableForPrediction = true;
            competition.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var season = await db.CompetitionSeasons.FindAsync([PremierLeagueCatalog.SeasonId], cancellationToken);
        if (season is null)
        {
            season = new CompetitionSeason
            {
                Id = PremierLeagueCatalog.SeasonId,
                CompetitionId = PremierLeagueCatalog.CompetitionId,
                Name = PremierLeagueCatalog.CurrentSeasonName,
                StartYear = PremierLeagueCatalog.CurrentSeasonStartYear,
                ProviderSeasonId = PremierLeagueCatalog.CurrentSeasonStartYear.ToString(),
                Status = "current",
                IsCurrent = true
            };
            db.CompetitionSeasons.Add(season);
        }
        else
        {
            season.IsCurrent = true;
            season.Status = "current";
            season.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return season;
    }

    public async Task<Matchweek> EnsureMatchweekAsync(
        CompetitionSeason season,
        int number,
        DateTimeOffset kickoff,
        CancellationToken cancellationToken)
    {
        var week = await db.Matchweeks.FirstOrDefaultAsync(
            w => w.CompetitionSeasonId == season.Id && w.Number == number,
            cancellationToken);

        if (week is null)
        {
            week = new Matchweek
            {
                Id = Guid.NewGuid(),
                CompetitionSeasonId = season.Id,
                Number = number,
                Name = $"Matchweek {number}",
                StartDate = kickoff,
                EndDate = kickoff,
                Status = "scheduled"
            };
            db.Matchweeks.Add(week);
            await db.SaveChangesAsync(cancellationToken);
            return week;
        }

        if (week.StartDate is null || kickoff < week.StartDate)
        {
            week.StartDate = kickoff;
        }

        if (week.EndDate is null || kickoff > week.EndDate)
        {
            week.EndDate = kickoff;
        }

        week.UpdatedAt = DateTimeOffset.UtcNow;
        return week;
    }

    public async Task UpsertClubAsync(
        string code,
        string name,
        string? logoUrl,
        string? providerTeamId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || code == "TBD")
        {
            return;
        }

        var normalized = code.Trim().ToUpperInvariant();
        var slug = ToSlug(name, normalized);
        var team = await db.ClubTeams.FirstOrDefaultAsync(
            t => t.Code == normalized || t.Slug == slug,
            cancellationToken);

        if (team is null)
        {
            team = new ClubTeam
            {
                Id = Guid.NewGuid(),
                Name = name,
                ShortName = name,
                Slug = slug,
                Code = normalized,
                LogoUrl = logoUrl,
                Provider = PremierLeagueCatalog.Provider,
                ProviderTeamId = providerTeamId
            };
            db.ClubTeams.Add(team);
        }
        else
        {
            team.Name = name;
            if (!string.IsNullOrWhiteSpace(logoUrl))
            {
                team.LogoUrl = logoUrl;
            }

            if (!string.IsNullOrWhiteSpace(providerTeamId))
            {
                team.ProviderTeamId = providerTeamId;
            }

            team.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var linked = await db.SeasonTeams.AnyAsync(
            st => st.CompetitionSeasonId == PremierLeagueCatalog.SeasonId && st.TeamId == team.Id,
            cancellationToken);
        if (!linked && team.Id != Guid.Empty)
        {
            await db.SaveChangesAsync(cancellationToken);
            if (!await db.SeasonTeams.AnyAsync(
                    st => st.CompetitionSeasonId == PremierLeagueCatalog.SeasonId && st.TeamId == team.Id,
                    cancellationToken))
            {
                db.SeasonTeams.Add(new SeasonTeam
                {
                    Id = Guid.NewGuid(),
                    CompetitionSeasonId = PremierLeagueCatalog.SeasonId,
                    TeamId = team.Id
                });
            }
        }
    }

    private static string ToSlug(string name, string fallback)
    {
        var slug = new string(name.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? fallback.ToLowerInvariant() : slug;
    }
}
