using DT_HR.Domain.Repositories;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TimeZoneConverter;

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
            
            // var utcMinus5 = TimeZoneInfo.FindSystemTimeZoneById("Etc/GMT+5");
            var tz = TZConvert.GetTimeZoneInfo("Asia/Tashkent");
            
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
            
            RecurringJob.AddOrUpdate<BackgroundTaskJobs>(
                "holiday-greetings",
                j => j.SendHolidayGreetingsAsync(cancellationToken),
                Cron.Daily(0,2),tz);
            
        }
        logger.LogInformation("Background tasks configured with UTC timezone");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}