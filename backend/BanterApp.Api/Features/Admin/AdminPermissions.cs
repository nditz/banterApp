namespace BanterApp.Api.Features.Admin;

/// <summary>
/// Named admin capabilities. Every permission currently resolves to the platform-admin
/// check; the constants exist so a permission table can be introduced later without
/// changing endpoint signatures.
/// </summary>
public static class AdminPermissions
{
    public const string AnalyticsView = "Admin.Analytics.View";
    public const string UsersView = "Admin.Users.View";
    public const string UsersManage = "Admin.Users.Manage";
    public const string JobsView = "Admin.Jobs.View";
    public const string JobsExecute = "Admin.Jobs.Execute";
    public const string CacheView = "Admin.Cache.View";
    public const string CacheInvalidate = "Admin.Cache.Invalidate";
    public const string AuditView = "Admin.Audit.View";

    public static IReadOnlyList<string> All { get; } =
    [
        AnalyticsView,
        UsersView,
        UsersManage,
        JobsView,
        JobsExecute,
        CacheView,
        CacheInvalidate,
        AuditView
    ];

    public static bool IsKnown(string permission) =>
        All.Any(p => string.Equals(p, permission, StringComparison.Ordinal));
}
