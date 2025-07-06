using DT_HR.Application.Attendance.Queries.ExportAttendance;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.MessageHandlers;

public class ExportDateInputHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    IMediator mediator,
    ILogger<ExportDateInputHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        
        var state = await stateService.GetStateAsync(message.From!.Id);
        return state?.CurrentAction == UserAction.ExportCustomStartDate || state?.CurrentAction == UserAction.ExportCustomEndDate;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var text = message.Text!.Trim();
        
        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? await localization.GetUserLanguage(userId);

        try
        {
            if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString("InvalidDateFormat", language) + "\n" + localization.GetString("DateFormatExample", language),
                    cancellationToken: cancellationToken);
                return;
            }

            if (state!.CurrentAction == UserAction.ExportCustomStartDate)
            {
                // Store start date and ask for end date
                state.Data["StartDate"] = date;
                state.CurrentAction = UserAction.ExportCustomEndDate;
                await stateService.SetStateAsync(userId, state);

                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString("EnterEndDate", language),
                    cancellationToken: cancellationToken);
            }
            else if (state.CurrentAction == UserAction.ExportCustomEndDate)
            {
                var startDate = (DateOnly)state.Data["StartDate"];
                var endDate = date;

                if (startDate > endDate)
                {
                    await messageService.SendTextMessageAsync(
                        chatId,
                        localization.GetString("StartDateMustBeBeforeEndDate", language),
                        cancellationToken: cancellationToken);
                    return;
                }

                // Export the data
                await ExportAttendanceData(userId, startDate, endDate, language, chatId, cancellationToken);
                
                // Clear state
                await stateService.RemoveStateAsync(userId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling export date input for user {UserId}", userId);
            await messageService.SendTextMessageAsync(
                chatId,
                localization.GetString(ResourceKeys.ErrorOccurred, language),
                cancellationToken: cancellationToken);
        }
    }

    private async Task ExportAttendanceData(long userId, DateOnly startDate, DateOnly endDate, string language, long chatId, CancellationToken cancellationToken)
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
            
            await messageService.SendDocumentAsync(
                chatId,
                inputFile,
                caption: localization.GetString("AttendanceReportGenerated", language),
                cancellationToken: cancellationToken);

            // Show main menu
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