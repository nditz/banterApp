namespace BanterApp.Api.Common;

public interface IUserContext
{
    Guid? UserId { get; }
    Guid? AnonymousUserId { get; }
    string? AnonymousCookieId { get; }
    bool IsAuthenticated { get; }
    bool IsAnonymous { get; }
}

public sealed class UserContext : IUserContext
{
    public Guid? UserId { get; set; }
    public Guid? AnonymousUserId { get; set; }
    public string? AnonymousCookieId { get; set; }
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsAnonymous => !IsAuthenticated && AnonymousUserId.HasValue;
}
