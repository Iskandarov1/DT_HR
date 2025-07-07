using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CallbackHandlers;

public class WorkTimeSettingsCallbackHandler(
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService, 
    IUserStateService stateService,
    IUserRepository userRepository,
    ILocalizationService localization,
    ILogger<WorkTimeSettingsCallbackHandler> logger) : ITelegramCallbackQuery
{
    public Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var data = callbackQuery.Data;
        return Task.FromResult(data is "lang_select" or "work_time_settings" or "work_time_start" or "work_time_end" or "back_to_settings");
    }

    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var callbackData = callbackQuery.Data!;
        
        logger.LogInformation("User {UserId} selected work time setting: {CallbackData}", userId, callbackData);

        await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
        
        // Get user and language
        var maybeUser = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);
        if (maybeUser.HasNoValue || !maybeUser.Value.IsManager())
        {
            var errorMessage = "This feature is only available for managers.";
            await messageService.EditMessageTextAsync(chatId, messageId, errorMessage, cancellationToken: cancellationToken);
            return;
        }

        var user = maybeUser.Value;
        var language = user.Language;

        switch (callbackData)
        {
            case "lang_select":
                await HandleLanguageSelectAsync(chatId, messageId, language, cancellationToken);
                break;
                
            case "work_time_settings":
                await HandleWorkTimeSettingsAsync(chatId, messageId, user, language, cancellationToken);
                break;
                
            case "work_time_start":
                await HandleWorkTimeStartAsync(chatId, messageId, userId, language, cancellationToken);
                break;
                
            case "work_time_end":
                await HandleWorkTimeEndAsync(chatId, messageId, userId, language, cancellationToken);
                break;
                
            case "back_to_settings":
                await HandleBackToSettingsAsync(chatId, messageId, language, cancellationToken);
                break;
        }
    }

    private async Task HandleLanguageSelectAsync(long chatId, int messageId, string language, CancellationToken cancellationToken)
    {
        var keyboard = keyboardService.GetLanguageSelectionKeyboard();
        var prompt = localization.GetString(ResourceKeys.SelectLanguage, language);
        
        await messageService.EditMessageTextAsync(
            chatId, 
            messageId, 
            prompt, 
            keyboard, 
            cancellationToken);
    }

    private async Task HandleWorkTimeSettingsAsync(long chatId, int messageId, Domain.Entities.User user, string language, CancellationToken cancellationToken)
    {
        var keyboard = keyboardService.GetWorkTimeSettingsKeyboard(language);
        var currentHoursText = localization.GetString(ResourceKeys.CurrentWorkHours, language);
        var message = string.Format(currentHoursText, 
            user.WorkStartTime.ToString("HH:mm"), 
            user.WorkEndTime.ToString("HH:mm"));
        
        await messageService.EditMessageTextAsync(
            chatId, 
            messageId, 
            message, 
            keyboard, 
            cancellationToken);
    }

    private async Task HandleWorkTimeStartAsync(long chatId, int messageId, long userId, string language, CancellationToken cancellationToken)
    {
        // Set user state to capture time input
        var state = new UserState
        {
            CurrentAction = UserAction.SettingWorkStartTime,
            Language = language
        };
        await stateService.SetStateAsync(userId, state);
        
        var prompt = localization.GetString(ResourceKeys.EnterWorkStartTime, language);
        var cancelKeyboard = keyboardService.GetCancelInlineKeyboard(language);
        
        await messageService.EditMessageTextAsync(
            chatId, 
            messageId, 
            prompt, 
            cancelKeyboard, 
            cancellationToken);
    }

    private async Task HandleWorkTimeEndAsync(long chatId, int messageId, long userId, string language, CancellationToken cancellationToken)
    {
        // Set user state to capture time input
        var state = new UserState
        {
            CurrentAction = UserAction.SettingWorkEndTime,
            Language = language
        };
        await stateService.SetStateAsync(userId, state);
        
        var prompt = localization.GetString(ResourceKeys.EnterWorkEndTime, language);
        var cancelKeyboard = keyboardService.GetCancelInlineKeyboard(language);
        
        await messageService.EditMessageTextAsync(
            chatId, 
            messageId, 
            prompt, 
            cancelKeyboard, 
            cancellationToken);
    }

    private async Task HandleBackToSettingsAsync(long chatId, int messageId, string language, CancellationToken cancellationToken)
    {
        var keyboard = keyboardService.GetManagerSettingsKeyboard(language);
        var prompt = localization.GetString(ResourceKeys.Settings, language);
        
        await messageService.EditMessageTextAsync(
            chatId, 
            messageId, 
            prompt, 
            keyboard, 
            cancellationToken);
    }
}