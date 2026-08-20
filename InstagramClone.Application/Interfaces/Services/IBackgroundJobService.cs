using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Services;

public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueue a fire-and-forget background job.
    /// </summary>
    string Enqueue(Expression<Action> methodCall);

    /// <summary>
    /// Enqueue an async fire-and-forget background job.
    /// </summary>
    string Enqueue(Expression<Func<Task>> methodCall);

    /// <summary>
    /// Enqueue a fire-and-forget background job for a specific service type T.
    /// </summary>
    string Enqueue<T>(Expression<Action<T>> methodCall);

    /// <summary>
    /// Enqueue an async fire-and-forget background job for a specific service type T.
    /// </summary>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Schedule a background job to run after a specified delay.
    /// </summary>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);
}
