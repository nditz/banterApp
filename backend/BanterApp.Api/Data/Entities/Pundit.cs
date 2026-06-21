namespace BanterApp.Api.Data.Entities;

public class Pundit
{
    public Guid Id { get; set; }
    public PunditKind Kind { get; set; } = PunditKind.Persona;
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Organization { get; set; } = string.Empty;
    /// <summary>Short vibe label shown under the persona name.</summary>
    public string Archetype { get; set; } = string.Empty;
    /// <summary>User-facing parody hint so players know who the desk evokes, e.g. "Parody · Neville energy".</summary>
    public string ParodyCue { get; set; } = string.Empty;
    /// <summary>Stable slug for matching scraped podcast/YouTube feeds to this desk.</summary>
    public string StyleSlug { get; set; } = string.Empty;
    public PunditAttributionMode AttributionMode { get; set; } = PunditAttributionMode.Persona;
    /// <summary>DiceBear / avatar seed for fictional desk likeness.</summary>
    public string AvatarSeed { get; set; } = string.Empty;
    /// <summary>Optional outlet-level source when licensed aggregation is used.</summary>
    public string? SourceUrl { get; set; }

    public ICollection<PunditPrediction> Predictions { get; set; } = [];
    public ICollection<PunditOpinion> Opinions { get; set; } = [];
}
