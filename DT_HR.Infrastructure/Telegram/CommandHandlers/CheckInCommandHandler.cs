using DT_HR.Application.Core.Abstractions.Enum;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Enumeration;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class CheckInCommandHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    IAttendanceRepository attendanceRepository,
    IUserRepository userRepository,
    ITelegramKeyboardService keyboardService,
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

        var user = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);
        if (user.HasValue && !user.Value.IsManager())
        {
            var today = DateOnly.FromDateTime(TimeUtils.Now);
            var attendance =
                await attendanceRepository.GetByUserAndDateAsync(user.Value.Id, today,
                    cancellationToken);

            if (attendance.HasValue)
            {
                if (attendance.Value.CheckInTime.HasValue && !attendance.Value.CheckOutTime.HasValue)
                {
                    await messageService.SendTextMessageAsync(chatId,
                        localization.GetString(ResourceKeys.AlreadyCheckedIn, language),
                        cancellationToken: cancellationToken);

                    await messageService.ShowMainMenuAsync(
                        chatId,
                        language,
                        menuType: MainMenuType.CheckedIn,
                        cancellationToken: cancellationToken);
                    return;

                }

                if (attendance.Value.CheckInTime.HasValue && attendance.Value.CheckOutTime.HasValue)
                {
                    await messageService.SendTextMessageAsync(chatId,
                        localization.GetString(ResourceKeys.AlreadyCheckedIn, language) +
                        localization.GetString(ResourceKeys.AlreadyCheckedOut, language),
                        cancellationToken: cancellationToken);

                    await messageService.ShowMainMenuAsync(
                        chatId,
                        language,
                        menuType: MainMenuType.CheckedOut,
                        cancellationToken: cancellationToken);
                    return;

                }

            }
        
            logger.LogInformation("Processing check-in command  for the user {UserId}", userId);

            var state = new UserState
            {
                CurrentAction = UserAction.CheckingIn,
                Language = language
            };
            await stateService.SetStateAsync(userId, state);

            var checkInProgress = localization.GetString(ResourceKeys.CheckInProcess, language);
            
            var optionsMessage = language switch
            {
                "ru" => "Выберите способ подтверждения местоположения:",
                "en" => "Choose how to verify your location:",
                _ => "Joylashuvingizni tasdiqlash usulini tanlang:"
            };

            var keyboard = keyboardService.GetCheckInOptionsKeyboard(language);

            await messageService.SendTextMessageAsync(
                chatId,
                $"{checkInProgress}\n\n{optionsMessage}",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
    }


}