using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.CallbackHandlers;

public class LanguageSelectionCallbackHandler(
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService, 
    IUserStateService stateService,
    ILocalizationService localization ,
    ILogger<LanguageSelectionCallbackHandler> logger) : ITelegramCallbackQuery
{
    public Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(callbackQuery.Data?.StartsWith("lang:") ?? false);
    }

    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var selectedLanguage = callbackQuery.Data!.Split(':')[1];
        
        logger.LogInformation("User {UserId} selected language {Language}",userId,selectedLanguage);

        await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
        
        // this will update user state with selected language
        var state = await stateService.GetStateAsync(userId) ?? new UserState();
        state.Language = selectedLanguage;
        state.CurrentAction = UserAction.Registering;
        await stateService.SetStateAsync(userId, state);

        //getting the phone keyboard options
        var keyborad = keyboardService.GetPhoneNumberOptionsKeyboard(selectedLanguage);

        var registrationPrompt = localization.GetString(ResourceKeys.RegistrationPrompt, selectedLanguage);
        var chooseHowToShare = localization.GetString(ResourceKeys.ChooseHowToShare, selectedLanguage);

        var message = $"{registrationPrompt}\n\n{chooseHowToShare}";

        await messageService.EditMessageTextAsync(
            chatId,
            messageId,
            message,
            cancellationToken: cancellationToken);

        await messageService.SendTextMessageAsync(
            chatId, 
            "👇", 
            keyborad,
            cancellationToken);





    }
}