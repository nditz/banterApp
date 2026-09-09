using System.Reflection;
using BanterApp.Api.Integrations.Jobs;
using Hangfire.Server;

namespace BanterApp.Api.Integrations.Common;

/// <summary>
/// Flows the current Hangfire job identity across async calls so providers can tag errors
/// with <c>jobKey</c> even when they do not receive the job as a parameter.
/// </summary>
public static class HangfireJobAmbientContext
{
    private static readonly AsyncLocal<State?> CurrentValue = new();

    public sealed record State(string JobKey, string HangfireJobId, string JobTypeName);

    public static State? Current => CurrentValue.Value;

    public static void Set(PerformingContext context)
    {
        var jobType = context.BackgroundJob?.Job?.Type;
        var typeName = jobType?.Name ?? "unknown";
        var hangfireJobId = ResolveHangfireJobId(jobType, typeName);
        var jobKey = JobRegistry.FindByHangfireId(hangfireJobId)?.Key
                     ?? JobRegistry.FindByKey(hangfireJobId)?.Key
                     ?? hangfireJobId;

        CurrentValue.Value = new State(jobKey, hangfireJobId, typeName);
    }

    public static void Clear() => CurrentValue.Value = null;

    public static string ResolveHangfireJobId(Type? jobType, string typeName)
    {
        if (jobType is not null)
        {
            var jobIdField = jobType.GetField("JobId", BindingFlags.Public | BindingFlags.Static);
            if (jobIdField?.GetValue(null) is string jobId && !string.IsNullOrWhiteSpace(jobId))
            {
                return jobId;
            }
        }

        return typeName;
    }
}
