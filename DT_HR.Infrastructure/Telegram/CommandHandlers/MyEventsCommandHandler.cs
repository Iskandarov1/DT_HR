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
        
        var events = await eventRepository.GetUpcomingEventsAsync(utcNow, cancellationToken);


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

        if (events.Count == 0)
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

        var upcomingEvent = localization.GetString(ResourceKeys.UpcomingEvents, langauge);
        var sb = new StringBuilder();
        sb.AppendLine($"📅 *{upcomingEvent}:*\n");
        
        foreach (var evt in events)
        {
            // var date = localization.GetString(ResourceKeys.Date, langauge);
            // var eventTime = localization.GetString(ResourceKeys.Event, langauge);
            var localEventTime = evt.EventTime.AddHours(5);
            sb.AppendLine($"📝 {evt.Description}");
            sb.AppendLine($"⏰ {localEventTime:dd-MM-yyyy HH:mm}");
            sb.AppendLine();
        }

        await messageService.SendTextMessageAsync(chatId, sb.ToString(), cancellationToken: cancellationToken);

        await messageService.ShowMainMenuAsync(
            chatId, 
            langauge,
            menuType: menuType,
            cancellationToken: cancellationToken);
    }
}