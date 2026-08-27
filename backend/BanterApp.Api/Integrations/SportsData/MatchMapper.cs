using BanterApp.Api.Common;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.SportsData.Dtos;
using BanterApp.Api.Services;

namespace BanterApp.Api.Integrations.SportsData;

internal static class MatchMapper
{
    public static Match FromDto(MatchDto dto) =>
        new()
        {
            Id = dto.Id,
            TeamA = dto.HomeTeam.Name,
            TeamB = dto.AwayTeam.Name,
            TeamACode = dto.HomeTeam.Code,
            TeamBCode = dto.AwayTeam.Code,
            HomeLogoUrl = dto.HomeTeam.LogoUrl,
            AwayLogoUrl = dto.AwayTeam.LogoUrl,
            KickoffTime = PostgresUtc.Normalize(dto.KickoffUtc),
            PredictionLockAtUtc = PostgresUtc.Normalize(dto.KickoffUtc),
            Stage = dto.Stage,
            Group = string.Empty,
            Venue = dto.Venue,
            Status = dto.Status,
            HomeScore = dto.HomeScore,
            AwayScore = dto.AwayScore,
            MatchweekNumber = dto.MatchweekNumber ?? MatchweekParser.TryParse(dto.Stage)
        };

    public static bool ApplyDto(Match match, MatchDto dto)
    {
        var changed = false;
        var week = dto.MatchweekNumber ?? MatchweekParser.TryParse(dto.Stage);

        if (match.TeamA != dto.HomeTeam.Name) { match.TeamA = dto.HomeTeam.Name; changed = true; }
        if (match.TeamB != dto.AwayTeam.Name) { match.TeamB = dto.AwayTeam.Name; changed = true; }
        if (match.TeamACode != dto.HomeTeam.Code) { match.TeamACode = dto.HomeTeam.Code; changed = true; }
        if (match.TeamBCode != dto.AwayTeam.Code) { match.TeamBCode = dto.AwayTeam.Code; changed = true; }
        if (match.HomeLogoUrl != dto.HomeTeam.LogoUrl && dto.HomeTeam.LogoUrl is not null)
        {
            match.HomeLogoUrl = dto.HomeTeam.LogoUrl;
            changed = true;
        }
        if (match.AwayLogoUrl != dto.AwayTeam.LogoUrl && dto.AwayTeam.LogoUrl is not null)
        {
            match.AwayLogoUrl = dto.AwayTeam.LogoUrl;
            changed = true;
        }
        var kickoffUtc = PostgresUtc.Normalize(dto.KickoffUtc);
        if (match.KickoffTime != kickoffUtc) { match.KickoffTime = kickoffUtc; changed = true; }
        if (match.PredictionLockAtUtc != kickoffUtc) { match.PredictionLockAtUtc = kickoffUtc; changed = true; }
        if (match.Stage != dto.Stage) { match.Stage = dto.Stage; changed = true; }
        if (!string.IsNullOrEmpty(match.Group)) { match.Group = string.Empty; changed = true; }
        if (match.Venue != dto.Venue) { match.Venue = dto.Venue; changed = true; }
        if (match.Status != dto.Status) { match.Status = dto.Status; changed = true; }
        if (match.HomeScore != dto.HomeScore) { match.HomeScore = dto.HomeScore; changed = true; }
        if (match.AwayScore != dto.AwayScore) { match.AwayScore = dto.AwayScore; changed = true; }
        if (match.MatchweekNumber != week) { match.MatchweekNumber = week; changed = true; }

        return changed;
    }
}
