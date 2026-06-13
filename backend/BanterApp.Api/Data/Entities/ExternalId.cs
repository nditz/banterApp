namespace BanterApp.Api.Data.Entities;

public class ExternalId
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ProviderExternalId { get; set; } = string.Empty;
    public DateTimeOffset LastSeenAt { get; set; }
    public string? RawPayloadHash { get; set; }
}
