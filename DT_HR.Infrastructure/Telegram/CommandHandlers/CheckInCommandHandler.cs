using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class CheckInCommandHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    ILogger<CheckInCommandHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;

        var state = await stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? await localization.GetUserLanguage(message.From!.Id);
        var text = message.Text.ToLower();
        var checkInText = localization.GetString(ResourceKeys.CheckIn, language).ToLower();
        return 
            text == "/checkin" ||
            text == checkInText ||
            text.Contains("check in");
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var currentState = await stateService.GetStateAsync(userId);
        var language = currentState?.Language ?? await localization.GetUserLanguage(message.From!.Id);
        
        logger.LogInformation("Processing check-in command  for the user {UserId}",userId);

        var state = new UserState
        {
            CurrentAction = UserAction.CheckingIn,
            Language = language
        };
        await stateService.SetStateAsync(userId, state);

        var checkInProgress = localization.GetString(ResourceKeys.CheckInProcess, language);
        var shareLocationPrompt = localization.GetString(ResourceKeys.ShareLocationPrompt, language);
        
        
        await messageService.SendLocationRequestAsync(
            chatId,
            $"{checkInProgress}\n\n{shareLocationPrompt}",
            language,
            cancellationToken);
    }
}