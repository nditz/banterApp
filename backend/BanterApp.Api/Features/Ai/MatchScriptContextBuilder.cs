using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Features.Ai;

public sealed class MatchScriptContextBuilder
{
    private readonly ISportsDataProvider _sports;
    private readonly ISportsDataEnrichment? _enrichment;

    public MatchScriptContextBuilder(
        ISportsDataProvider sports,
        IServiceProvider serviceProvider)
    {
        _sports = sports;
        _enrichment = serviceProvider.GetService<ISportsDataEnrichment>();
    }

    public async Task<MatchScriptContext?> BuildAsync(
        string matchId,
        string phase,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(matchId))
        {
            return null;
        }

        var match = await ResolveMatchAsync(matchId, cancellationToken);
        if (match is null)
        {
            return null;
        }

        var stats = await _sports.GetMatchStatisticsAsync(matchId, cancellationToken);

        IReadOnlyList<MatchEventDto> events = [];
        IReadOnlyList<LineupPlayerDto> lineups = [];

        if (string.Equals(phase, "post_match", StringComparison.OrdinalIgnoreCase) && _enrichment is not null)
        {
            try
            {
                events = await _enrichment.GetMatchEventsAsync(matchId, cancellationToken);
            }
            catch
            {
                events = [];
            }

            try
            {
                lineups = await _enrichment.GetMatchLineupsAsync(matchId, cancellationToken);
            }
            catch
            {
                lineups = [];
            }
        }
        else if (_enrichment is not null)
        {
            try
            {
                lineups = await _enrichment.GetMatchLineupsAsync(matchId, cancellationToken);
            }
            catch
            {
                lineups = [];
            }
        }

        IReadOnlyList<StandingDto> standings = [];
        if (!string.IsNullOrWhiteSpace(match.Group))
        {
            try
            {
                standings = await _sports.GetStandingsAsync(match.Group, cancellationToken);
            }
            catch
            {
                standings = [];
            }
        }

        return new MatchScriptContext(match, stats, events, lineups, standings);
    }

    private async Task<MatchDto?> ResolveMatchAsync(string matchId, CancellationToken cancellationToken)
    {
        var upcoming = await _sports.GetUpcomingFixturesAsync(cancellationToken);
        var found = upcoming.FirstOrDefault(m =>
            string.Equals(m.Id, matchId, StringComparison.OrdinalIgnoreCase));
        if (found is not null)
        {
            return found;
        }

        var results = await _sports.GetResultsAsync(cancellationToken);
        return results.FirstOrDefault(m =>
            string.Equals(m.Id, matchId, StringComparison.OrdinalIgnoreCase));
    }
}
