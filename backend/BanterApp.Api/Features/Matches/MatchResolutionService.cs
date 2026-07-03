using System.Text.Json;
using System.Text.RegularExpressions;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Matches;

public sealed class MatchResolutionService
{
    private static readonly Regex VersusPattern = new(
        @"(?<teamA>.+?)\s+(?:vs\.?|v\.?|versus|-)\s+(?<teamB>.+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> MatchLevelPredictionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "match_result",
        "correct_score",
        "double_chance",
        "score_prediction",
        "match_prediction",
        "result",
        "score"
    };

    private readonly AppDbContext _db;

    public MatchResolutionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MatchCatalogEntry>> GetFixtureCatalogAsync(
        int maxFixtures = 40,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddDays(-3);
        var windowEnd = now.AddDays(14);

        var matches = await _db.Matches
            .AsNoTracking()
            .Where(m => m.KickoffTime >= windowStart && m.KickoffTime <= windowEnd)
            .OrderBy(m => m.KickoffTime)
            .Take(maxFixtures)
            .ToListAsync(cancellationToken);

        return matches
            .Select(m => new MatchCatalogEntry(
                m.Id,
                m.TeamA,
                m.TeamB,
                m.TeamACode,
                m.TeamBCode,
                m.KickoffTime,
                m.Stage,
                m.Group))
            .ToList();
    }

    public async Task<string> BuildFixtureCatalogJsonAsync(
        int maxFixtures = 40,
        CancellationToken cancellationToken = default)
    {
        var catalog = await GetFixtureCatalogAsync(maxFixtures, cancellationToken);
        return JsonSerializer.Serialize(catalog);
    }

    public async Task<MatchResolutionResult> ResolveAsync(
        string? matchText,
        string? teamHint,
        CancellationToken cancellationToken = default)
    {
        var catalog = await GetFixtureCatalogAsync(cancellationToken: cancellationToken);
        if (catalog.Count == 0)
        {
            return new MatchResolutionResult(null, null, null, 0);
        }

        var aliases = await LoadTeamAliasesAsync(cancellationToken);
        var candidates = new List<(MatchCatalogEntry Match, double Score)>();

        foreach (var fixture in catalog)
        {
            var score = ScoreFixture(fixture, matchText, teamHint, aliases);
            if (score > 0)
            {
                candidates.Add((fixture, score));
            }
        }

        if (candidates.Count == 0 && !string.IsNullOrWhiteSpace(teamHint))
        {
            foreach (var fixture in catalog)
            {
                var score = ScoreTeamOnly(fixture, teamHint, aliases);
                if (score > 0)
                {
                    candidates.Add((fixture, score * 0.6));
                }
            }
        }

        var best = candidates
            .OrderByDescending(c => c.Score)
            .FirstOrDefault();

        if (best.Match is null || best.Score < 0.45)
        {
            return new MatchResolutionResult(null, null, null, best.Score);
        }

        return new MatchResolutionResult(
            best.Match.Id,
            best.Match.TeamA,
            best.Match.TeamB,
            Math.Min(best.Score, 1.0));
    }

    public static bool IsMatchLevelPrediction(string? predictionType)
    {
        if (string.IsNullOrWhiteSpace(predictionType))
        {
            return false;
        }

        return MatchLevelPredictionTypes.Contains(predictionType.Trim());
    }

    private static double ScoreFixture(
        MatchCatalogEntry fixture,
        string? matchText,
        string? teamHint,
        IReadOnlyDictionary<string, string> aliases)
    {
        var score = 0.0;

        if (!string.IsNullOrWhiteSpace(matchText))
        {
            var normalized = Normalize(matchText);
            if (normalized.Contains(Normalize(fixture.Id), StringComparison.Ordinal))
            {
                return 1.0;
            }

            if (ContainsBothTeams(normalized, fixture.TeamA, fixture.TeamB, aliases))
            {
                score = Math.Max(score, 0.95);
            }
            else
            {
                var parsed = VersusPattern.Match(matchText);
                if (parsed.Success)
                {
                    var teamA = parsed.Groups["teamA"].Value.Trim();
                    var teamB = parsed.Groups["teamB"].Value.Trim();
                    if (TeamsMatch(teamA, fixture.TeamA, aliases) && TeamsMatch(teamB, fixture.TeamB, aliases))
                    {
                        score = Math.Max(score, 0.9);
                    }
                    else if (TeamsMatch(teamA, fixture.TeamB, aliases) && TeamsMatch(teamB, fixture.TeamA, aliases))
                    {
                        score = Math.Max(score, 0.9);
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(teamHint))
        {
            score = Math.Max(score, ScoreTeamOnly(fixture, teamHint, aliases));
        }

        return score;
    }

    private static double ScoreTeamOnly(
        MatchCatalogEntry fixture,
        string teamHint,
        IReadOnlyDictionary<string, string> aliases)
    {
        if (TeamsMatch(teamHint, fixture.TeamA, aliases) || TeamsMatch(teamHint, fixture.TeamB, aliases))
        {
            return 0.55;
        }

        return 0;
    }

    private static bool ContainsBothTeams(
        string normalizedText,
        string teamA,
        string teamB,
        IReadOnlyDictionary<string, string> aliases)
    {
        var hasA = ContainsTeam(normalizedText, teamA, aliases);
        var hasB = ContainsTeam(normalizedText, teamB, aliases);
        return hasA && hasB;
    }

    private static bool ContainsTeam(
        string normalizedText,
        string team,
        IReadOnlyDictionary<string, string> aliases)
    {
        var normalizedTeam = Normalize(team);
        if (normalizedText.Contains(normalizedTeam, StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var alias in ExpandAliases(team, aliases))
        {
            if (normalizedText.Contains(Normalize(alias), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TeamsMatch(
        string left,
        string right,
        IReadOnlyDictionary<string, string> aliases)
    {
        if (string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal))
        {
            return true;
        }

        var leftAliases = ExpandAliases(left, aliases).Select(Normalize).ToHashSet(StringComparer.Ordinal);
        var rightAliases = ExpandAliases(right, aliases).Select(Normalize).ToHashSet(StringComparer.Ordinal);
        return leftAliases.Overlaps(rightAliases);
    }

    private static IEnumerable<string> ExpandAliases(string team, IReadOnlyDictionary<string, string> aliases)
    {
        yield return team;

        var normalized = Normalize(team);
        foreach (var pair in aliases)
        {
            if (string.Equals(Normalize(pair.Key), normalized, StringComparison.Ordinal) ||
                string.Equals(Normalize(pair.Value), normalized, StringComparison.Ordinal))
            {
                yield return pair.Key;
                yield return pair.Value;
            }
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> LoadTeamAliasesAsync(
        CancellationToken cancellationToken)
    {
        var countries = await _db.Countries
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new { c.Name, c.Code })
            .ToListAsync(cancellationToken);

        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var country in countries)
        {
            if (!string.IsNullOrWhiteSpace(country.Name) && !string.IsNullOrWhiteSpace(country.Code))
            {
                aliases[country.Name] = country.Code;
            }
        }

        return aliases;
    }

    private static string Normalize(string value) =>
        Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9]+", " ");
}
