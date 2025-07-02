using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Entities;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace DT_HR.Infrastructure.Services;

public class UserBackgroundJobService(ILogger<UserBackgroundJobService> logger) : IUserBackgroundJobService
{
    public async Task InitializeBackgroundJobsForUserAsync(User user, CancellationToken cancellationToken = default)
    {
        var utcHour = (user.WorkStartTime.Hour - 5 + 24) % 24;
        
        RecurringJob.AddOrUpdate<BackgroundTaskJobs>(
            $"checkin-reminder-{user.TelegramUserId}",
            j => j.SendCheckInReminderAsync(user.TelegramUserId, cancellationToken),
            Cron.Daily(utcHour, user.WorkStartTime.Minute),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });
        
        logger.LogInformation("Check-in reminder scheduled for newly registered user {UserId} at {LocalTime} (UTC: {UtcHour}:{Minute})", 
            user.TelegramUserId, user.WorkStartTime, utcHour, user.WorkStartTime.Minute);
        
        await Task.CompletedTask;
    }
}