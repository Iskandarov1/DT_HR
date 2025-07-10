using System.Text;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class AttendanceDetailsCommandHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    IUserRepository repository,
    IAttendanceReportService reportService, 
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

        var list = await reportService.GetDetailedAttendance(DateOnly.FromDateTime(TimeUtils.Now), cancellationToken);
        var attendanceDetailsText =  localization.GetString(ResourceKeys.AttendanceRate, language);
        var totalEmployeesText = localization.GetString(ResourceKeys.TotalEmployees, language);
        var present = localization.GetString(ResourceKeys.Present, language);
        var lateText = localization.GetString(ResourceKeys.Late, language);
        var onTheWayText = localization.GetString(ResourceKeys.OnTheWay, language);
        var absentText = localization.GetString(ResourceKeys.Absent, language);



        var sb = new StringBuilder();

        sb.AppendLine($"📊 *{attendanceDetailsText}*");
        sb.AppendLine($"📅 {TimeUtils.Now:dd MMMM yyyy}");
        sb.AppendLine();


        var groupedByStatus =
            list.GroupBy(x => x.Status)
                .OrderBy(g => GetStatusOrder(g.Key));

        foreach (var statusGroup in groupedByStatus)
        {
            var statusEmoji = GetStatusEmoji(statusGroup.Key);
            var statusTitle = statusGroup.Key.ToLower() switch
            {
                "present" => localization.GetString(ResourceKeys.Present, language),
                "late" => localization.GetString(ResourceKeys.Late, language),
                "on the way" => localization.GetString(ResourceKeys.OnTheWay, language),
                "absent" => localization.GetString(ResourceKeys.Absent, language),
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
                    sb.AppendLine($"  🕒 {localization.GetString(ResourceKeys.Expected,language)}: `{eta}`");
                }
                
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine($"📋 *{totalEmployeesText}:* {list.Count}");

        await messageService.SendTextMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken: cancellationToken);
        await messageService.ShowMainMenuAsync(
            chatId, 
            language, 
            isManager:true,
            cancellationToken:cancellationToken);
    }

    private static string GetStatusEmoji(string status) => status.ToLower() switch
    {
        "present" => "✅",
        "absent" => "❌",
        "late" => "⏰",
        "on the way" => "🚗",
        _ => "❓"
    };

    private static int GetStatusOrder(string status) => status.ToLower() switch
    {
        "present" => 1,
        "late" => 2,
        "on the way" => 3,
        "absent" => 4,
        _ => 5
    };
}