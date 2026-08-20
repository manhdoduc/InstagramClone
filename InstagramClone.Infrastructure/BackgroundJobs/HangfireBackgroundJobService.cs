using Hangfire;
using InstagramClone.Application.Interfaces.Services;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace InstagramClone.Infrastructure.BackgroundJobs;

public class HangfireBackgroundJobService(IBackgroundJobClient backgroundJobClient) : IBackgroundJobService
{
    public string Enqueue(Expression<Action> methodCall) => 
        backgroundJobClient.Enqueue(methodCall);

    public string Enqueue(Expression<Func<Task>> methodCall) => 
        backgroundJobClient.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Action<T>> methodCall) => 
        backgroundJobClient.Enqueue<T>(methodCall);

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall) => 
        backgroundJobClient.Enqueue<T>(methodCall);

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay) => 
        backgroundJobClient.Schedule<T>(methodCall, delay);
}
