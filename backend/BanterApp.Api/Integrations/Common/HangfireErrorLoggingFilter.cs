using BanterApp.Api.Common;
using BanterApp.Api.Integrations.Jobs;
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
        var errorTracking = scope.ServiceProvider.GetRequiredService<IErrorTrackingService>();
        var hangfireJobId = context.BackgroundJob?.Job?.Type?.Name ?? "unknown";
        var jobDefinition = JobRegistry.FindByHangfireId(hangfireJobId);
        var jobKey = jobDefinition?.Key ?? hangfireJobId;

        errorTracking.TrackExceptionAsync(new ErrorTrackRequest
        {
            Source = "job",
            ErrorCode = ErrorCodes.JobFailed,
            MessageSafe = "A background task failed.",
            Severity = "error",
            JobKey = jobKey,
            Provider = "job",
            IsRetryable = true,
            Metadata = new Dictionary<string, object?>
            {
                ["hangfire_job_id"] = hangfireJobId,
                ["retry_count"] = 0
            }
        }, context.Exception, context.CancellationToken.ShutdownToken).GetAwaiter().GetResult();
    }
}
