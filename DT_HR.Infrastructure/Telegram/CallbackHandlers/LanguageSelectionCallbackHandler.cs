using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Enum;
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
        
        var maybeUser = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);
        if (maybeUser.HasValue)
        {

            var user = maybeUser.Value;
            user.SetLanguage(selectedLanguage);
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            

            var state = await stateService.GetStateAsync(userId);
            var previousMenuTypeString = state?.Data.GetValueOrDefault("previousMenuType")?.ToString();
            var menuType = MainMenuType.Default;
            if (!string.IsNullOrEmpty(previousMenuTypeString) && Enum.TryParse<MainMenuType>(previousMenuTypeString, out var parsedMenuType))
            {
                menuType = parsedMenuType;
            }


            await stateService.RemoveStateAsync(userId);
            

            var confirmationMessage = localization.GetString(ResourceKeys.LanguageChanged, selectedLanguage);
            await messageService.EditMessageTextAsync(
                chatId,
                messageId,
                confirmationMessage,
                replyMarkUp: null,
                cancellationToken: cancellationToken);
            
            await messageService.ShowMainMenuAsync(
                chatId,
                selectedLanguage,
                menuType: menuType,
                cancellationToken: cancellationToken);
        }
        else
        {
            var state = new UserState
            {
                Language = selectedLanguage,
                CurrentAction = UserAction.Registering
            };
            state.Data["step"] = "phone";
            await stateService.SetStateAsync(userId, state);

            var keyboard = keyboardService.GetPhoneNumberOptionsKeyboard(selectedLanguage);
            var registrationPrompt = localization.GetString(ResourceKeys.RegistrationPrompt, selectedLanguage);
            var chooseHowtoShare = localization.GetString(ResourceKeys.ChooseHowToShare, selectedLanguage);
            var message = $"{registrationPrompt}\n\n{chooseHowtoShare}";

            await messageService.EditMessageTextAsync(
                chatId,
                messageId,
                message,
                replyMarkUp: null,
                cancellationToken: cancellationToken);
            await messageService.SendTextMessageAsync(
                chatId, 
                "👇", 
                keyboard, 
                cancellationToken);
        }
    }
}