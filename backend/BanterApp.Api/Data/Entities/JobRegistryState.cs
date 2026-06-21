namespace BanterApp.Api.Data.Entities;

public class JobRegistryState
{
    public Guid Id { get; set; }
    public string JobKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Paused { get; set; }
    public string? Schedule { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
