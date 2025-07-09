using DT_HR.Application.Core.Abstractions.Enum;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CallbackHandlers;

public class CancelCallbackHandler(
    ITelegramMessageService messageService, 
    IUserStateService userState,
    ILocalizationService localization,
    ILogger<CancelCallbackHandler> logger) : ITelegramCallbackQuery
{
    public Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(callbackQuery.Data == "action:cancel");
    }

    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var language = await localization.GetUserLanguage(userId);
        
        logger.LogInformation("Cancel action received from user {UserId}", userId);

        await userState.RemoveStateAsync(userId);
        await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
        
        // Edit the message to remove the inline keyboard
        await messageService.EditMessageTextAsync(
            chatId,
            messageId,
            localization.GetString(ResourceKeys.Cancel, language),
            replyMarkUp: null,
            cancellationToken: cancellationToken);
            
        await messageService.ShowMainMenuAsync(
            chatId,
            language,
            menuType: MainMenuType.CheckPrompt,
            cancellationToken: cancellationToken);
    }
}