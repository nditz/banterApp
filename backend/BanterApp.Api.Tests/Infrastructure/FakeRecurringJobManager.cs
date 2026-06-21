using System.Linq.Expressions;
using Hangfire;
using Hangfire.Common;

namespace BanterApp.Api.Tests.Infrastructure;

internal sealed class FakeRecurringJobManager : IRecurringJobManager
{
    public List<string> TriggeredJobIds { get; } = [];

    public void AddOrUpdate(string recurringJobId, Job job, string cronExpression) { }

    public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options) { }

    public void AddOrUpdate(string recurringJobId, Job job, RecurringJobOptions options) { }

    public void AddOrUpdate<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression) { }

    public void AddOrUpdate<T>(
        string recurringJobId,
        Expression<Func<T, Task>> methodCall,
        RecurringJobOptions options) { }

    public void RemoveIfExists(string recurringJobId) { }

    public void Trigger(string recurringJobId) => TriggeredJobIds.Add(recurringJobId);
}
