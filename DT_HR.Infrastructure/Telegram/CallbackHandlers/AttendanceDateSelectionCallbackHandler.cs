using System.Text;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CallbackHandlers;

public class AttendanceDateSelectionCallbackHandler(
    ITelegramMessageService messageService,
    ITelegramCalendarService calendarService,
    IUserStateService stateService,
    ILocalizationService localization,
    IUserRepository userRepository,
    IAttendanceReportService reportService,
    ILogger<AttendanceDateSelectionCallbackHandler> logger) : ITelegramCallbackQuery
{
    public async Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        if (callbackQuery.Data == null) return false;
        
        var state = await stateService.GetStateAsync(callbackQuery.From.Id);
        if (state?.CurrentAction != UserAction.SelectingAttendanceDate) return false;

        return calendarService.IsDateCallback(callbackQuery.Data) || 
               calendarService.IsNavigationCallback(callbackQuery.Data);
    }

    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var callbackData = callbackQuery.Data!;

        var state = await stateService.GetStateAsync(userId);
        if (state == null) return;

        var language = state.Language;
        var user = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);

        if (user.HasNoValue || !user.Value.IsManager())
        {
            await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, 
                localization.GetString(ResourceKeys.OptionNotAvailable, language),
                cancellationToken: cancellationToken);
            return;
        }

        logger.LogInformation("Processing attendance date selection callback for user {UserId}: {CallbackData}", userId, callbackData);

        await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);


        if (calendarService.IsDateCallback(callbackData))
        {
            logger.LogInformation("Detected date callback for attendance: {CallbackData}", callbackData);
            var selectedDate = calendarService.ParseDateFromCallback(callbackData);
            if (selectedDate.HasValue)
            {
                await stateService.RemoveStateAsync(userId);
                await ShowAttendanceDetailsForDate(chatId, messageId, selectedDate.Value, language, cancellationToken);
            }
        }

        else if (calendarService.IsNavigationCallback(callbackData))
        {
            logger.LogInformation("Detected navigation callback for attendance: {CallbackData}", callbackData);
            var updatedCalendar = await calendarService.HandleNavigationAsync(callbackData);
            
            await messageService.EditMessageReplyMarkupAsync(
                chatId,
                messageId,
                updatedCalendar,
                cancellationToken: cancellationToken);
        }
        else
        {
            logger.LogWarning("Unknown attendance calendar callback: {CallbackData}", callbackData);
        }
    }

    private async Task ShowAttendanceDetailsForDate(long chatId, int messageId, DateTime selectedDate, string language, CancellationToken cancellationToken)
    {
        var list = await reportService.GetDetailedAttendance(DateOnly.FromDateTime(selectedDate), cancellationToken);
        var attendanceDetailsText = localization.GetString(ResourceKeys.AttendanceRate, language);
        var totalEmployeesText = localization.GetString(ResourceKeys.TotalEmployees, language);
        
        var sb = new StringBuilder();

        sb.AppendLine($"📊 *{attendanceDetailsText}*");
        sb.AppendLine($"📅 {selectedDate:dd MMMM yyyy}");
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
                
                if (item.WorkDuration.HasValue)
                {
                    var duration = item.WorkDuration.Value;
                    var hours = (int)duration.TotalHours;
                    var minutes = duration.Minutes;
                    sb.AppendLine($"  ⏱️ {localization.GetString(ResourceKeys.WorkDuration,language)}: `{hours:D2}:{minutes:D2}`");
                }
                    
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

        await messageService.EditMessageTextAsync(chatId, messageId, sb.ToString(), replyMarkUp: null, cancellationToken: cancellationToken);
        
        await messageService.ShowMainMenuAsync(
            chatId, 
            language, 
            isManager: true,
            cancellationToken: cancellationToken);
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