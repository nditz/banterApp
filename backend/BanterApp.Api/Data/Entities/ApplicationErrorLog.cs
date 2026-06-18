namespace BanterApp.Api.Data.Entities;

public class ApplicationErrorLog
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? RequestMethod { get; set; }
    public string? RequestPath { get; set; }
    public int? StatusCode { get; set; }
    public Guid? SyncRunId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
