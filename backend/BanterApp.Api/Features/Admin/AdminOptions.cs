namespace BanterApp.Api.Features.Admin;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public List<string> AllowedEmails { get; set; } = [];
    public List<string> AllowedUserIds { get; set; } = [];
    public bool ExposeErrorDetail { get; set; }
}
