using DT_HR.Application.Core.Abstractions.Enum;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class SettingsCommandHandler(
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService,
    IUserStateService stateService,
    IUserRepository userRepository,
    IAttendanceRepository attendanceRepository,
    ILocalizationService localization,
    ILogger<SettingsCommandHandler> logger) : ITelegramService   
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        var state = await stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? await localization.GetUserLanguage(message.From!.Id);
        var text = message.Text.ToLower();
        var settingsText = localization.GetString(ResourceKeys.Settings, language).ToLower();
        return text == "/settings" || text == settingsText || text.Contains("settings");

    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        
        logger.LogInformation("Processing settings command for the user {UserId}",userId);

        var state = await stateService.GetStateAsync(userId) ?? new UserState();
        var language = state.Language ?? await localization.GetUserLanguage(userId);
        state.CurrentAction = UserAction.SelectingLanguage;
        state.Language = language;
        
        // Store the current menu type in state for later restoration
        var menuType = MainMenuType.Default;
        var maybeUser = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);

        if (maybeUser.HasValue)
        {
            var today = DateOnly.FromDateTime(TimeUtils.Now);
            var attendance =
                await attendanceRepository.GetByUserAndDateAsync(maybeUser.Value.Id, today, cancellationToken);
            
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
        
        //storing teh menu type for restoration after language selection
        state.Data["previousMenuType"] = menuType.ToString();
        await stateService.SetStateAsync(userId, state);
        
        var keyboard = keyboardService.GetLanguageSelectionKeyboard();
        var prompt = localization.GetString(ResourceKeys.PleaseSelectLanguage, language);
        await messageService.SendTextMessageAsync(chatId, prompt, keyboard, cancellationToken);
        
    }
}