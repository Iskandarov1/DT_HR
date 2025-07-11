using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class AttendanceDetailsCommandHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    IUserRepository repository,
    ITelegramCalendarService calendarService,
    ILogger<AttendanceDetailsCommandHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        var state = await stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? await localization.GetUserLanguage(message.From!.Id);
        var text = message.Text.ToLower();
        var startsText = localization.GetString(ResourceKeys.AttendanceDetails, language).Trim().ToLower();

        return text == "/details" || text == startsText;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? await localization.GetUserLanguage(userId);

        var user = await repository.GetByTelegramUserIdAsync(userId, cancellationToken);

        if (user.HasNoValue || !user.Value.IsManager())
        {
            await messageService.SendTextMessageAsync(chatId,
                localization.GetString(ResourceKeys.OptionNotAvailable, language),
                cancellationToken: cancellationToken);
            return;
        }


        var newState = new UserState
        {
            CurrentAction = UserAction.SelectingAttendanceDate,
            Language = language,
            Data = []
        };
        await stateService.SetStateAsync(userId, newState);

        var selectDateText = localization.GetString(ResourceKeys.SelectDate, language);
        var calendar = calendarService.GetCalendarKeyboard();

        await messageService.SendTextMessageAsync(
            chatId,
            $"📅 *{selectDateText}*",
            replyMarkup: calendar,
            cancellationToken: cancellationToken);
    }

}