using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Core;
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

    public Task ScheduleEventReminderAsync(string description, DateTime eventTime, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("ScheduleEventReminderAsync called for event: {Description} at {EventTime}", description, eventTime);

        var tenMinBefore = eventTime.AddMinutes(-10);
        var runAt = new DateTimeOffset(eventTime, TimeSpan.Zero); 

        if (tenMinBefore < TimeUtils.Now) tenMinBefore = TimeUtils.Now;

        jobClient.Schedule<BackgroundTaskJobs>(
            j => j.SendEventReminderAsync(description, eventTime, cancellationToken),
            tenMinBefore);
        jobClient.Schedule<BackgroundTaskJobs>(
            j => j.SendEventReminderAsync(description, eventTime, cancellationToken),
            runAt);
        
        logger.LogInformation("Event reminders scheduled at {First} and {Second}", tenMinBefore, eventTime);

        return Task.CompletedTask;
    }
}