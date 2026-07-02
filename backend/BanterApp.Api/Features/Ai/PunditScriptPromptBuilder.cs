using System.Text;
using System.Text.Json;
using BanterApp.Api.Features.Pundits;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Features.Ai;

public static class PunditScriptPromptBuilder
{
    public static string BuildSystemPrompt(string basePrompt, PunditPersonaSeed persona, PunditStyleProfile style)
    {
        var sb = new StringBuilder(basePrompt.Trim());
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine($"PERSONA: {persona.Name} ({persona.Organization})");
        sb.AppendLine($"ARCHETYPE: {persona.Archetype}");
        sb.AppendLine($"PERSONALITY: {style.PersonalityTraits}");
        sb.AppendLine($"DELIVERY: {style.DeliveryStyle}");
        sb.AppendLine($"VOCABULARY: {style.VocabularyNotes}");
        sb.AppendLine($"DEFAULT SETTING: {style.DefaultSceneSetting}");
        sb.AppendLine($"SIGN-OFF STYLE: {style.SignOffStyle}");
        return sb.ToString();
    }

    public static string BuildUserPrompt(
        MatchScriptContext context,
        PunditPersonaSeed persona,
        string phase,
        VideoScriptDuration duration)
    {
        var match = context.Match;
        var isPostMatch = string.Equals(phase, "post_match", StringComparison.OrdinalIgnoreCase);

        var contextPayload = new
        {
            phase,
            duration_seconds = (int)duration,
            persona = new
            {
                persona.Name,
                persona.Organization,
                persona.Archetype,
                persona.StyleSlug,
            },
            match = new
            {
                match.Id,
                home_team = match.HomeTeam.Name,
                away_team = match.AwayTeam.Name,
                home_code = match.HomeTeam.Code,
                away_code = match.AwayTeam.Code,
                kickoff_utc = match.KickoffUtc,
                stage = match.Stage,
                group = match.Group,
                venue = match.Venue,
                status = match.Status,
                home_score = match.HomeScore,
                away_score = match.AwayScore,
            },
            statistics = context.Statistics is null
                ? null
                : new
                {
                    possession = $"{context.Statistics.HomePossessionPercent}-{context.Statistics.AwayPossessionPercent}%",
                    shots = $"{context.Statistics.HomeShots}-{context.Statistics.AwayShots}",
                    shots_on_target = $"{context.Statistics.HomeShotsOnTarget}-{context.Statistics.AwayShotsOnTarget}",
                    corners = $"{context.Statistics.HomeCorners}-{context.Statistics.AwayCorners}",
                    fouls = $"{context.Statistics.HomeFouls}-{context.Statistics.AwayFouls}",
                    yellow_cards = $"{context.Statistics.HomeYellowCards}-{context.Statistics.AwayYellowCards}",
                    red_cards = $"{context.Statistics.HomeRedCards}-{context.Statistics.AwayRedCards}",
                },
            events = context.Events.Select(e => new
            {
                e.Minute,
                e.Type,
                e.TeamCode,
                e.PlayerName,
                e.Detail,
            }),
            lineups = context.Lineups
                .Where(p => !p.IsSubstitute)
                .Take(22)
                .Select(p => new
                {
                    p.TeamCode,
                    p.PlayerName,
                    p.Position,
                    p.ShirtNumber,
                }),
            standings = context.Standings.Take(8).Select(s => new
            {
                team_code = s.Team.Code,
                team_name = s.Team.Name,
                s.Played,
                s.Points,
                s.GoalDifference,
            }),
        };

        var json = JsonSerializer.Serialize(contextPayload, new JsonSerializerOptions { WriteIndented = true });

        var sb = new StringBuilder();
        sb.AppendLine($"Generate a {(int)duration}-second pundit analysis script for this {(isPostMatch ? "post-match" : "pre-match")} segment.");
        sb.AppendLine();
        sb.AppendLine("SCENE GUIDANCE:");
        if (isPostMatch)
        {
            sb.AppendLine("- SCENE 1: Hook with final score and immediate reaction.");
            sb.AppendLine("- SCENE 2: Recap match significance using stage, venue, group context.");
            sb.AppendLine("- SCENE 3: Analyse the first pivotal event from the events list (or key stat if no events).");
            sb.AppendLine("- SCENE 4: Highlight standout player from events or dominant team stats.");
            sb.AppendLine("- SCENE 5: Deep tactical breakdown using possession, shots, and shape indicators from stats.");
            sb.AppendLine("- SCENE 6: Identify the game-changing turning point from events.");
            sb.AppendLine("- SCENE 7: Summarise and look ahead to what's next in the competition.");
        }
        else
        {
            sb.AppendLine("- SCENE 1: Attention-grabbing intro to the fixture.");
            sb.AppendLine("- SCENE 2: Explain match stakes using stage, group, venue, and standings.");
            sb.AppendLine("- SCENE 3: Preview the key tactical matchup to watch.");
            sb.AppendLine("- SCENE 4: Spotlight a key player from lineups or squad context.");
            sb.AppendLine("- SCENE 5: Formation and tactical shape preview.");
            sb.AppendLine("- SCENE 6: X-factor or wildcard that could decide the game.");
            sb.AppendLine("- SCENE 7: Prediction and what to watch for at kickoff.");
        }

        sb.AppendLine("- SCENE 8: Memorable persona sign-off.");
        sb.AppendLine();
        sb.AppendLine("MATCH CONTEXT (ground truth — do not invent facts beyond this):");
        sb.AppendLine(json);

        return sb.ToString();
    }
}
