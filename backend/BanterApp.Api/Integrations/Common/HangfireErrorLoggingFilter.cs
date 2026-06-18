using BanterApp.Api.Services;
using Hangfire.Server;

namespace BanterApp.Api.Integrations.Common;

public sealed class HangfireErrorLoggingFilter(IServiceScopeFactory scopeFactory) : IServerFilter
{
    public void OnPerforming(PerformingContext context)
    {
    }

    public void OnPerformed(PerformedContext context)
    {
        if (context.Exception is null)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var errorLogger = scope.ServiceProvider.GetRequiredService<IApplicationErrorLogger>();
        var jobName = context.BackgroundJob?.Job?.Type?.Name ?? "unknown";

        errorLogger.LogExceptionAsync(
            "background",
            context.Exception,
            category: jobName,
            ct: context.CancellationToken.ShutdownToken).GetAwaiter().GetResult();
    }
}
