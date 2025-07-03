using DT_HR.Application.Core.Abstractions.Enum;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DT_HR.Infrastructure.Services;

public class BackgroundTaskJobs(
    ITelegramMessageService messageService,
    ILocalizationService localization,
    IUserRepository userRepository,
    IAttendanceRepository attendanceRepository,
    IAttendanceReportService reportService,
    IGroupRepository groupRepository,
    ILogger<BackgroundTaskJobs> logger)
{
    public async Task SendCheckInReminderAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var maybeUser = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (maybeUser.HasNoValue)
            {
                logger.LogWarning("User {UserId} not found", telegramUserId);
                return;
            }

            var user = maybeUser.Value;
            var today = DateOnly.FromDateTime(TimeUtils.Now);
            var attendance = await attendanceRepository.GetByUserAndDateAsync(user.Id, today, cancellationToken);
            var language = await localization.GetUserLanguage(telegramUserId);

            if (attendance.HasValue && attendance.Value.CheckInTime.HasValue)
            {
                var text = localization.GetString(ResourceKeys.ThankYouCheckIn, language);
                await messageService.SendTextMessageAsync(telegramUserId, text, cancellationToken: cancellationToken);
                logger.LogInformation("Thank you message sent to user {UserId} who already checked in", telegramUserId);
            }
            else
            {
                var text = localization.GetString(ResourceKeys.CheckInCheck, language);
                await messageService.SendTextMessageAsync(telegramUserId, text , cancellationToken: cancellationToken);
                await messageService.ShowMainMenuAsync(
                    telegramUserId,
                    language,
                    menuType: MainMenuType.CheckPrompt,
                    cancellationToken:cancellationToken);
                logger.LogInformation("Check-in reminder sent to user {UserId}", telegramUserId);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error sending check-in reminder to user {UserId}", telegramUserId);
        }
    }

    public async Task CheckArrivalAsync(long telegramUserId, DateTime eta,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("CheckArrivalAsync called for user {UserId} at ETA {ETA}", telegramUserId, eta);
        try
        {
            
            var maybeUser = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (maybeUser.HasNoValue)
            {
                logger.LogWarning("User {UserId} not found", telegramUserId);
                return;
            }

            var user = maybeUser.Value;
            var today = DateOnly.FromDateTime(TimeUtils.Now);
            var attendance = await attendanceRepository.GetByUserAndDateAsync(user.Id, today, cancellationToken);
            var language = await localization.GetUserLanguage(telegramUserId);

            if (attendance?.Value.CheckInTime.HasValue == true)
            {
                var text = localization.GetString(ResourceKeys.ThankYouCheckIn, language);
                await messageService.SendTextMessageAsync(telegramUserId, text, cancellationToken:cancellationToken);
            }
            else
            {
                var text = localization.GetString(ResourceKeys.TimePassed, language);
                await messageService.SendTextMessageAsync(telegramUserId, text, cancellationToken: cancellationToken);
            }
            logger.LogInformation("Arrival check completed for user {UserId}", telegramUserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking arrival for user {UserId}", telegramUserId);
        }

    }
    
    public async Task SendAttendanceStatsAsync(CancellationToken cancellationToken = default)
    {
        var managers = await userRepository.GetManagersAsync(cancellationToken);
        if(managers.Count == 0) return;

        var report =
            await reportService.GetDailyAttendanceReport(DateOnly.FromDateTime(TimeUtils.Now), cancellationToken);
        
        foreach (var manager in managers)
        {
            var lang = manager.Language;
            var title = localization.GetString(ResourceKeys.AttendanceStats, lang);
            var total = localization.GetString(ResourceKeys.TotalEmployees, lang);
            var present = localization.GetString(ResourceKeys.Present, lang);
            var late = localization.GetString(ResourceKeys.Late, lang);
            var absent = localization.GetString(ResourceKeys.Absent, lang);
            var onTheWay = localization.GetString(ResourceKeys.OnTheWay, lang);
            
            var text = $"*{title}*\n" +
                       $"📅 *{report.Date:yyyy-MM-dd}*\n" +
                       $"{total}: {report.TotalEmployees}\n" +
                       $"{present}: {report.Present}\n" +
                       $"{late}: {report.Late}\n" +
                       $"{absent}: {report.Absent}\n" +
                       $"{onTheWay}: {report.OnTheWay}";
            
            await messageService.SendTextMessageAsync(manager.TelegramUserId, text,
                cancellationToken: cancellationToken);
        }

        var groups = await groupRepository.GetActiveSubscribersAsync(cancellationToken);
        foreach (var group in groups)
        {
            
            
            var lang = "uz";
            var title = localization.GetString(ResourceKeys.AttendanceStats, lang);
            var total = localization.GetString(ResourceKeys.TotalEmployees, lang);
            var present = localization.GetString(ResourceKeys.Present, lang);
            var late = localization.GetString(ResourceKeys.Late, lang);
            var absent = localization.GetString(ResourceKeys.Absent, lang);
            var onTheWay = localization.GetString(ResourceKeys.OnTheWay, lang);
            
            var text = $"*{title}*\n" +
                       $"📅 *{report.Date:yyyy-MM-dd}*\n" +
                       $"{total}: {report.TotalEmployees}\n" +
                       $"{present}: {report.Present}\n" +
                       $"{late}: {report.Late}\n" +
                       $"{absent}: {report.Absent}\n" +
                       $"{onTheWay}: {report.OnTheWay}";
            await messageService.SendTextMessageAsync(group.ChatId, text, cancellationToken: cancellationToken);
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
                    var localEventTime = eventTime.AddHours(5);
                    
                    var title = localization.GetString(ResourceKeys.EventReminder, language);
                    var text = $"🔔 *{title}*\n\n" +
                               $"📅 {description}\n" +
                               $"⏰ {localEventTime:dd-MM-yyyy HH:mm}";
                    
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
    
    private static readonly Dictionary<(int Month ,int Day),string> UzbekHolidays = new()
    {
        {(1,  1),  "Yangi yil"},                          
        {(1, 14),  "Vatan himoyachilari kuni"},           
        {(3,  8),  "Xotin-qizlar kuni"},                   
        {(3, 21),  "Navro'z"},                            
        {(5,  9),  "Xotira va qadrlash kuni"},            
        {(6,  1),  "Xalqaro bolalar kuni"},                
        {(6, 27),  "Matbuot va OAV xodimlari kuni"},      
        {(6, 30),  "Yoshlar kuni"},                        
        {(8, 31),  "Qatag'on qurbonlari xotirasi kuni"},   
        {(9,  1),  "Mustaqillik kuni"},                    
        {(10, 1),  "Ustoz va murabbiylar kuni"},           
        {(10, 21), "O'zbek tili kuni"},                   
        {(12, 8),  "Konstitutsiya kuni"}                   
    };

    public async Task SendHolidayGreetingsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(TimeUtils.Now);
        if (!UzbekHolidays.TryGetValue((today.Month,today.Day), out var holidayName))
            return;
        var users = await userRepository.GetActiveUsersAsync(cancellationToken);
        foreach (var user in users)
        {
            var language = await localization.GetUserLanguage(user.TelegramUserId);
            var text = localization.GetString(ResourceKeys.HolidayGreeting, language, holidayName);
            await messageService.SendTextMessageAsync(user.TelegramUserId, text, cancellationToken: cancellationToken);
        }
        logger.LogInformation("Holiday greetings sent for {Holiday}",holidayName);
    }

    public async Task SendBirthdayGreetingsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(TimeUtils.Now);
        var users = await userRepository.GetUsersWithBirthdayAsync(today, cancellationToken);
        foreach (var user in users)
        {
            var lang = await localization.GetUserLanguage(user.TelegramUserId);
            var text = localization.GetString(ResourceKeys.HappyBirthday, lang);
            await messageService.SendTextMessageAsync(user.TelegramUserId, text, cancellationToken: cancellationToken);
        }
        if(users.Count > 0)
            logger.LogInformation("Birthday greetings sent to {Count} users ",users.Count);
        
    }


}