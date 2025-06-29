using DT_HR.Application.Core.Abstractions.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace DT_HR.Infrastructure.Services;

public class HangfireBackgroundTaskService(
    IBackgroundJobClient jobClient,
    ILogger<HangfireBackgroundTaskService> logger) : IBackgroundTaskService
{
    public Task ScheduleCheckInReminderAsync(long telegramUserId, DateTime scheduledFor,
        CancellationToken cancellationToken = default)
    {
        jobClient.Schedule<BackgroundTaskJobs>(j => j.SendCheckInReminderAsync(telegramUserId, cancellationToken),
            scheduledFor);
        logger.LogInformation("Check-in reminder scheduled for user {UserId} at {Time}", telegramUserId, scheduledFor);
        
        return Task.CompletedTask;
    }

    public Task ScheduleArrivalCheckAsync(long telegramUserId, DateTime eta, CancellationToken cancellationToken = default)
    {
        jobClient.Schedule<BackgroundTaskJobs>(j => j.CheckArrivalAsync(telegramUserId, eta, cancellationToken), eta);
        logger.LogInformation("Arrival check scheduled for user {UserId} at {Time}", telegramUserId, eta);
        return Task.CompletedTask;
    }

    public Task ScheduleAttendanceStatsAsync(DateTime scheduledFor, CancellationToken cancellationToken = default)
    {
        jobClient.Schedule<BackgroundTaskJobs>(j => j.SendAttendanceStatsAsync(cancellationToken), scheduledFor);
        logger.LogInformation("Attendance stats scheduled at {Time}", scheduledFor);
        return Task.CompletedTask;

    }
}