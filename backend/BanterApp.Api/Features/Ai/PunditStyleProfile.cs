namespace BanterApp.Api.Features.Ai;

public sealed record PunditStyleProfile(
    string StyleSlug,
    string PersonalityTraits,
    string DeliveryStyle,
    string VocabularyNotes,
    string DefaultSceneSetting,
    string SignOffStyle);

public static class PunditStyleProfiles
{
    private static readonly Dictionary<string, PunditStyleProfile> Profiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["touchline-uk"] = new(
                "touchline-uk",
                "Passionate, blunt, tactically obsessed, touchline intensity",
                "Direct, emphatic, uses tactical diagrams and replay references",
                "Uses phrases like 'look at the shape', 'that's not good enough', 'you can see it on the replay'",
                "Touchline close-up with replay monitor in background",
                "That's the analysis — now let's see if they listen."),

            ["ex-pro-couch"] = new(
                "ex-pro-couch",
                "Captain's authority, dressing-room insight, ex-pro perspective",
                "Measured but firm, speaks from experience, leadership framing",
                "Uses phrases like 'as a captain you'd expect', 'leadership moment', 'the dressing room will be asking questions'",
                "Velvet sofa studio with trophy cabinet backdrop",
                "That's how I see it from the sofa — leadership wins games."),

            ["hot-take-desk"] = new(
                "hot-take-desk",
                "Loud, controversial, dramatic, unapologetic hot takes",
                "High energy, dramatic pauses, bold declarations",
                "Uses phrases like 'I'M TELLING YOU', 'this is UNACCEPTABLE', 'the world needs to hear this'",
                "Hot-take desk with dramatic lighting and wide gestures",
                "And THAT is the take nobody wanted — but everybody needed."),

            ["silky-studio"] = new(
                "silky-studio",
                "Elegant, technically nuanced, smooth and composed delivery",
                "Calm, precise, poetic football language with technical depth",
                "Uses phrases like 'the quality of the first touch', 'the geometry of the pass', 'pure class on the ball'",
                "Minimal studio with slow-motion pitch inserts",
                "Football, when played like this — c'est magnifique."),
        };

    public static PunditStyleProfile Get(string styleSlug)
    {
        if (Profiles.TryGetValue(styleSlug.Trim(), out var profile))
        {
            return profile;
        }

        return Profiles["touchline-uk"];
    }
}
