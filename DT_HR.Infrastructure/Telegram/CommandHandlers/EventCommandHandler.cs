using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class EventCommandHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    ITelegramKeyboardService keyboardService,
    IUserRepository userRepository,
    ILogger<EventCommandHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        var maybeUser = await userRepository.GetByTelegramUserIdAsync(message.From!.Id,cancellationToken);
        if (maybeUser.HasNoValue || !maybeUser.Value.IsManager())
            return false;
        
        var state = await stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? await localization.GetUserLanguage(message.From!.Id);
        var text = message.Text.ToLower();
        var eventText = localization.GetString(ResourceKeys.CreateEvent, language).ToLower();
        return text == "/event" || text == eventText || text.Contains("event");
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var maybeUser = await userRepository.GetByTelegramUserIdAsync(message.From!.Id,cancellationToken);

        if (maybeUser.HasNoValue || !maybeUser.Value.IsManager())
        {
            var language = await localization.GetUserLanguage(userId);
            await messageService.ShowMainMenuAsync(chatId,
                language,
                cancellationToken: cancellationToken);
            return;
        }
        logger.LogInformation("Processing event creating for the user {UserId}",userId);

        var state = new UserState
        {
            CurrentAction = UserAction.CreatingEvent,
            Language = (await localization.GetUserLanguage(userId))
        };

        state.Data["step"] = "description";
        await stateService.SetStateAsync(userId, state);
        
        await messageService.SendTextMessageAsync(
            chatId,
            localization.GetString(ResourceKeys.EnterEventDescription, state.Language),
            keyboardService.GetCancelInlineKeyboard(state.Language),
            cancellationToken: cancellationToken);
    }
}