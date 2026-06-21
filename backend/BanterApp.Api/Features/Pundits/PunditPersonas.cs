using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Pundits;

/// <summary>
/// Recognizable parody desks — dummy names + clear "who this evokes" cues for users.
/// Not affiliated with real people or broadcasters; licensed mode uses real attribution when scraped.
/// </summary>
public sealed record PunditPersonaSeed(
    Guid Id,
    string Name,
    string Organization,
    string Archetype,
    string ParodyCue,
    string StyleSlug,
    string AvatarSeed);

public static class PunditPersonas
{
    public static readonly PunditPersonaSeed[] Defaults =
    [
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111101"),
            "Side-View Gary",
            "Rant & Chips TV",
            "Touchline rage merchant",
            "Parody · the touchline close-up guy (Neville energy)",
            "touchline-uk",
            "side-view-gary"),
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111102"),
            "Sofa Captain Rio",
            "Sofa Champions",
            "Ex-pro captain couch takes",
            "Parody · the velvet sofa legend (Rio energy)",
            "ex-pro-couch",
            "sofa-captain-rio"),
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111103"),
            "Screamin' Stephen",
            "First Controversy Desk",
            "Loudest desk in the building",
            "Parody · controversy merchant (Stephen A. energy)",
            "hot-take-desk",
            "screamin-stephen"),
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111104"),
            "Le Prof Henri",
            "Class on Grass",
            "Silky studio icon",
            "Parody · the smooth studio legend (Henry energy)",
            "silky-studio",
            "le-prof-henri"),
    ];

    public static PunditPersonaSeed? FindByStyleSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return Defaults.FirstOrDefault(p =>
            string.Equals(p.StyleSlug, slug.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static Pundit ToEntity(PunditPersonaSeed seed) =>
        new()
        {
            Id = seed.Id,
            Kind = PunditKind.Persona,
            Name = seed.Name,
            NormalizedName = seed.Name.Trim().ToLowerInvariant(),
            Organization = seed.Organization,
            Archetype = seed.Archetype,
            ParodyCue = seed.ParodyCue,
            StyleSlug = seed.StyleSlug,
            AttributionMode = PunditAttributionMode.Persona,
            AvatarSeed = seed.AvatarSeed,
        };

    public static void Apply(Pundit pundit, PunditPersonaSeed seed)
    {
        pundit.Name = seed.Name;
        pundit.Organization = seed.Organization;
        pundit.Archetype = seed.Archetype;
        pundit.ParodyCue = seed.ParodyCue;
        pundit.StyleSlug = seed.StyleSlug;
        pundit.AttributionMode = PunditAttributionMode.Persona;
        pundit.AvatarSeed = seed.AvatarSeed;
    }
}
