using DT_HR.Application.Core.Abstractions.Enum;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.MessageHandlers;

public class ManagerSettingsMessageHandler(
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService,
    IUserStateService stateService,
    IUserRepository userRepository,
    ILocalizationService localization,
    ILogger<ManagerSettingsMessageHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        
        var userId = message.From!.Id;
        var maybeUser = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);
        if (maybeUser.HasNoValue || !maybeUser.Value.IsManager()) return false;

        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? await localization.GetUserLanguage(userId);
        var text = message.Text.Trim();
        
        var selectLanguageText = localization.GetString(ResourceKeys.SelectLanguage, language);
        var workTimeSettingsText = localization.GetString(ResourceKeys.WorkTimeSettings, language);
        var backText = localization.GetString(ResourceKeys.Back, language);
        
        return text == selectLanguageText || text == workTimeSettingsText || text == backText;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var text = message.Text!.Trim();
        
        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? await localization.GetUserLanguage(userId);

        logger.LogInformation("Processing manager settings button for user {UserId}: {ButtonText}", userId, text);

        var selectLanguageText = localization.GetString(ResourceKeys.SelectLanguage, language);
        var workTimeSettingsText = localization.GetString(ResourceKeys.WorkTimeSettings, language);
        var backText = localization.GetString(ResourceKeys.Back, language);

        logger.LogInformation("Button pressed: '{ButtonText}', comparing with: Language='{LanguageText}', WorkTime='{WorkTimeText}', Back='{BackText}'", 
            text, selectLanguageText, workTimeSettingsText, backText);

        if (text == selectLanguageText)
        {
            logger.LogInformation("Handling language selection for user {UserId}", userId);
            await HandleLanguageSelection(chatId, language, cancellationToken);
        }
        else if (text == workTimeSettingsText)
        {
            logger.LogInformation("Handling work time settings for user {UserId}", userId);
            await HandleWorkTimeSettings(chatId, language, cancellationToken);
        }
        else if (text == backText)
        {
            logger.LogInformation("Handling back to menu for user {UserId}", userId);
            await HandleBackToMenu(userId, chatId, language, cancellationToken);
        }
        else
        {
            logger.LogWarning("Unknown settings button pressed by user {UserId}: '{ButtonText}'", userId, text);
        }
    }

    private async Task HandleLanguageSelection(long chatId, string language, CancellationToken cancellationToken)
    {
        var keyboard = keyboardService.GetLanguageSelectionKeyboard();
        var prompt = localization.GetString(ResourceKeys.SelectLanguage, language);
        await messageService.SendTextMessageAsync(chatId, prompt, keyboard, cancellationToken);
    }

    private async Task HandleWorkTimeSettings(long chatId, string language, CancellationToken cancellationToken)
    {
        var keyboard = keyboardService.GetWorkTimeSettingsKeyboard(language);
        var prompt = localization.GetString(ResourceKeys.WorkTimeSettings, language);
        await messageService.SendTextMessageAsync(chatId, prompt, keyboard, cancellationToken);
    }

    private async Task HandleBackToMenu(long userId, long chatId, string language, CancellationToken cancellationToken)
    {
        // Get the stored menu type from state
        var state = await stateService.GetStateAsync(userId);
        var menuTypeString = state?.Data.GetValueOrDefault("previousMenuType")?.ToString();
        var menuType = MainMenuType.Default;
        
        if (!string.IsNullOrEmpty(menuTypeString) && Enum.TryParse<MainMenuType>(menuTypeString, out var parsedMenuType))
        {
            menuType = parsedMenuType;
        }
        
        await stateService.RemoveStateAsync(userId);
        await messageService.ShowMainMenuAsync(chatId, language, menuType: menuType, cancellationToken: cancellationToken);
    }
}