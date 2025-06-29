using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CallbackHandlers;

public class LanguageSelectionCallbackHandler(
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService, 
    IUserStateService stateService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
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
        
        var state = await stateService.GetStateAsync(userId) ?? new UserState();
        if (state != null && state.CurrentAction == UserAction.SelectingLanguage)
        {
            var maybeUser = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);
            bool isManager = false;
            if (maybeUser.HasValue)
            {
                var user = maybeUser.Value;
                user.SetLanguage(selectedLanguage);
                userRepository.Update(user);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                isManager = user.IsManager();
            }

            await stateService.RemoveStateAsync(userId);
            await messageService.EditMessageTextAsync(
                chatId,
                messageId,
                localization.GetString(ResourceKeys.PleaseSelectFromMenu, selectedLanguage),
                cancellationToken: cancellationToken);
            await messageService.ShowMainMenuAsync(chatId,
                localization.GetString(ResourceKeys.PleaseSelectFromMenu, selectedLanguage),
                selectedLanguage, 
                isManager,
                cancellationToken);

        }
        else
        {
            state ??= new UserState();
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
}