using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.SportsData.Dtos;

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
            KickoffTime = dto.KickoffUtc,
            Stage = dto.Stage,
            Group = dto.Group,
            Venue = dto.Venue,
            Status = dto.Status,
            HomeScore = dto.HomeScore,
            AwayScore = dto.AwayScore
        };

    public static bool ApplyDto(Match match, MatchDto dto)
    {
        var changed = false;

        if (match.TeamA != dto.HomeTeam.Name) { match.TeamA = dto.HomeTeam.Name; changed = true; }
        if (match.TeamB != dto.AwayTeam.Name) { match.TeamB = dto.AwayTeam.Name; changed = true; }
        if (match.TeamACode != dto.HomeTeam.Code) { match.TeamACode = dto.HomeTeam.Code; changed = true; }
        if (match.TeamBCode != dto.AwayTeam.Code) { match.TeamBCode = dto.AwayTeam.Code; changed = true; }
        if (match.KickoffTime != dto.KickoffUtc) { match.KickoffTime = dto.KickoffUtc; changed = true; }
        if (match.Stage != dto.Stage) { match.Stage = dto.Stage; changed = true; }
        if (match.Group != dto.Group) { match.Group = dto.Group; changed = true; }
        if (match.Venue != dto.Venue) { match.Venue = dto.Venue; changed = true; }
        if (match.Status != dto.Status) { match.Status = dto.Status; changed = true; }
        if (match.HomeScore != dto.HomeScore) { match.HomeScore = dto.HomeScore; changed = true; }
        if (match.AwayScore != dto.AwayScore) { match.AwayScore = dto.AwayScore; changed = true; }

        return changed;
    }
}
