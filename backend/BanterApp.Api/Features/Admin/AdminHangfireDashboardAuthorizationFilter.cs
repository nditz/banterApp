using BanterApp.Api.Common;
using BanterApp.Api.Features.Admin;
using Hangfire.Dashboard;

namespace BanterApp.Api.Features.Admin;

public sealed class AdminHangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var adminAuth = http.RequestServices.GetRequiredService<IAdminAuthorizationService>();
        var userContext = http.RequestServices.GetRequiredService<IUserContext>();
        return adminAuth.IsAdminAsync(userContext, http).GetAwaiter().GetResult();
    }
}
