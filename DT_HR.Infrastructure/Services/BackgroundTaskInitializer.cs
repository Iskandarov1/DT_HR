using DT_HR.Domain.Repositories;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DT_HR.Infrastructure.Services;

public class BackgroundTaskInitializer(
    IServiceProvider serviceProvider,
    ILogger<BackgroundTaskInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        
        var users = await userRepository.GetActiveUsersAsync(cancellationToken);
        foreach (var user in users)
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
            
                
            logger.LogInformation("Check-in reminder scheduled for user {UserId} at {LocalTime} (UTC: {UtcHour}:{Minute})", 
                user.TelegramUserId, user.WorkStartTime, utcHour, user.WorkStartTime.Minute);
        }  

        if (users.Count > 0)
        {
            var start = users[0].WorkStartTime;
            var end = users[0].WorkEndTime;
            

            var utcStartHour = (start.Hour + 1 - 5 + 24) % 24;
            var utcEndHour = (end.Hour + 1 - 5 + 24) % 24;
            
            RecurringJob.AddOrUpdate<BackgroundTaskJobs>(
                "daily-stats-after-checkin",
                j => j.SendAttendanceStatsAsync(cancellationToken),
                Cron.Daily(utcStartHour, start.Minute),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });
                
            RecurringJob.AddOrUpdate<BackgroundTaskJobs>(
                "daily-stats-end-of-day",
                j => j.SendAttendanceStatsAsync(cancellationToken),
                Cron.Daily(utcEndHour, end.Minute),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                });
            
            logger.LogInformation("Daily stats scheduled in UTC - After start: {LocalStart} -> UTC {UtcStart}, End: {LocalEnd} -> UTC {UtcEnd}", 
                $"{start.Hour + 1}:{start.Minute:D2}", $"{utcStartHour}:{start.Minute:D2}",
                $"{end.Hour + 1}:{end.Minute:D2}", $"{utcEndHour}:{end.Minute:D2}");
        }
        logger.LogInformation("Background tasks configured with UTC timezone");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}