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
        var startsText = localization.GetString(ResourceKeys.AttendanceStats, language).Trim().ToLower();

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
        
        var title = localization.GetString(ResourceKeys.AttendanceStats, language);
        var totalText = localization.GetString(ResourceKeys.TotalEmployees, language);
        var presentText = localization.GetString(ResourceKeys.Present, language);
        var lateText = localization.GetString(ResourceKeys.Late, language);
        var absentText = localization.GetString(ResourceKeys.Absent, language);
        var onTheWayText = localization.GetString(ResourceKeys.OnTheWay, language);
        
        var text = $"*{title}*\n" +
                   $"📅 *{report.Date.ToString("yyyy-MM-dd")}*\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"👥 *{totalText}:* `{report.TotalEmployees}`\n\n" +
                   $"✅ *{presentText}:* `{report.Present}`\n" +
                   $"⏰ *{lateText}:* `{report.Late}`\n" +
                   $"❌ *{absentText}:* `{report.Absent}`\n" +
                   $"🚗 *{onTheWayText}:* `{report.OnTheWay}`\n\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━\n";

        await messageService.SendTextMessageAsync(chatId, text, cancellationToken: cancellationToken);
//                   $"📈 *Attendance Rate:* `{(report.TotalEmployees > 0 ? Math.Round((double)(report.Present + report.Late) / report.TotalEmployees * 100, 1) : 0)}%`";

        await messageService.ShowMainMenuAsync(
            chatId,
            language, 
            isManager:true,
            cancellationToken : cancellationToken);



    }
}