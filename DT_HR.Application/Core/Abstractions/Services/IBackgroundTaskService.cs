namespace DT_HR.Application.Core.Abstractions.Services;

public interface IBackgroundTaskService
{
    Task<string> ScheduleTaskAsync(
        string taskType,
        DateTime scheduledFor,
        object? payload = null,
        CancellationToken cancellationToken = default);


    Task<string> ScheduleRecurringTaskAsync(
        string taskType,
        string cronExpression,
        object? payload = null,
        CancellationToken cancellationToken = default);


    Task<string> ScheduleDelayedTaskAsync(
        string taskType,
        TimeSpan delay,
        object? payload = null,
        CancellationToken cancellationToken = default);


    Task CancelTaskAsync(string taskId, CancellationToken cancellationToken);

   // Task<TaskStatus> GetTaskStatusAsync(string taskId, CancellationToken cancellationToken);
}