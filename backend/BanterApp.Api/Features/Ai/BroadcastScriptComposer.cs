using System.Text;
using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Features.Ai;

/// <summary>
/// Composes prediction recaps in the style of a TV sports journalist's studio
/// review: cold open, headlines, per-match segments backed by stats from the
/// sports data provider, and a sign-off.
/// </summary>
public static class BroadcastScriptComposer
{
    private const string Tagline = "I know ball — watch me.";

    public static string Compose(
        string phase,
        IReadOnlyList<BroadcastPick> picks,
        IReadOnlyDictionary<string, MatchStatisticsDto> statsByMatchId)
    {
        return phase == "post_match"
            ? ComposePostMatch(picks, statsByMatchId)
            : ComposePreMatch(picks, statsByMatchId);
    }

    private static string ComposePreMatch(
        IReadOnlyList<BroadcastPick> picks,
        IReadOnlyDictionary<string, MatchStatisticsDto> stats)
    {
        var sb = new StringBuilder();

        sb.AppendLine("[STUDIO — COLD OPEN]");
        sb.AppendLine(picks.Count == 0
            ? "Good evening from the BanterApp studio. The card is empty tonight — make your picks and come back for the full rundown."
            : $"Good evening from the BanterApp studio. Matchday is upon us, and I'm filing {picks.Count} prediction{(picks.Count == 1 ? "" : "s")} on the record tonight.");
        sb.AppendLine();

        if (picks.Count > 0)
        {
            sb.AppendLine("[HEADLINES]");
            var headline = picks[0];
            sb.AppendLine($"Top of the bulletin: {headline.TeamA} against {headline.TeamB} — my call is {headline.Prediction}. Hold that thought.");
            sb.AppendLine();

            for (var i = 0; i < picks.Count; i++)
            {
                var pick = picks[i];
                sb.AppendLine($"[SEGMENT {i + 1} — {pick.TeamA} v {pick.TeamB}]");
                sb.AppendLine($"To camera: \"My call here is {pick.Prediction}. Lock it in.\"");

                if (pick.MatchId is not null && stats.TryGetValue(pick.MatchId, out var s))
                {
                    sb.AppendLine(
                        $"Over to the stats desk: {pick.TeamA} are projected for {s.HomeShots} shots ({s.HomeShotsOnTarget} on target) " +
                        $"and {s.HomePossessionPercent}% of the ball; {pick.TeamB} counter with {s.AwayShots} shots and {s.AwayPossessionPercent}%. " +
                        "The numbers frame the matchup — the pitch decides it.");
                }

                sb.AppendLine();
            }
        }

        sb.AppendLine("[SIGN-OFF — to camera]");
        sb.AppendLine($"That's the card, on the record before kickoff. Clip it, post it, hold me to it. {Tagline}");
        sb.AppendLine();
        sb.Append("#WorldCup2026 #BanterApp #IKnowBall");

        return sb.ToString();
    }

    private static string ComposePostMatch(
        IReadOnlyList<BroadcastPick> picks,
        IReadOnlyDictionary<string, MatchStatisticsDto> stats)
    {
        var sb = new StringBuilder();
        var totalPoints = picks.Sum(p => p.PointsAwarded ?? 0);
        var hits = picks.Count(p => (p.PointsAwarded ?? 0) > 0);

        sb.AppendLine("[STUDIO — COLD OPEN]");
        sb.AppendLine(picks.Count == 0
            ? "Full time across the board — but the results desk is empty. Come back once your picks have gone the distance."
            : "Full time across the board. The graphics are up, the numbers are in — let's go through my card, call by call.");
        sb.AppendLine();

        if (picks.Count > 0)
        {
            sb.AppendLine("[SCOREBOARD]");
            sb.AppendLine($"{picks.Count} match{(picks.Count == 1 ? "" : "es")} reviewed · {hits}/{picks.Count} calls landed · {totalPoints} points banked.");
            sb.AppendLine();

            for (var i = 0; i < picks.Count; i++)
            {
                var pick = picks[i];
                var points = pick.PointsAwarded ?? 0;

                sb.AppendLine($"[SEGMENT {i + 1} — {pick.TeamA} v {pick.TeamB}]");
                sb.AppendLine($"The call: {pick.Prediction}. The result: {pick.ActualResult ?? "to be confirmed"}.");

                if (pick.MatchId is not null && stats.TryGetValue(pick.MatchId, out var s))
                {
                    sb.AppendLine(
                        $"Stats desk: possession {s.HomePossessionPercent}–{s.AwayPossessionPercent}, " +
                        $"shots {s.HomeShots}–{s.AwayShots} ({s.HomeShotsOnTarget}–{s.AwayShotsOnTarget} on target), " +
                        $"corners {s.HomeCorners}–{s.AwayCorners}.");
                }

                sb.AppendLine(points > 0
                    ? $"Verdict: the tape backs the take. +{points} points."
                    : "Verdict: football humbles us all. We go again.");
                sb.AppendLine();
            }

            sb.AppendLine("[ANALYSIS — to camera]");
            sb.AppendLine(hits == picks.Count
                ? "A clean sweep. The pundits get paid for this — I do it for the group chat."
                : hits == 0
                    ? "A rough night at the desk, but the receipts are public and so is the comeback."
                    : "A mixed bag — the kind of card that keeps the banter honest.");
            sb.AppendLine();
        }

        sb.AppendLine("[SIGN-OFF — to camera]");
        sb.AppendLine($"Same desk, next matchday. {Tagline}");
        sb.AppendLine();
        sb.Append("#WorldCup2026 #BanterApp #MatchdayRecap");

        return sb.ToString();
    }
}
