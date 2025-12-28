using System.Linq.Expressions;

namespace Application.Shared.Background;

public interface IBackgroundJobScheduler
{
    string Enqueue<TJob>(Expression<Action<TJob>> job);
    string Enqueue<TJob>(Expression<Func<TJob, Task>> job);
    string Schedule<TJob>(Expression<Action<TJob>> job, TimeSpan delay);
    string Schedule<TJob>(Expression<Func<TJob, Task>> job, TimeSpan delay);
}
