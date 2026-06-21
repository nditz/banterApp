using BanterApp.Api.Common;
using Microsoft.AspNetCore.Authorization;

namespace BanterApp.Api.Features.Admin;

public sealed class AdminRequirement : IAuthorizationRequirement;

public sealed class AdminAuthorizationHandler(IAdminAuthorizationService adminAuth)
    : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement)
    {
        if (context.Resource is not HttpContext http)
        {
            return;
        }

        var userContext = http.RequestServices.GetRequiredService<IUserContext>();
        if (await adminAuth.IsAdminAsync(userContext, http))
        {
            context.Succeed(requirement);
        }
    }
}
