namespace BanterApp.Api.Integrations.Banter;

public static class PredefinedBanterConcepts
{
    private static readonly IReadOnlyDictionary<BanterScenario, string[]> Map =
        new Dictionary<BanterScenario, string[]>
        {
            [BanterScenario.Overconfidence] =
            [
                "delete tweet reaction",
                "I have made a huge mistake",
                "walk of shame",
                "pretending nothing happened",
                "hiding embarrassment",
                "too confident then fails",
                "laughing then crying",
                "cringe reaction"
            ],
            [BanterScenario.PredictionAgedBadly] =
            [
                "prediction aged poorly",
                "delete tweet reaction",
                "this aged terribly",
                "facepalm reaction",
                "awkward silence meme",
                "regret face",
                "oh no reaction",
                "big mistake reaction"
            ],
            [BanterScenario.PredictionNailedIt] =
            [
                "told you so reaction",
                "mic drop",
                "smug celebration",
                "chef kiss reaction",
                "perfect prediction vibe",
                "flexing celebration",
                "genius reaction",
                "nail on the head"
            ],
            [BanterScenario.Heartbreak] =
            [
                "heartbroken reaction",
                "crying meme",
                "devastated sports fan",
                "why me reaction",
                "pain meme",
                "emotional damage",
                "broken heart reaction",
                "disappointment face"
            ],
            [BanterScenario.DominantWin] =
            [
                "absolute domination celebration",
                "too easy celebration",
                "trophy flex",
                "unstoppable vibe",
                "steamroll celebration",
                "boss mode reaction",
                "clap clap roast",
                "victory lap meme"
            ],
            [BanterScenario.SmugWinner] =
            [
                "smug smile reaction",
                "knowing look meme",
                "quiet flex",
                "told you so smirk",
                "cool celebration",
                "main character walk",
                "smooth celebration",
                "subtle roast"
            ],
            [BanterScenario.LuckyEscape] =
            [
                "narrow escape reaction",
                "phew meme",
                "that was close",
                "sweating nervous then relief",
                "barely survived",
                "clutch save vibe",
                "lucky break meme",
                "close call reaction"
            ],
            [BanterScenario.UnderdogUpset] =
            [
                "underdog celebration",
                "shock upset reaction",
                "impossible win meme",
                "plot twist celebration",
                "giant killing vibe",
                "shocked then cheering",
                "against all odds",
                "surprise victory"
            ],
            [BanterScenario.DrawFrustration] =
            [
                "frustrated draw reaction",
                "almost but not quite",
                "sigh meme",
                "so close reaction",
                "annoyed shrug",
                "unfinished business vibe",
                "tied game frustration",
                "meh reaction"
            ],
            [BanterScenario.LastMinuteDrama] =
            [
                "last minute drama",
                "stoppage time chaos",
                "heart attack sports moment",
                "sudden twist reaction",
                "screaming celebration",
                "unbelievable finish",
                "nail biting reaction",
                "chaos erupting"
            ],
            [BanterScenario.BottledIt] =
            [
                "bottled it reaction",
                "choke meme",
                "how did they lose that",
                "collapse reaction",
                "from winning to losing",
                "disaster finish",
                "face in hands",
                "unbelievable collapse"
            ],
            [BanterScenario.Comeback] =
            [
                "comeback celebration",
                "never give up vibe",
                "turnaround moment",
                "from behind to winning",
                "resurrection celebration",
                "momentum shift meme",
                "believe again reaction",
                "epic comeback"
            ],
            [BanterScenario.RivalMockery] =
            [
                "rival banter roast",
                "point and laugh meme",
                "mocking celebration",
                "owning the rivals",
                "spicy roast reaction",
                "trash talk vibe",
                "laughing at rivals",
                "ratio energy"
            ],
            [BanterScenario.RefereeControversy] =
            [
                "ref controversy reaction",
                "disbelief sports meme",
                "are you serious reaction",
                "angry rant vibe",
                "robbed reaction",
                "bad call outrage",
                "hands on head shock",
                "injustice meme"
            ],
            [BanterScenario.GenericWin] =
            [
                "football win celebration",
                "soccer celebration meme",
                "happy sports fan",
                "victory dance",
                "cheering crowd vibe",
                "big win energy",
                "party celebration",
                "yes reaction"
            ],
            [BanterScenario.GenericDraw] =
            [
                "draw reaction meme",
                "shrug sports fan",
                "mixed feelings reaction",
                "okay then meme",
                "stalemate vibe",
                "neutral reaction",
                "unsatisfied fan",
                "meh sports"
            ],
            [BanterScenario.GenericLoss] =
            [
                "disappointed football fan",
                "loss reaction meme",
                "sad sports fan",
                "defeat face",
                "tough day reaction",
                "head in hands",
                "painful loss vibe",
                "why though meme"
            ],
            [BanterScenario.GenericNews] =
            [
                "breaking news reaction meme",
                "sports pundit meme",
                "football meme",
                "shocked reaction",
                "group chat reaction",
                "timeline reaction",
                "hot take vibe",
                "debate energy"
            ]
        };

    public static IReadOnlyList<BanterConcept> ForScenario(BanterScenario scenario, int count)
    {
        if (!Map.TryGetValue(scenario, out var phrases))
        {
            phrases = Map[BanterScenario.GenericNews];
        }

        return phrases
            .Take(Math.Max(1, count))
            .Select(p => new BanterConcept(p))
            .ToList();
    }
}
