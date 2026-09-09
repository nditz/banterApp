namespace BanterApp.Api.Data.Entities;

/// <summary>
/// User or anonymous session following an existing <see cref="Pundit"/>.
/// Does not introduce a second pundit directory.
/// </summary>
public class PunditFollow
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AnonymousUserId { get; set; }
    public Guid PunditId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
    public AnonymousUser? AnonymousUser { get; set; }
    public Pundit Pundit { get; set; } = null!;
}
