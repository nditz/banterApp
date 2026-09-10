using System.Text;

namespace BanterApp.Api.Features.Studio;

public static class StudioContentPackComposer
{
    public static StudioContentPack Compose(
        StudioPackContext context,
        string contentType,
        string tone,
        Guid packId,
        DateTimeOffset createdAt)
    {
        var type = StudioContentCatalog.NormalizeType(contentType);
        var voice = StudioContentCatalog.NormalizeTone(tone);
        var title = BuildTitle(context, type);
        var hook = BuildHook(context, voice);
        var script = BuildScript(context, type, voice);
        var caption = BuildCaption(context, type, voice);
        var visual = BuildVisualPlan(context, type);
        var meme = type is "meme" or "caption" or "short"
            ? BuildMemeDirection(context, voice)
            : null;
        var hashtags = BuildHashtags(context);
        var imagePrompt = BuildImagePrompt(context);
        var voiceover = BuildVoiceoverPrompt(context, hook, script);

        var notes = context.SourceNotes.ToList();
        if (!notes.Exists(n => n.Contains("generated framing", StringComparison.OrdinalIgnoreCase)))
        {
            notes.Add("Hook, script, captions, and prompts are generated framing. Facts are listed separately.");
        }

        return new StudioContentPack(
            packId,
            type,
            voice,
            title,
            hook,
            script,
            context.Facts,
            visual,
            meme,
            caption,
            hashtags,
            imagePrompt,
            voiceover,
            notes,
            context.StoryKind,
            context.ReceiptId,
            context.MatchId,
            context.FeedItemId,
            createdAt);
    }

    private static string BuildTitle(StudioPackContext context, string type)
    {
        var fixture = context.TeamA is { Length: > 0 } && context.TeamB is { Length: > 0 }
            ? $"{context.TeamA} vs {context.TeamB}"
            : context.Headline;

        return type switch
        {
            "meme" => context.Scoreline is not null
                ? $"Receipt meme: {context.Scoreline}"
                : $"Meme concept: {Truncate(context.Headline, 72)}",
            "thread" => $"Thread: {fixture}",
            "carousel" => $"Carousel: {fixture}",
            "podcast" => $"Desk tape: {fixture}",
            "commentary" => $"Commentary: {fixture}",
            "caption" => Truncate(context.Headline, 80),
            _ => Truncate(context.Headline, 90)
        };
    }

    private static string BuildHook(StudioPackContext context, string tone)
    {
        var subject = context.Scoreline
            ?? (context.TeamA is { Length: > 0 } && context.TeamB is { Length: > 0 }
                ? $"{context.TeamA} vs {context.TeamB}"
                : context.Headline);

        var beaten = context.PunditTakes.FirstOrDefault(t => !t.WasCorrect);
        var beating = context.PunditTakes.FirstOrDefault(t => t.WasCorrect);

        var vsLine = beaten is not null && (context.PointsAwarded ?? 0) > 0
            ? $" You beat {beaten.Name}'s sourced pick."
            : beating is not null && (context.PointsAwarded ?? 0) <= 0
                ? $" {beating.Name}'s sourced pick landed."
                : "";

        var pickBit = context.UserPick is { Length: > 0 } ? $" You were on {context.UserPick}." : "";

        return tone switch
        {
            "funny" => $"{subject}.{pickBit}{vsLine} Clip it before the group chat does.",
            "ruthless" => $"{subject}.{pickBit}{vsLine} No soft landing.",
            "analytical" => $"{subject}.{pickBit}{vsLine} Here's the record, not the vibe.",
            "rant" => $"{subject}.{pickBit}{vsLine} I'm not wrapping this in politeness.",
            "victory_lap" => $"{subject}.{pickBit}{vsLine} That's a receipt.",
            "self_roast" => $"{subject}.{pickBit}{vsLine} If this aged badly, that's on me.",
            "pundit" => $"{subject}.{pickBit}{vsLine} From the desk — on the record.",
            _ => $"{subject}.{pickBit}{vsLine} Straight facts, then the frame."
        };
    }

    private static string BuildScript(StudioPackContext context, string type, string tone)
    {
        var open = ToneOpen(tone);
        var factsBlock = string.Join("\n", context.Facts.Select(f => $"- {f.Label}: {f.Value}"));
        var scoreNote = context.Scoreline is null
            ? "No recorded scoreline in this context — do not invent one on camera."
            : $"Recorded result: {context.Scoreline}.";
        var pickLine = context.UserPick is { Length: > 0 }
            ? $"Your pick: {context.UserPick}."
            : "No user pick in this context.";
        var punditLine = context.PunditTakes.Count == 0
            ? "No sourced pundit take on this story."
            : string.Join(" ", context.PunditTakes.Select(t =>
                $"{t.Name} sourced pick: {t.Prediction} ({(t.WasCorrect ? "landed" : "missed")})."));

        return type switch
        {
            "podcast" => JoinBlocks(
                $"[COLD OPEN — {open}]",
                context.Headline,
                scoreNote,
                pickLine,
                punditLine,
                "[FACTS — sourced]",
                factsBlock,
                "[CLOSE]",
                "Separate the record from the bit. Don't add a score, scorer, or quote that isn't listed."),
            "meme" => JoinBlocks(
                "[MEME CONCEPT]",
                $"{open} Top text: {Truncate(context.Headline, 48)}",
                $"Bottom text: {MemeBottom(context, tone)}",
                "[SOURCED FACTS]",
                factsBlock,
                scoreNote),
            "caption" => JoinBlocks(
                BuildCaption(context, type, tone),
                "[SOURCED FACTS]",
                factsBlock),
            "thread" => BuildThread(context, tone, factsBlock, scoreNote, pickLine, punditLine),
            "carousel" => JoinBlocks(
                "[SLIDE 1 — HOOK]",
                BuildHook(context, tone),
                "[SLIDE 2 — RESULT]",
                scoreNote,
                "[SLIDE 3 — YOUR PICK]",
                pickLine,
                "[SLIDE 4 — SOURCED PUNDIT]",
                punditLine,
                "[SLIDE 5 — FACTS]",
                factsBlock,
                "[SLIDE 6 — CTA]",
                "Export this pack. Don't invent extras."),
            "commentary" => JoinBlocks(
                $"[DESK — {open}]",
                context.Headline,
                scoreNote,
                pickLine,
                punditLine,
                "[RECORD]",
                factsBlock,
                "[FRAME]",
                "Talk only to what's on the receipt or sourced headline. If a number isn't listed, skip it."),
            _ => JoinBlocks(
                $"[SHORT / REEL — {open}]",
                $"0–2s: {BuildHook(context, tone)}",
                $"2–8s: {scoreNote} {pickLine}",
                $"8–14s: {punditLine}",
                "14–20s: Hold the receipt graphic. Cut before you add a fact that isn't sourced.",
                "[SOURCED FACTS]",
                factsBlock)
        };
    }

    private static string BuildThread(
        StudioPackContext context,
        string tone,
        string factsBlock,
        string scoreNote,
        string pickLine,
        string punditLine)
    {
        var sb = new StringBuilder();
        sb.AppendLine("1/ " + BuildHook(context, tone));
        sb.AppendLine();
        sb.AppendLine("2/ " + scoreNote);
        sb.AppendLine();
        sb.AppendLine("3/ " + pickLine);
        sb.AppendLine();
        sb.AppendLine("4/ " + punditLine);
        sb.AppendLine();
        sb.AppendLine("5/ Sourced facts:");
        sb.AppendLine(factsBlock);
        sb.AppendLine();
        sb.AppendLine($"6/ Tone: {tone}. If it isn't in the facts list, it didn't happen on Ball Takes.");
        return sb.ToString().Trim();
    }

    private static string BuildCaption(StudioPackContext context, string type, string tone)
    {
        var core = context.Scoreline ?? context.Headline;
        var pick = context.UserPick is { Length: > 0 } ? $" Pick: {context.UserPick}." : "";
        return type == "caption"
            ? $"{ToneOpen(tone)} {core}.{pick} #BallTakes"
            : $"{core}.{pick}";
    }

    private static string BuildMemeDirection(StudioPackContext context, string tone)
    {
        var beaten = context.PunditTakes.FirstOrDefault(t => !t.WasCorrect);
        if (beaten is not null && (context.PointsAwarded ?? 0) > 0)
        {
            return $"Split image: your pick vs {beaten.Name}'s sourced pick. No made-up quote. Tone: {tone}.";
        }

        if (context.Scoreline is not null)
        {
            return $"Scoreline graphic over {context.Scoreline}. Overlay the recorded pick only. Tone: {tone}.";
        }

        return $"Headline card from the sourced story. Do not draw a fake scoreboard. Tone: {tone}.";
    }

    private static IReadOnlyList<string> BuildVisualPlan(StudioPackContext context, string type)
    {
        var plan = new List<string>();
        if (context.TeamA is { Length: > 0 } && context.TeamB is { Length: > 0 })
        {
            plan.Add($"Club crests: {context.TeamA} vs {context.TeamB}.");
        }

        if (context.Scoreline is not null)
        {
            plan.Add($"Scoreline card: {context.Scoreline}.");
        }
        else
        {
            plan.Add("No scoreboard graphic — score is not in the sourced context.");
        }

        if (context.UserPick is { Length: > 0 })
        {
            plan.Add($"Your pick chip: {context.UserPick}.");
        }

        if (context.PunditTakes.Count > 0)
        {
            plan.Add("Side-by-side sourced pundit take vs your pick. Attribution on screen.");
        }

        if (context.FeedItemId is not null)
        {
            plan.Add("Headline card from the sourced feed item. Keep the source name visible.");
        }

        plan.Add(type switch
        {
            "carousel" => "Six-slide stills, high contrast, no extra stats.",
            "podcast" => "Waveform + desk title card. Cut B-roll only to listed facts.",
            "meme" => "One still. Big type. No invented player reaction.",
            _ => "Vertical 9:16. Text on the record only."
        });

        return plan;
    }

    private static IReadOnlyList<string> BuildHashtags(StudioPackContext context)
    {
        var tags = new List<string> { "#BallTakes", "#PremierLeague" };
        if (context.TeamA is { Length: > 0 })
        {
            tags.Add("#" + SanitizeTag(context.TeamA));
        }

        if (context.TeamB is { Length: > 0 })
        {
            tags.Add("#" + SanitizeTag(context.TeamB));
        }

        return tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string BuildImagePrompt(StudioPackContext context)
    {
        if (context.Scoreline is not null)
        {
            return $"Editorial football graphic, dark studio, green accent, scoreline exactly \"{context.Scoreline}\", no extra players or invented scorers.";
        }

        if (context.TeamA is { Length: > 0 } && context.TeamB is { Length: > 0 })
        {
            return $"Editorial football graphic, dark studio, green accent, fixture \"{context.TeamA} vs {context.TeamB}\", no scoreboard.";
        }

        return $"Editorial football headline card: \"{Truncate(context.Headline, 80)}\". No invented scoreline or player.";
    }

    private static string BuildVoiceoverPrompt(StudioPackContext context, string hook, string script) =>
        $"Read in Ball Takes desk energy. Start with: {hook} Only mention facts from the pack. Do not add scorers, minutes, or quotes. Script follows:\n{script}";

    private static string ToneOpen(string tone) => tone switch
    {
        "funny" => "group-chat energy",
        "ruthless" => "no mercy",
        "analytical" => "numbers first",
        "rant" => "full rant",
        "victory_lap" => "victory lap",
        "self_roast" => "self roast",
        "pundit" => "pundit desk",
        _ => "neutral explainer"
    };

    private static string MemeBottom(StudioPackContext context, string tone)
    {
        if (context.UserPick is { Length: > 0 } && context.Scoreline is not null)
        {
            return tone == "self_roast" && (context.PointsAwarded ?? 0) <= 0
                ? "I said that with my chest"
                : context.UserPick;
        }

        return "That's the record";
    }

    private static string SanitizeTag(string name)
    {
        var chars = name.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "PL" : new string(chars);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)].TrimEnd() + "…";

    private static string JoinBlocks(params string?[] parts)
    {
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine(part);
        }

        return sb.ToString().Trim();
    }
}
