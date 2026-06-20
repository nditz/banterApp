using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Features.Ai;

/// <summary>
/// Composes prediction recaps for short-form content: pundit-style pre-match,
/// post-match praise (best picks) or burn (worst picks).
/// </summary>
public static class BroadcastScriptComposer
{
    private const string Tagline = "I know ball — watch me.";

    public static string Compose(
        string phase,
        string? style,
        IReadOnlyList<BroadcastPick> picks,
        IReadOnlyDictionary<string, MatchStatisticsDto> statsByMatchId)
    {
        var scriptStyle = NormalizeStyle(style, phase);
        var filtered = FilterPicks(picks, phase, scriptStyle);

        return phase == "post_match"
            ? ComposePostMatch(filtered, statsByMatchId, scriptStyle)
            : ComposePreMatch(filtered, statsByMatchId, scriptStyle);
    }

    private static string NormalizeStyle(string? style, string phase) =>
        phase == "pre_match"
            ? "pundit"
            : style?.Trim().ToLowerInvariant() switch
            {
                "praise" or "burn" or "full" => style.Trim().ToLowerInvariant(),
                _ => "full",
            };

    private static IReadOnlyList<BroadcastPick> FilterPicks(
        IReadOnlyList<BroadcastPick> picks,
        string phase,
        string style)
    {
        if (phase != "post_match" || style == "full")
        {
            return picks;
        }

        return style == "praise"
            ? picks.Where(p => (p.PointsAwarded ?? 0) > 0).ToList()
            : picks.Where(p => (p.PointsAwarded ?? 0) <= 0).ToList();
    }

    private static string ComposePreMatch(
        IReadOnlyList<BroadcastPick> picks,
        IReadOnlyDictionary<string, MatchStatisticsDto> stats,
        string style)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("[DESK — COLD OPEN — pundit cadence]");
        sb.AppendLine(picks.Count == 0
            ? "Alright, quick one from the desk — card's empty. Get your picks in before we go live."
            : $"Right, let's get into it. I've got {picks.Count} call{(picks.Count == 1 ? "" : "s")} on the record before a ball's kicked — and yes, the group chat will have opinions.");

        if (picks.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[BULLETIN HEADLINE]");
            var headline = picks[0];
            sb.AppendLine(
                $"Main event: {headline.TeamA} vs {headline.TeamB}. I'm leaning {headline.Prediction}. " +
                "The desk is split, the timeline is loud, we're doing this properly.");

            for (var i = 0; i < picks.Count; i++)
            {
                var pick = picks[i];
                sb.AppendLine();
                sb.AppendLine($"[SEGMENT {i + 1} — {pick.TeamA} v {pick.TeamB}]");
                sb.AppendLine($"To camera: \"I'm on {pick.Prediction}. Lowkey confident, highkey accountable.\"");
                sb.AppendLine("B-roll: crowd shots, form table, your pick receipt on screen.");

                if (pick.MatchId is not null && stats.TryGetValue(pick.MatchId, out var s))
                {
                    sb.AppendLine(
                        $"Stats overlay: {pick.TeamA} {s.HomeShots} shots ({s.HomeShotsOnTarget} on target), " +
                        $"{s.HomePossessionPercent}% possession · {pick.TeamB} {s.AwayShots} shots, {s.AwayPossessionPercent}%.");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("[OUTRO — vertical clip hook]");
        sb.AppendLine($"Post this before kickoff. Tag us when it lands. {Tagline}");
        sb.AppendLine();
        sb.Append("#WorldCup2026 #BanterApp #BallTakes #PreMatch");

        return sb.ToString();
    }

    private static string ComposePostMatch(
        IReadOnlyList<BroadcastPick> picks,
        IReadOnlyDictionary<string, MatchStatisticsDto> stats,
        string style)
    {
        var sb = new System.Text.StringBuilder();
        var totalPoints = picks.Sum(p => p.PointsAwarded ?? 0);
        var hits = picks.Count(p => (p.PointsAwarded ?? 0) > 0);

        var tone = style switch
        {
            "praise" => "PRAISE CUT — best picks only",
            "burn" => "BURN CUT — worst picks only",
            _ => "FULL RECAP",
        };

        sb.AppendLine($"[STUDIO — {tone}]");
        sb.AppendLine(picks.Count == 0
            ? style == "praise"
                ? "No W's on the card yet — come back when you've got receipts to flex."
                : style == "burn"
                    ? "Nothing to roast yet. Either you're perfect or the results desk is empty."
                    : "Full time, but the results desk is empty. Pull your picks and run this again."
            : style == "praise"
                ? $"Okay okay — {hits} banger{(hits == 1 ? "" : "s")} on the board. Let's hype what actually cooked."
                : style == "burn"
                    ? $"We don't skip the L's. {picks.Count} pick{(picks.Count == 1 ? "" : "s")} to put on blast — respectfully, but publicly."
                    : "Full time. Graphics up. Let's walk through the card — wins first, then the pain.");

        if (picks.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[SCOREBOARD GRAPHIC]");
            sb.AppendLine($"{hits}/{picks.Count} landed · {totalPoints} points · content engine go brr.");

            for (var i = 0; i < picks.Count; i++)
            {
                var pick = picks[i];
                var points = pick.PointsAwarded ?? 0;
                var hit = points > 0;

                sb.AppendLine();
                sb.AppendLine($"[SEGMENT {i + 1} — {pick.TeamA} v {pick.TeamB}]");
                sb.AppendLine($"Pick: {pick.Prediction}. Result: {pick.ActualResult ?? "TBC"}.");

                if (pick.MatchId is not null && stats.TryGetValue(pick.MatchId, out var s))
                {
                    sb.AppendLine(
                        $"Stats: {s.HomePossessionPercent}–{s.AwayPossessionPercent}% possession, " +
                        $"shots {s.HomeShots}–{s.AwayShots}.");
                }

                sb.AppendLine(hit
                    ? style == "praise" || style == "full"
                        ? $"Verdict: generational read. +{points} pts. Caption this \"I know ball\"."
                        : $"Verdict: W. +{points} pts."
                    : style == "burn" || style == "full"
                        ? "Verdict: cooked. Post the apology video or delete the app — your choice."
                        : "Verdict: miss. We move.");
            }

            sb.AppendLine();
            sb.AppendLine("[TO CAMERA — CTA]");
            sb.AppendLine(style switch
            {
                "praise" => "That's the flex reel. Stitch it, duet it, act like you meant it all along.",
                "burn" => "That's the roast reel. Lean in — the internet respects accountability (sometimes).",
                _ => hits == picks.Count
                    ? "Clean card. Rare air. The pundits are nervous."
                    : hits == 0
                        ? "Rough night. But the content writes itself when you're honest."
                        : "Mixed bag — perfect for a split praise/burn drop.",
            });
        }

        sb.AppendLine();
        sb.AppendLine($"[SIGN-OFF] {Tagline}");
        sb.AppendLine();
        sb.Append(style == "burn" ? "#Receipts #BanterApp #Cooked" : "#BallTakes #BanterApp #WorldCup2026");

        return sb.ToString();
    }
}
