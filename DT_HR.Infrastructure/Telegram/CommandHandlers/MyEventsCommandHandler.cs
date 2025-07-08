using System.Text;
using DT_HR.Application.Core.Abstractions.Enum;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class MyEventsCommandHandler (
    ITelegramMessageService messageService, 
    IUserStateService stateService, 
    ILocalizationService localization, 
    IEventRepository eventRepository,
    IUserRepository userRepository,
    IAttendanceRepository attendanceRepository,
    ILogger<MyEventsCommandHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        var state = await stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? await localization.GetUserLanguage(message.From!.Id);
        var text = message.Text.ToLower();
        var eventsText = localization.GetString(ResourceKeys.MyEvents, language).ToLower();
            return text == "/events" || 
                   text == eventsText || 
                   text.Contains("events");
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var state = await stateService.GetStateAsync(userId);
        var langauge = state?.Language ?? await localization.GetUserLanguage(userId);
        logger.LogInformation("Processing event getting for the user {UserId}",userId);

        var localNow = TimeUtils.Now;
        var utcNow = localNow.AddHours(-5); 
        
        var allEvents = await eventRepository.GetUpcomingEventsAsync(utcNow, cancellationToken);
        var events = allEvents.Where(e => !e.Description.ToLower().Contains("holiday") && !e.Description.ToLower().Contains("праздник")).ToList();
        var holidays = allEvents.Where(e => e.Description.ToLower().Contains("holiday") || e.Description.ToLower().Contains("праздник")).ToList();
        
        // Gettin upcoming birthdays (next 7 days)
        var next30Days = new List<DateOnly>();
        for (int i = 0; i < 7; i++)
        {
            next30Days.Add(DateOnly.FromDateTime(localNow.AddDays(i)));
        }
        
        var upcomingBirthdays = new List<Domain.Entities.User>();
        foreach (var date in next30Days)
        {
            var birthdayUsers = await userRepository.GetUsersWithBirthdayAsync(date, cancellationToken);
            upcomingBirthdays.AddRange(birthdayUsers);
        }


        var menuType = MainMenuType.Default;
        var maybeUser = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);
        
        if (maybeUser.HasValue)
        {
            var today = DateOnly.FromDateTime(TimeUtils.Now);
            var attendance = await attendanceRepository.GetByUserAndDateAsync(maybeUser.Value.Id, today, cancellationToken);
            
            if (attendance.HasValue)
            {
                if (attendance.Value.CheckInTime.HasValue && !attendance.Value.CheckOutTime.HasValue)
                {
                    menuType = MainMenuType.CheckedIn;
                }
                else if (attendance.Value.CheckInTime.HasValue && attendance.Value.CheckOutTime.HasValue)
                {
                    menuType = MainMenuType.CheckedOut;
                }
                else
                {
                    menuType = MainMenuType.CheckPrompt;
                }
            }
            else
            {
                menuType = MainMenuType.CheckPrompt;
            }
        }

        if (events.Count == 0 && upcomingBirthdays.Count == 0 && holidays.Count == 0)
        {
            await messageService.SendTextMessageAsync(chatId, localization.GetString(ResourceKeys.NoEvents, langauge),
                cancellationToken: cancellationToken);
            await messageService.ShowMainMenuAsync(
                chatId, 
                langauge,
                menuType: menuType,
                cancellationToken: cancellationToken);
            
            return;
        }

        var sb = new StringBuilder();
        var hasAnyContent = false;
        

        if (events.Count > 0)
        {
            var upcomingEvent = localization.GetString(ResourceKeys.UpcomingEvents, langauge);
            sb.AppendLine($"📅 *{upcomingEvent}:*\n");
            
            foreach (var evt in events)
            {
                var localEventTime = evt.EventTime.AddHours(5);
                sb.AppendLine($"📝 {evt.Description}");
                sb.AppendLine($"⏰ {localEventTime:dd-MM-yyyy HH:mm}");
                sb.AppendLine();
            }
            hasAnyContent = true;
        }
        
        if (upcomingBirthdays.Count > 0)
        {
            if (hasAnyContent) sb.AppendLine();
            sb.AppendLine($"🎂 *{localization.GetString(ResourceKeys.Birthdays, langauge)}:*\n");
            
            foreach (var user in upcomingBirthdays)
            {
                var birthdayThisYear = new DateOnly(localNow.Year, user.BirtDate.Month, user.BirtDate.Day);
                var age = localNow.Year - user.BirtDate.Year;
                if (birthdayThisYear < DateOnly.FromDateTime(localNow))
                {
                    birthdayThisYear = birthdayThisYear.AddYears(1);
                    age--;
                }
                
                sb.AppendLine($"🎉 {user.FirstName} {user.LastName}");
                sb.AppendLine($"📅 {birthdayThisYear:dd-MM-yyyy} ({age + 1} years old)");
                sb.AppendLine();
            }
            hasAnyContent = true;
        }
        
        if (holidays.Count > 0)
        {
            if (hasAnyContent) sb.AppendLine();
            sb.AppendLine($"🎊 *{localization.GetString(ResourceKeys.HolidayGreeting, langauge)}:*\n");
            
            foreach (var holiday in holidays)
            {
                var localHolidayTime = holiday.EventTime.AddHours(5);
                sb.AppendLine($"🎄 {holiday.Description}");
                sb.AppendLine($"📅 {localHolidayTime:dd-MM-yyyy}");
                sb.AppendLine();
            }
        }

        await messageService.SendTextMessageAsync(chatId, sb.ToString(), cancellationToken: cancellationToken);

        await messageService.ShowMainMenuAsync(
            chatId, 
            langauge,
            menuType: menuType,
            cancellationToken: cancellationToken);
    }
}