using System.Collections.Concurrent;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DT_HR.Infrastructure;

public class BackgroundTaskService : IBackgroundTaskService, IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundTaskService> _logger;
    private static readonly TimeSpan localOffset = TimeUtils.LocalOffset;
    private readonly ConcurrentDictionary<string, ScheduledTask> _scheduledTasks = new();
    private readonly ConcurrentDictionary<string, Timer> _recurringTasks = new();
    private Timer? _cleanupTimer;

    public BackgroundTaskService(
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundTaskService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task<string> ScheduleTaskAsync(
        string taskType,
        DateTime scheduledFor,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        var taskId = Guid.NewGuid().ToString();
        var delay = scheduledFor - TimeUtils.Now;

        if (delay < TimeSpan.Zero)
        {
            _logger.LogWarning("Task scheduled for past time. Executing immediately.");
            delay = TimeSpan.Zero;
        }

        var timer = new Timer(
            async _ => await ExecuteTask(taskId, taskType, payload),
            null,
            delay,
            Timeout.InfiniteTimeSpan);

        _scheduledTasks[taskId] = new ScheduledTask
        {
            Id = taskId,
            Type = taskType,
            ScheduledFor = scheduledFor,
            Payload = payload,
            Timer = timer
        };

        _logger.LogInformation("Task {TaskId} of type {TaskType} scheduled for {ScheduledFor}",
            taskId, taskType, scheduledFor);

        return Task.FromResult(taskId);
    }

    public Task<string> ScheduleRecurringTaskAsync(
        string taskType,
        string cronExpression,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        // For simplicity, using a basic interval instead of cron parsing
        // In production, use a library like NCrontab for proper cron support
        var taskId = Guid.NewGuid().ToString();
        var interval = TimeSpan.FromMinutes(30); // Default interval

        var timer = new Timer(
            async _ => await ExecuteTask(taskId, taskType, payload),
            null,
            TimeSpan.Zero,
            interval);

        _recurringTasks[taskId] = timer;

        _logger.LogInformation("Recurring task {TaskId} of type {TaskType} scheduled",
            taskId, taskType);

        return Task.FromResult(taskId);
    }

    public Task<string> ScheduleDelayedTaskAsync(
        string taskType,
        TimeSpan delay,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        return ScheduleTaskAsync(taskType, TimeUtils.Now.Add(delay), payload, cancellationToken);
    }

    public Task CancelTaskAsync(string taskId, CancellationToken cancellationToken)
    {
        if (_scheduledTasks.TryRemove(taskId, out var task))
        {
            task.Timer?.Dispose();
            _logger.LogInformation("Task {TaskId} cancelled", taskId);
        }
        else if (_recurringTasks.TryRemove(taskId, out var timer))
        {
            timer?.Dispose();
            _logger.LogInformation("Recurring task {TaskId} cancelled", taskId);
        }

        return Task.CompletedTask;
    }

    private async Task ExecuteTask(string taskId, string taskType, object? payload)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            
            _logger.LogInformation("Executing task {TaskId} of type {TaskType}", taskId, taskType);

            // Handle specific task types
            switch (taskType)
            {
                case "Check Arrival":
                    await HandleCheckArrival(scope, payload);
                    break;
                default:
                    _logger.LogWarning("Unknown task type: {TaskType}", taskType);
                    break;
            }

            // Remove one-time tasks after execution
            if (_scheduledTasks.TryRemove(taskId, out var task))
            {
                task.Timer?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing task {TaskId} of type {TaskType}", taskId, taskType);
        }
    }

    private async Task HandleCheckArrival(IServiceScope scope, object? payload)
    {
        if (payload is not IDictionary<string, object> data) return;

        if (data.TryGetValue("TelegramUserId", out var userIdObj) && 
            data.TryGetValue("EstimatedArrivalTime", out var etaObj))
        {
            var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();
            var userId = Convert.ToInt64(userIdObj);
            var eta = (DateTime)etaObj;
            var etaLocal = DateTime.SpecifyKind(eta, DateTimeKind.Utc).Add(localOffset);

            await telegramService.SendTextMessageAsync(
                userId,
                $"👋 Hi! You were expected to arrive by {etaLocal:HH:mm}. Have you arrived at the office?");
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cleanupTimer = new Timer(
            _ => CleanupExpiredTasks(),
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));

        _logger.LogInformation("Background task service started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cleanupTimer?.Dispose();

        foreach (var task in _scheduledTasks.Values)
        {
            task.Timer?.Dispose();
        }

        foreach (var timer in _recurringTasks.Values)
        {
            timer?.Dispose();
        }

        _logger.LogInformation("Background task service stopped");
        return Task.CompletedTask;
    }

    private void CleanupExpiredTasks()
    {
        var now = TimeUtils.Now;
        var expiredTasks = _scheduledTasks
            .Where(kvp => kvp.Value.ScheduledFor < now.AddHours(-1))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var taskId in expiredTasks)
        {
            if (_scheduledTasks.TryRemove(taskId, out var task))
            {
                task.Timer?.Dispose();
            }
        }

        if (expiredTasks.Any())
        {
            _logger.LogInformation("Cleaned up {Count} expired tasks", expiredTasks.Count);
        }
    }

    private class ScheduledTask
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime ScheduledFor { get; set; }
        public object? Payload { get; set; }
        public Timer? Timer { get; set; }
    }
}