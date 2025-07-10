using System.Text;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DT_HR.Infrastructure.Telegram.CallbackHandlers;

public class AttendanceToggleCallbackHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    IUserRepository userRepository,
    IAttendanceReportService reportService,
    ILogger<AttendanceToggleCallbackHandler> logger) : ITelegramCallbackQuery
{
    public Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(callbackQuery.Data == "attendance:details" || callbackQuery.Data == "attendance:stats");
    }

    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? await localization.GetUserLanguage(userId);

        var user = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);

        if (user.HasNoValue || !user.Value.IsManager())
        {
            await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, 
                localization.GetString(ResourceKeys.OptionNotAvailable, language),
                cancellationToken: cancellationToken);
            return;
        }

        await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

        if (callbackQuery.Data == "attendance:details")
        {
            await ShowAttendanceDetails(chatId, messageId, language, cancellationToken);
        }
        else if (callbackQuery.Data == "attendance:stats")
        {
            await ShowAttendanceStats(chatId, messageId, language, cancellationToken);
        }
    }

    private async Task ShowAttendanceDetails(long chatId, int messageId, string language, CancellationToken cancellationToken)
    {
        var list = await reportService.GetDetailedAttendance(DateOnly.FromDateTime(TimeUtils.Now), cancellationToken);
        var attendanceDetailsText = localization.GetString(ResourceKeys.AttendanceRate, language);
        var totalEmployeesText = localization.GetString(ResourceKeys.TotalEmployees, language);
        var statsButtonText = localization.GetString(ResourceKeys.AttendanceStats, language);
        
        var sb = new StringBuilder();

        sb.AppendLine($"📊 *{attendanceDetailsText}*");
        sb.AppendLine($"📅 {TimeUtils.Now:dd MMMM yyyy}");
        sb.AppendLine();

        var groupedByStatus = list.GroupBy(x => x.Status).OrderBy(g => GetStatusOrder(g.Key));
        
        foreach (var statusGroup in groupedByStatus)
        {
            var statusEmoji = GetStatusEmoji(statusGroup.Key);
            var statusTitle = statusGroup.Key.ToLower() switch
            {
                "present" => localization.GetString(ResourceKeys.Present, language),
                "ontheway" => localization.GetString(ResourceKeys.OnTheWay, language),
                "absent" => localization.GetString(ResourceKeys.Absent, language),
                "norecord" => localization.GetString(ResourceKeys.NoRecord, language),
                _ => statusGroup.Key
            };
            
            sb.AppendLine($"{statusEmoji} *{statusTitle}* ({statusGroup.Count()})");
            sb.AppendLine();
            
            foreach (var item in statusGroup.OrderBy(x => x.Name))
            {
                var checkIn = item.CheckInTime?.ToString("HH:mm") ?? "──:──";
                var checkOut = item.CheckOutTime?.ToString("HH:mm") ?? "──:──";
                var lateIndicator = item.IsLate == true ? " ⏰" : "";
                
                sb.AppendLine($"  👤 *{item.Name}*{lateIndicator}");
                sb.AppendLine($"  🕐 In: `{checkIn}` • Out: `{checkOut}`");
                    
                if (!string.IsNullOrEmpty(item.AbsenceReason))
                {
                    sb.AppendLine($"  📝 {item.AbsenceReason}");
                }
                    
                if (item.EstimatedArrival.HasValue)
                {
                    var eta = item.EstimatedArrival.Value.ToString("HH:mm");
                    sb.AppendLine($"  🕒 {localization.GetString(ResourceKeys.Expected, language)}: `{eta}`");
                }
                
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine($"📋 *{totalEmployeesText}:* {list.Count}");

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData(statsButtonText, "attendance:stats")
        });

        await messageService.EditMessageTextAsync(chatId, messageId, sb.ToString(), replyMarkUp: inlineKeyboard, cancellationToken: cancellationToken);
    }

    private async Task ShowAttendanceStats(long chatId, int messageId, string language, CancellationToken cancellationToken)
    {
        var report = await reportService.GetDailyAttendanceReport(DateOnly.FromDateTime(TimeUtils.Now), cancellationToken);
        
        var title = localization.GetString(ResourceKeys.AttendanceStats, language);
        var totalText = localization.GetString(ResourceKeys.TotalEmployees, language);
        var presentText = localization.GetString(ResourceKeys.Present, language);
        var lateText = localization.GetString(ResourceKeys.Late, language);
        var absentText = localization.GetString(ResourceKeys.Absent, language);
        var onTheWayText = localization.GetString(ResourceKeys.OnTheWay, language);
        var noRecord = localization.GetString(ResourceKeys.NoRecord, language);
        var detailsButtonText = localization.GetString(ResourceKeys.AttendanceDetails, language);
        
        var attendanceRate = report.TotalEmployees > 0 
            ? Math.Round((double)(report.Present + report.Late) / report.TotalEmployees * 100, 1) 
            : 0;

        var rateEmoji = attendanceRate switch
        {
            >= 95 => "🟢",
            >= 85 => "🟡", 
            >= 70 => "🟠",
            _ => "🔴"
        };

        var text = $" *{title}*\n" +
                   $"📅 {report.Date:dd MMMM yyyy}\n\n" +
                   $"👥 *{totalText}:* {report.TotalEmployees}\n\n" +
                   $"✅ *{presentText}* — {report.Present}\n" +
                   $"⏰ *{lateText}* — {report.Late}\n" +
                   $"*{onTheWayText}* — {report.OnTheWay}\n" +
                   $"❌ *{absentText}* — {report.Absent}\n" +
                   $"❓*{noRecord}* - {report.NotCheckedIn}\n\n " +
                   $"{rateEmoji} *Attendance Rate:* {attendanceRate:F1}%";

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData(detailsButtonText, "attendance:details")
        });

        await messageService.EditMessageTextAsync(chatId, messageId, text, replyMarkUp: inlineKeyboard, cancellationToken: cancellationToken);
    }

    private static string GetStatusEmoji(string status) => status.ToLower() switch
    {
        "present" => "✅",
        "absent" => "❌",
        "ontheway" => "",
        "norecord" => "🔴",
        _ => "❓"
    };

    private static int GetStatusOrder(string status) => status.ToLower() switch
    {
        "present" => 1,
        "ontheway" => 2,
        "absent" => 3,
        "norecord" => 4,
        _ => 5
    };
}