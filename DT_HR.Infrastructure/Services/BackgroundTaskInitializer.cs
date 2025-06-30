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
            // RecurringJob.AddOrUpdate<BackgroundTaskJobs>(
            //     $"checkin-reminder-{user.TelegramUserId}",
            //     j => j.SendCheckInReminderAsync(user.TelegramUserId, cancellationToken),
            //     "*/10 * * * * *");
        } // Cron.Daily(user.WorkStartTime.Hour, user.WorkStartTime.Minute

        if (users.Count > 0)
        {
            var start = users[0].WorkStartTime;
            var end = users[0].WorkEndTime;
            RecurringJob.AddOrUpdate<BackgroundTaskJobs>(
                "daily-stats-after-checkin",
                j => j.SendAttendanceStatsAsync(cancellationToken),
                Cron.Daily(start.Hour + 1, start.Minute ));
            RecurringJob.AddOrUpdate<BackgroundTaskJobs>(
                "daily-stats-end-of-day",
                j => j.SendAttendanceStatsAsync(cancellationToken),
                Cron.Daily(end.Hour + 1, end.Minute));
            
            
            
            // RecurringJob.AddOrUpdate<BackgroundTaskJobs>(
            //     "frequent-stats-check",
            //     j => j.SendAttendanceStatsAsync(cancellationToken),
            //     "*/5 * * * * *"); //5 sec
        }
        logger.LogInformation("Background tasks configured");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}