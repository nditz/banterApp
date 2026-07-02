using BanterApp.Api.Features.Pundits;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Features.Ai;

/// <summary>
/// Template fallback for pundit scripts when OpenAI is unavailable (stub provider).
/// </summary>
public static class PunditScriptComposer
{
    public static string Compose(
        MatchScriptContext context,
        PunditPersonaSeed persona,
        string phase,
        VideoScriptDuration duration)
    {
        var style = PunditStyleProfiles.Get(persona.StyleSlug);
        var match = context.Match;
        var home = match.HomeTeam.Name;
        var away = match.AwayTeam.Name;
        var isPostMatch = string.Equals(phase, "post_match", StringComparison.OrdinalIgnoreCase);
        var scoreLine = match.HomeScore.HasValue && match.AwayScore.HasValue
            ? $"{home} {match.HomeScore}-{match.AwayScore} {away}"
            : $"{home} vs {away}";

        var keyEvent = context.Events.FirstOrDefault();
        var keyPlayer = keyEvent?.PlayerName
            ?? context.Lineups.FirstOrDefault(p => !p.IsSubstitute)?.PlayerName
            ?? "the key man";
        var statsLine = BuildStatsLine(context.Statistics, home, away);
        var setting = style.DefaultSceneSetting;

        var scenes = new List<string>
        {
            BuildScene(1, "INTRODUCTION", "3-5s", setting, "split-screen team crests and presenter",
                "excited", "close-up",
                isPostMatch
                    ? $"Right then — {scoreLine}. {persona.Name} here, and we need to talk about what we just witnessed."
                    : $"Alright, settle in — {home} against {away}. {persona.Name}, and this one has everything on the line."),
            BuildScene(2, "MATCH CONTEXT", "10-15s", "studio", $"venue exterior of {match.Venue}, stage graphic",
                "analytical", "wide",
                isPostMatch
                    ? $"Full time at {match.Venue}. {match.Stage} — {match.Group}. {statsLine} This result matters."
                    : $"We're at {match.Venue} for {match.Stage}, {match.Group}. The stakes are real — form, pride, and a place in the narrative."),
            BuildScene(3, isPostMatch ? "KEY MOMENT" : "MATCHUP TO WATCH", "10-15s",
                isPostMatch ? "replay" : "pitch-side",
                isPostMatch ? "slow-motion replay of pivotal moment" : "tactical wide shot of both teams",
                "serious", "slow-mo",
                isPostMatch && keyEvent is not null
                    ? $"The moment that changed it — minute {keyEvent.Minute}, {keyEvent.PlayerName ?? "the player"} with the {keyEvent.Type.ToLowerInvariant()}. That's the clip."
                    : $"Watch the battle in midfield — whoever controls the tempo between {home} and {away} wins this."),
            BuildScene(4, "PLAYER PERFORMANCE", "10-15s", "replay", $"close-up on {keyPlayer}",
                "passionate", "close-up",
                isPostMatch
                    ? $"{keyPlayer} — take a bow. That performance told the story of the night."
                    : $"Keep your eye on {keyPlayer}. If they turn up, {home} or {away} have a live weapon."),
            BuildScene(5, "TACTICAL ANALYSIS", "12-18s", "studio", "tactical board with formation arrows",
                "analytical", "overhead",
                string.IsNullOrWhiteSpace(statsLine)
                    ? "Shape, pressing triggers, and who commits first — that's the tactical story."
                    : $"Look at the numbers: {statsLine}. That tells you everything about who imposed their game."),
            BuildScene(6, "TURNING POINT", "8-12s", "replay", "highlight reel montage",
                "shocked", "wide",
                isPostMatch && context.Events.Count > 1
                    ? $"Then it flipped — minute {context.Events.Skip(1).First().Minute}. Game changed. No coming back from that momentum shift."
                    : "One moment — a set piece, a press, a individual bit of quality — that's your turning point. Mark it."),
            BuildScene(7, "CONCLUSION", "8-12s", "studio", "presenter to camera, score graphic",
                "serious", "close-up",
                isPostMatch
                    ? $"So where does that leave us? {scoreLine} — and the table doesn't lie. On to the next one."
                    : $"My read? Tight, tense, and whoever handles the big moments wins. We'll find out soon enough."),
            BuildScene(8, "CLOSING", "3-5s", setting, "presenter sign-off, channel logo",
                "passionate", "close-up",
                style.SignOffStyle),
        };

        var header = $"--- PUNDIT SCRIPT: {persona.Name} | {scoreLine} | {(int)duration}s {(isPostMatch ? "POST-MATCH" : "PRE-MATCH")} ---";
        return header + "\n\n" + string.Join("\n\n", scenes);
    }

    private static string BuildStatsLine(MatchStatisticsDto? stats, string home, string away)
    {
        if (stats is null)
        {
            return string.Empty;
        }

        return
            $"{home} {stats.HomePossessionPercent}% possession, {stats.HomeShots} shots ({stats.HomeShotsOnTarget} on target) · " +
            $"{away} {stats.AwayPossessionPercent}% possession, {stats.AwayShots} shots ({stats.AwayShotsOnTarget} on target).";
    }

    private static string BuildScene(
        int number,
        string title,
        string duration,
        string scene,
        string visual,
        string tone,
        string camera,
        string dialogue) =>
        $"SCENE {number}: {title} ({duration})\n" +
        $"[Scene: {scene}]\n" +
        $"[Visual: {visual}]\n" +
        $"[Tone: {tone}]\n" +
        $"[Camera: {camera}]\n" +
        $"DIALOGUE: {dialogue}";
}
