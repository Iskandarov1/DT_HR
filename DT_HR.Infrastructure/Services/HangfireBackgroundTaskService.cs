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
            var checkTimeOffset = new DateTimeOffset(eta, TimeSpan.Zero);

        jobClient.Schedule<BackgroundTaskJobs>(
            j => j.CheckArrivalAsync(telegramUserId, eta, cancellationToken),
            checkTimeOffset);
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
        logger.LogInformation("ScheduleEventReminderAsync called for event: {Description} at {EventTime} UTC", description, eventTime);


        var tenMinBefore = eventTime.AddMinutes(-10);


        var utcNow = DateTime.UtcNow;
        if (tenMinBefore < utcNow) 
        {
            tenMinBefore = utcNow.AddMinutes(1); 
        }


        var beforeOffset = new DateTimeOffset(tenMinBefore, TimeSpan.Zero);
        var eventOffset = new DateTimeOffset(eventTime, TimeSpan.Zero);


        jobClient.Schedule<BackgroundTaskJobs>(
            j => j.SendEventReminderAsync(description, eventTime, cancellationToken),
            beforeOffset);
        

        jobClient.Schedule<BackgroundTaskJobs>(
            j => j.SendEventReminderAsync(description, eventTime, cancellationToken),
            eventOffset);
        
        logger.LogInformation("Event reminders scheduled at {First}  and {Second} ", tenMinBefore, eventTime);

        return Task.CompletedTask;
    }
}