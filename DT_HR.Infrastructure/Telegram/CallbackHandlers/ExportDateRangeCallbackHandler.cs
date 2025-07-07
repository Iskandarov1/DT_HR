using DT_HR.Application.Attendance.Queries.ExportAttendance;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CallbackHandlers;

public class ExportDateRangeCallbackHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    ITelegramKeyboardService keyboardService,
    IMediator mediator,
    ILogger<ExportDateRangeCallbackHandler> logger) : ITelegramCallbackQuery
{
    public Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(callbackQuery.Data?.StartsWith("export_date_") == true);
    }

    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var data = callbackQuery.Data!;

        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? await localization.GetUserLanguage(userId);

        try
        {
            if (data == "export_date_today")
            {
                var today = DateOnly.FromDateTime(TimeUtils.Now);
                await ExportAttendanceData(userId, today, today, language, chatId,messageId, cancellationToken);
            }
            else if (data == "export_date_week")
            {
                var today = DateOnly.FromDateTime(TimeUtils.Now);
                var startOfWeek = today.AddDays(-7);
                await ExportAttendanceData(userId, startOfWeek, today, language, chatId,messageId, cancellationToken);
            }
            else if (data == "export_date_month")
            {
                var today = DateOnly.FromDateTime(TimeUtils.Now);
                var startOfMonth = new DateOnly(today.Year, today.Month, 1);
                await ExportAttendanceData(userId, startOfMonth, today, language, chatId,messageId, cancellationToken);
            }
            else if (data == "export_date_custom")
            {

                await stateService.SetStateAsync(userId, new UserState
                {
                    Language = language,
                    CurrentAction = UserAction.ExportCustomStartDate,
                    Data = new Dictionary<string, object>()
                });

                await messageService.EditMessageTextAsync(
                    chatId,
                    messageId,
                    localization.GetString("EnterStartDate", language),
                    cancellationToken: cancellationToken);
                return;
            }
            
            await stateService.RemoveStateAsync(userId);
            
            await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling export date range callback for user {UserId}", userId);
            await messageService.SendTextMessageAsync(
                chatId,
                localization.GetString(ResourceKeys.ErrorOccurred, language),
                cancellationToken: cancellationToken);
        }
    }

    private async Task ExportAttendanceData(long userId, DateOnly startDate, DateOnly endDate, string language, long chatId,int messageId, CancellationToken cancellationToken)
    {
        try
        {
            // Send processing message
            await messageService.SendTextMessageAsync(
                chatId,
                localization.GetString("GeneratingReport", language),
                cancellationToken: cancellationToken);

            // Execute the query
            var query = new ExportAttendanceQuery(userId, startDate, endDate, language);
            var result = await mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString("ExportFailed", language),
                    cancellationToken: cancellationToken);
                return;
            }

            // Send Excel file
            var fileName = $"Attendance_Report_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}.xlsx";
            
            using var stream = new MemoryStream(result.Value);
            var inputFile = InputFile.FromStream(stream, fileName);
            var dateSelected = localization.GetString(ResourceKeys.DateSelected, language);
            
            await messageService.SendDocumentAsync(
                chatId,
                inputFile,
                caption: localization.GetString("AttendanceReportGenerated", language),
                cancellationToken: cancellationToken);
            await messageService.EditMessageTextAsync(
                chatId,
                messageId,
                dateSelected,
                replyMarkUp: null,
                cancellationToken: cancellationToken);
            
            await messageService.ShowMainMenuAsync(
                chatId,
                language,
                isManager: true,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting attendance data for user {UserId}", userId);
            await messageService.SendTextMessageAsync(
                chatId,
                localization.GetString(ResourceKeys.ErrorOccurred, language),
                cancellationToken: cancellationToken);
        }
    }
}