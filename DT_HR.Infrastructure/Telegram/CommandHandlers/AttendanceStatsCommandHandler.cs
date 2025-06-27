using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class AttendanceStatsCommandHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    IUserRepository userRepository,
    IAttendanceReportService reportService,
    ILogger<AttendanceStatsCommandHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        var state = await stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? await localization.GetUserLanguage(message.From!.Id);
        var text = message.Text.ToLower();
        var startsText = localization.GetString(ResourceKeys.AttendanceStats, language).ToLower();

        return text == "/stats" || text == startsText;
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

        var report =
            await reportService.GetDailyAttendanceReport(DateOnly.FromDateTime(TimeUtils.Now), cancellationToken);
        var text = $"*{report.Date.ToString("yyyy-MM-dd") }*\n" +
                   $"Total: {report.TotalEmployees}\n" +
                   $"Present: {report.Present}\nLate: {report.Late}\n" +
                   $"Absent: {report.Absent}\nOn The Way: {report.OnTheWay}";

        await messageService.SendTextMessageAsync(chatId, text, cancellationToken: cancellationToken);

        await messageService.ShowMainMenuAsync(
            chatId,
            localization.GetString(ResourceKeys.PleaseSelectFromMenu, language),
            language, 
            isManager:true,
            cancellationToken : cancellationToken);



    }
}