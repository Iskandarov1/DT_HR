using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Core;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace DT_HR.Infrastructure.Services;

public class BackgroundTaskJobs(
    ITelegramMessageService messageService,
    ILocalizationService localization,
    IUserRepository userRepository,
    IAttendanceReportService reportService,
    ILogger<BackgroundTaskJobs> logger)
{
    public async Task SendCheckInReminderAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        var language = await localization.GetUserLanguage(telegramUserId);
        var text = "Please remember to check in";
        await messageService.SendTextMessageAsync(telegramUserId, text, cancellationToken: cancellationToken);
        logger.LogInformation("Check-in reminder sent to {UserId}", telegramUserId);
    }

    public async Task CheckArrivalAsync(long telegramUserId, DateTime eta,
        CancellationToken cancellationToken = default)
    {
        var language = await localization.GetUserLanguage(telegramUserId);
        var text = $"It's {TimeUtils.Now:HH:mm}. Please confirm your arrival.";
        await messageService.SendTextMessageAsync(telegramUserId, text, cancellationToken: cancellationToken);
        logger.LogInformation("Arrival follow-up sent to {UserId}", telegramUserId);
    }
    
    public async Task SendAttendanceStatsAsync(CancellationToken cancellationToken = default)
    {
        var managers = await userRepository.GetManagersAsync(cancellationToken);
        if(managers.Count == 0) return;

        var report =
            await reportService.GetDailyAttendanceReport(DateOnly.FromDateTime(TimeUtils.Now), cancellationToken);
        var text = $"*{report.Date:yyyy-MM-dd}*\n" +
                   $"Total: {report.TotalEmployees}\n" +
                   $"Present: {report.Present}\nLate: {report.Late}\n" +
                   $"Absent: {report.Absent}\nOn The Way: {report.OnTheWay}";
        foreach (var manager in managers)
        {
            await messageService.SendTextMessageAsync(manager.TelegramUserId, text,
                cancellationToken: cancellationToken);
        }
        logger.LogInformation("Attendance stats sent to managers");
    }

    public async Task SendEventReminderAsync(string description, DateTime eventTime,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SendEventReminderAsync called with description: {Description}, eventTime: {EventTime} UTC", description, eventTime);
        
        var users = await userRepository.GetActiveUsersAsync(cancellationToken);
        logger.LogInformation("Found {Count} active users", users.Count);
        
        if (users.Count == 0)
        {
            logger.LogWarning("No active users found for event reminder");
            return;
        }
        
        try
        {
            logger.LogInformation("Sending event reminder to {Count} users for event: {Description}", users.Count, description);
            
            var sentCount = 0;
            foreach (var user in users)
            {
                try
                {
                    var language = await localization.GetUserLanguage(user.TelegramUserId);
                    
                    // Convert UTC to local time (UTC+5) for display
                    var localEventTime = eventTime.AddHours(5);
                    
                    var text = $"🔔 *Event Reminder*\n\n" +
                              $"📅 Event: {description}\n" +
                              $"⏰ Time: {localEventTime:yyyy-MM-dd HH:mm}";
                    
                    await messageService.SendTextMessageAsync(user.TelegramUserId, text, 
                        cancellationToken: cancellationToken);
                    sentCount++;
                    logger.LogDebug("Event reminder sent to user {UserId}", user.TelegramUserId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send event reminder to user {UserId}", user.TelegramUserId);
                }
            }
            
            logger.LogInformation("Event reminder sent successfully to {SentCount}/{TotalCount} users", sentCount, users.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send event reminders for event: {Description}", description);
            throw;
        }
    }


}