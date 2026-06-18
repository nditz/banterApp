using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Pundits;

public sealed record PunditDisplay(
    string DisplayName,
    string DeskLabel,
    string? Archetype,
    string? ParodyCue,
    string? StyleSlug,
    bool IsFictionalPersona,
    string? SourceUrl,
    string? SourcePlatform,
    string AttributionNote,
    string AvatarSeed);

/// <summary>
/// Resolves pundit labels: obvious parody personas by default, credited takes when licensed/scraped.
/// </summary>
public static class PunditDisplayResolver
{
    public const string PersonaDisclaimer =
        "Obvious parody desk — not affiliated with any real person, podcast, or broadcaster.";

    public static PunditDisplay Resolve(Pundit pundit, PunditPrediction? prediction = null)
    {
        var sourceUrl = FirstUrl(prediction?.SourceUrl, pundit.SourceUrl);
        var sourcePlatform = PunditSourcePlatform.Normalize(prediction?.SourceType);
        var avatarSeed = string.IsNullOrWhiteSpace(pundit.AvatarSeed)
            ? pundit.Id.ToString("N")
            : pundit.AvatarSeed;

        return pundit.AttributionMode switch
        {
            PunditAttributionMode.Licensed when sourceUrl is not null =>
                new(
                    pundit.Name,
                    pundit.Organization,
                    NullIfWhite(pundit.Archetype),
                    BuildLicensedParodyCue(pundit, prediction),
                    NullIfWhite(pundit.StyleSlug),
                    IsFictionalPersona: false,
                    sourceUrl,
                    sourcePlatform,
                    FormatLicensedNote(pundit, sourcePlatform, sourceUrl),
                    avatarSeed),

            PunditAttributionMode.Licensed =>
                new(
                    pundit.Name,
                    pundit.Organization,
                    NullIfWhite(pundit.Archetype),
                    BuildLicensedParodyCue(pundit, prediction),
                    NullIfWhite(pundit.StyleSlug),
                    IsFictionalPersona: false,
                    sourceUrl,
                    sourcePlatform,
                    $"Attributed to {pundit.Name} · {pundit.Organization}.",
                    avatarSeed),

            PunditAttributionMode.PublicationOnly =>
                new(
                    $"{pundit.Organization} desk",
                    pundit.Organization,
                    NullIfWhite(pundit.Archetype),
                    NullIfWhite(pundit.ParodyCue),
                    NullIfWhite(pundit.StyleSlug),
                    IsFictionalPersona: false,
                    sourceUrl,
                    sourcePlatform,
                    sourceUrl is not null
                        ? $"Aggregated from {pundit.Organization}."
                        : $"Desk pick from {pundit.Organization}.",
                    avatarSeed),

            _ => new(
                pundit.Name,
                pundit.Organization,
                NullIfWhite(pundit.Archetype),
                NullIfWhite(pundit.ParodyCue),
                NullIfWhite(pundit.StyleSlug),
                IsFictionalPersona: true,
                sourceUrl,
                sourcePlatform,
                PersonaDisclaimer,
                avatarSeed),
        };
    }

    public static string FeedTitle(PunditDisplay display, bool hit) =>
        hit
            ? $"{display.DisplayName} called it"
            : $"Desk roast: {display.DisplayName}";

    public static string FeedBody(
        PunditDisplay display,
        string prediction,
        string scoreline,
        bool hit)
    {
        var cue = string.IsNullOrWhiteSpace(display.ParodyCue)
            ? display.Archetype ?? "The desk"
            : display.ParodyCue;

        return hit
            ? $"{cue} at {display.DeskLabel} backed {prediction} before kickoff. Final: {scoreline}."
            : $"{display.DisplayName} ({cue}) said {prediction}. Reality said {scoreline}.";
    }

    public static string FormatSourceLine(PunditDisplay display)
    {
        if (!string.IsNullOrWhiteSpace(display.SourcePlatform) && !string.IsNullOrWhiteSpace(display.SourceUrl))
        {
            return $"{display.DeskLabel} · via {display.SourcePlatform}";
        }

        return string.IsNullOrWhiteSpace(display.ParodyCue)
            ? display.DeskLabel
            : $"{display.DeskLabel} · {display.ParodyCue}";
    }

    private static string FormatLicensedNote(Pundit pundit, string? platform, string sourceUrl) =>
        platform switch
        {
            PunditSourcePlatform.YouTube => $"Take sourced from YouTube · {pundit.Organization}.",
            PunditSourcePlatform.Podcast => $"Take sourced from podcast · {pundit.Organization}.",
            _ => $"Prediction cited from {pundit.Organization}.",
        };

    private static string? BuildLicensedParodyCue(Pundit pundit, PunditPrediction? prediction)
    {
        if (!string.IsNullOrWhiteSpace(pundit.ParodyCue))
        {
            return pundit.ParodyCue;
        }

        if (!string.IsNullOrWhiteSpace(prediction?.Speaker))
        {
            return $"Sourced take · {prediction.Speaker.Trim()}";
        }

        return NullIfWhite(pundit.Archetype);
    }

    private static string? FirstUrl(params string?[] urls)
    {
        foreach (var url in urls)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                return url.Trim();
            }
        }

        return null;
    }

    private static string? NullIfWhite(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
