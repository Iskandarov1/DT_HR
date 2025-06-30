using System.Text;
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
    ILogger<MyEventsCommandHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        var state = await stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? await localization.GetUserLanguage(message.From!.Id);
        var text = message.Text.ToLower();
        var eventsText = localization.GetString(ResourceKeys.MyEvents, language);
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

        if (events.Count == 0)
        {
            await messageService.SendTextMessageAsync(chatId, localization.GetString(ResourceKeys.NoEvents, langauge),
                cancellationToken: cancellationToken);
            await messageService.ShowMainMenuAsync(chatId, localization.GetString(ResourceKeys.MyEvents, langauge),
                langauge,
                cancellationToken: cancellationToken);
            
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("📅 *Upcoming Events:*\n");
        
        foreach (var evt in events)
        {

            var localEventTime = evt.EventTime.AddHours(5);
            sb.AppendLine($"📝 {evt.Description}");
            sb.AppendLine($"⏰ {localEventTime:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
        }

        await messageService.SendTextMessageAsync(chatId, sb.ToString(), cancellationToken: cancellationToken);

        await messageService.ShowMainMenuAsync(chatId, localization.GetString(ResourceKeys.MyEvents, langauge),
            langauge,
            cancellationToken: cancellationToken);

    }
}