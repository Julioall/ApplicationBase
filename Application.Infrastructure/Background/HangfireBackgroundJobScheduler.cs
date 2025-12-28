using Application.Shared.Background;
using Hangfire;
using System.Linq.Expressions;

namespace Application.Infrastructure.Background;

public class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    private readonly IBackgroundJobClient _client;

    public HangfireBackgroundJobScheduler(IBackgroundJobClient client)
    {
        _client = client;
    }

    public string Enqueue<TJob>(Expression<Action<TJob>> job)
    {
        return _client.Enqueue(job);
    }

    public string Enqueue<TJob>(Expression<Func<TJob, Task>> job)
    {
        return _client.Enqueue(job);
    }

    public string Schedule<TJob>(Expression<Action<TJob>> job, TimeSpan delay)
    {
        return _client.Schedule(job, delay);
    }

    public string Schedule<TJob>(Expression<Func<TJob, Task>> job, TimeSpan delay)
    {
        return _client.Schedule(job, delay);
    }
}
