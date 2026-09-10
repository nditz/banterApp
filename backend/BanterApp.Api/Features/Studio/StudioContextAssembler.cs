using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Receipts;

namespace BanterApp.Api.Features.Studio;

public sealed record StudioSourcedPunditTake(
    string Name,
    string Prediction,
    string? SourceUrl,
    string? SourcePlatform,
    bool WasCorrect);

public sealed record StudioPackContext(
    string StoryKind,
    string Headline,
    Guid? ReceiptId,
    string? MatchId,
    string? FeedItemId,
    string? TeamA,
    string? TeamB,
    string? Scoreline,
    string? UserPick,
    string? PredictionType,
    int? PointsAwarded,
    int? AuraDelta,
    string? StoryType,
    IReadOnlyList<StudioSourcedPunditTake> PunditTakes,
    IReadOnlyList<StudioFact> Facts,
    IReadOnlyList<string> SourceNotes);

public static class StudioContextAssembler
{
    public static StudioPackContext FromReceipt(ReceiptDto receipt)
    {
        var teamA = receipt.Match?.TeamA;
        var teamB = receipt.Match?.TeamB;
        var hasScore = receipt.HomeScore.HasValue && receipt.AwayScore.HasValue;
        var scoreline = hasScore
            ? $"{teamA} {receipt.HomeScore}–{receipt.AwayScore} {teamB}"
            : null;
        var userPick = FormatPick(receipt.PredictionType, receipt.PredictionValue, teamA, teamB);
        var fixture = teamA is { Length: > 0 } && teamB is { Length: > 0 }
            ? $"{teamA} vs {teamB}"
            : "Match";

        var facts = new List<StudioFact>
        {
            new("Fixture", fixture, "match")
        };

        if (scoreline is not null)
        {
            facts.Add(new StudioFact("Result", scoreline, "match"));
        }

        facts.Add(new StudioFact("Your pick", userPick, "prediction"));
        facts.Add(new StudioFact("Pick type", receipt.PredictionType, "prediction"));
        facts.Add(new StudioFact("Points", receipt.PointsAwarded.ToString(), "receipt"));
        facts.Add(new StudioFact("Aura delta", receipt.AuraDelta.ToString(), "receipt"));
        if (!string.IsNullOrWhiteSpace(receipt.StoryType))
        {
            facts.Add(new StudioFact("Story type", receipt.StoryType, "receipt"));
        }

        var takes = receipt.PunditTakes
            .Select(t => new StudioSourcedPunditTake(
                t.Name,
                t.Prediction,
                t.SourceUrl,
                t.SourcePlatform,
                t.WasCorrect))
            .ToList();

        foreach (var take in takes)
        {
            var result = take.WasCorrect ? "landed" : "missed";
            facts.Add(new StudioFact(
                $"Sourced take · {take.Name}",
                $"{take.Prediction} ({result})",
                "pundit_source"));
        }

        var notes = new List<string>
        {
            "Facts are copied from your Ball Takes receipt, match result, and prediction. Creative hook/script/captions are generated framing, not extra football data."
        };
        if (!hasScore)
        {
            notes.Add("This receipt has no recorded scoreline. Studio did not invent one.");
        }

        foreach (var take in takes)
        {
            if (!string.IsNullOrWhiteSpace(take.SourceUrl))
            {
                notes.Add($"Sourced pundit take: {take.Name} via {take.SourceUrl}");
            }
            else
            {
                notes.Add($"Sourced pundit take: {take.Name} ({take.SourcePlatform ?? "source desk"}). No fabricated quote.");
            }
        }

        var headline = receipt.StoryCandidates.FirstOrDefault()?.Summary
            ?? (scoreline is not null ? $"{fixture}: {scoreline}" : fixture);

        var kind = takes.Count > 0 ? StudioContentCatalog.KindVsPundit : StudioContentCatalog.KindReceipt;

        return new StudioPackContext(
            kind,
            headline,
            receipt.Id,
            receipt.MatchId,
            null,
            teamA,
            teamB,
            scoreline,
            userPick,
            receipt.PredictionType,
            receipt.PointsAwarded,
            receipt.AuraDelta,
            receipt.StoryType,
            takes,
            facts,
            notes);
    }

    public static StudioPackContext FromNews(NewsFeedItem item)
    {
        var title = string.IsNullOrWhiteSpace(item.Title) ? "Football story" : item.Title.Trim();
        var summary = string.IsNullOrWhiteSpace(item.Summary) ? null : item.Summary.Trim();
        var facts = new List<StudioFact>
        {
            new("Headline", title, "news")
        };
        if (summary is not null)
        {
            facts.Add(new StudioFact("Summary", summary, "news"));
        }

        if (!string.IsNullOrWhiteSpace(item.Source))
        {
            facts.Add(new StudioFact("Source", item.Source, "news"));
        }

        if (!string.IsNullOrWhiteSpace(item.Author))
        {
            facts.Add(new StudioFact("Author", item.Author, "news"));
        }

        facts.Add(new StudioFact("Published", item.PublishedAt.ToString("u"), "news"));

        var notes = new List<string>
        {
            "Facts are copied from the sourced feed item. Studio did not invent a scoreline, lineup, or pundit quote.",
            "Creative hook/script/captions are generated framing around that headline only."
        };
        if (!string.IsNullOrWhiteSpace(item.Url))
        {
            notes.Add($"Source URL: {item.Url}");
        }

        return new StudioPackContext(
            StudioContentCatalog.KindTrending,
            title,
            null,
            string.IsNullOrWhiteSpace(item.MatchId) ? null : item.MatchId,
            item.Id,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [],
            facts,
            notes);
    }

    public static StudioPackContext FromPreviousPack(StudioContentPack pack)
    {
        var facts = pack.Facts.ToList();
        var notes = pack.SourceNotes.ToList();
        if (!notes.Exists(n => n.Contains("previous project", StringComparison.OrdinalIgnoreCase)))
        {
            notes.Add("Regenerated from a saved Studio project. Facts were sourced at original generation time.");
        }

        return new StudioPackContext(
            pack.StoryKind,
            pack.Title,
            pack.ReceiptId,
            pack.MatchId,
            pack.FeedItemId,
            ExtractTeam(facts, 0),
            ExtractTeam(facts, 1),
            facts.FirstOrDefault(f => string.Equals(f.Label, "Result", StringComparison.OrdinalIgnoreCase))?.Value,
            facts.FirstOrDefault(f => string.Equals(f.Label, "Your pick", StringComparison.OrdinalIgnoreCase))?.Value,
            facts.FirstOrDefault(f => string.Equals(f.Label, "Pick type", StringComparison.OrdinalIgnoreCase))?.Value,
            TryInt(facts, "Points"),
            TryInt(facts, "Aura delta"),
            facts.FirstOrDefault(f => string.Equals(f.Label, "Story type", StringComparison.OrdinalIgnoreCase))?.Value,
            [],
            facts,
            notes);
    }

    private static string FormatPick(string type, string value, string? teamA, string? teamB) =>
        type switch
        {
            "correct_score" => $"Correct score {value}",
            "double_chance" => value.Replace('_', ' '),
            "result" when value == "home" && teamA is { Length: > 0 } => $"{teamA} to win",
            "result" when value == "away" && teamB is { Length: > 0 } => $"{teamB} to win",
            "result" when value == "draw" => "Draw",
            _ => value
        };

    private static string? ExtractTeam(IReadOnlyList<StudioFact> facts, int index)
    {
        var fixture = facts.FirstOrDefault(f => string.Equals(f.Label, "Fixture", StringComparison.OrdinalIgnoreCase))?.Value;
        if (string.IsNullOrWhiteSpace(fixture))
        {
            return null;
        }

        var parts = fixture.Split(" vs ", 2, StringSplitOptions.TrimEntries);
        return parts.Length > index ? parts[index] : null;
    }

    private static int? TryInt(IReadOnlyList<StudioFact> facts, string label)
    {
        var value = facts.FirstOrDefault(f => string.Equals(f.Label, label, StringComparison.OrdinalIgnoreCase))?.Value;
        return int.TryParse(value, out var n) ? n : null;
    }
}
