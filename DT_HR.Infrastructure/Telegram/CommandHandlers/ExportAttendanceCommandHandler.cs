using DT_HR.Application.Attendance.Queries.ExportAttendance;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class ExportAttendanceCommandHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    IUserRepository userRepository,
    ITelegramKeyboardService keyboardService,
    IMediator mediator,
    ILogger<ExportAttendanceCommandHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        var state = await stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? await localization.GetUserLanguage(message.From!.Id);
        var text = message.Text.ToLower();
        var exportText = localization.GetString("ExportAttendance", language).Trim().ToLower();

        return text == "/export" || text == exportText;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? await localization.GetUserLanguage(userId);

        var user = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);

        if (user.HasNoValue || !user.Value.IsManager())
        {
            await messageService.SendTextMessageAsync(chatId,
                localization.GetString(ResourceKeys.OptionNotAvailable, language),
                cancellationToken: cancellationToken);
            return;
        }

        // Set user state for export flow
        await stateService.SetStateAsync(userId, new UserState
        {
            Language = language,
            CurrentAction = UserAction.ExportDateSelection,
            Data = new Dictionary<string, object>()
        });

        // Show date range selection options
        var keyboard = keyboardService.CreateDateRangeSelectionKeyboard(language);
        
        await messageService.SendTextMessageAsync(
            chatId,
            localization.GetString("SelectDateRange", language),
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}