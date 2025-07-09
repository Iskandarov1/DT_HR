using DT_HR.Application.Attendance.Queries.ExportAttendance;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CallbackHandlers;

public class ExportCalendarCallbackHandler(
    ITelegramMessageService messageService,
    ITelegramCalendarService calendarService,
    IUserStateService stateService,
    ILocalizationService localizationService,
    ITelegramKeyboardService keyboardService,
    IMediator mediator,
    ILogger<ExportCalendarCallbackHandler> logger) : ITelegramCallbackQuery
{
    public async Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        if (callbackQuery.Data == null) return false;
        
        var state = await stateService.GetStateAsync(callbackQuery.From.Id);
        if (state?.CurrentAction != UserAction.ExportSelectingStartDate && 
            state?.CurrentAction != UserAction.ExportSelectingEndDate) return false;

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

        logger.LogInformation("Processing export calendar callback for user {UserId}: {CallbackData}", userId, callbackData);

        try
        {
            // Handle date selection
            if (calendarService.IsDateCallback(callbackData))
            {
                logger.LogInformation("Detected date callback: {CallbackData}", callbackData);
                var selectedDate = calendarService.ParseDateFromCallback(callbackData);
                if (selectedDate.HasValue)
                {
                    if (state.CurrentAction == UserAction.ExportSelectingStartDate)
                    {
                        // Store start date and show calendar for end date selection
                        state.Data["StartDate"] = DateOnly.FromDateTime(selectedDate.Value);
                        state.CurrentAction = UserAction.ExportSelectingEndDate;
                        await stateService.SetStateAsync(userId, state);

                        var endCalendar = calendarService.GetCalendarKeyboard(selectedDate.Value);
                        await messageService.EditMessageTextAsync(
                            chatId,
                            messageId,
                            $"{localizationService.GetString("StartDateSelected", language)}: {selectedDate.Value:dd-MM-yyyy}\n\n{localizationService.GetString("SelectEndDate", language)}",
                            endCalendar,
                            cancellationToken: cancellationToken);
                    }
                    else if (state.CurrentAction == UserAction.ExportSelectingEndDate)
                    {
                        var startDate = (DateOnly)state.Data["StartDate"];
                        var endDate = DateOnly.FromDateTime(selectedDate.Value);

                        if (startDate > endDate)
                        {
                            await messageService.EditMessageTextAsync(
                                chatId,
                                messageId,
                                localizationService.GetString("StartDateMustBeBeforeEndDate", language),
                                calendarService.GetCalendarKeyboard(selectedDate.Value),
                                cancellationToken: cancellationToken);
                            return;
                        }

                        // Export the data
                        await messageService.EditMessageTextAsync(
                            chatId,
                            messageId,
                            $"{localizationService.GetString("StartDateSelected", language)}: {startDate:dd-MM-yyyy}\n{localizationService.GetString("EndDateSelected", language)}: {endDate:dd-MM-yyyy}\n\n{localizationService.GetString("GeneratingReport", language)}",
                            replyMarkUp: null,
                            cancellationToken: cancellationToken);

                        await ExportAttendanceData(userId, startDate, endDate, language, chatId, cancellationToken);
                        
                        // Clear state
                        await stateService.RemoveStateAsync(userId);
                    }
                }
            }
            // Handle navigation (month change)
            else if (calendarService.IsNavigationCallback(callbackData))
            {
                logger.LogInformation("Detected navigation callback: {CallbackData}", callbackData);
                var updatedCalendar = await calendarService.HandleNavigationAsync(callbackData);
                
                await messageService.EditMessageReplyMarkupAsync(
                    chatId,
                    messageId,
                    updatedCalendar,
                    cancellationToken: cancellationToken);
            }

            await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling export calendar callback for user {UserId}", userId);
            await messageService.SendTextMessageAsync(
                chatId,
                localizationService.GetString(ResourceKeys.ErrorOccurred, language),
                cancellationToken: cancellationToken);
        }
    }

    private async Task ExportAttendanceData(long userId, DateOnly startDate, DateOnly endDate, string language, long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var query = new ExportAttendanceQuery(userId, startDate, endDate, language);
            var result = await mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                await messageService.SendTextMessageAsync(
                    chatId,
                    localizationService.GetString("ExportFailed", language),
                    cancellationToken: cancellationToken);
                return;
            }

            // Send Excel file
            var fileName = $"Attendance_Report_{startDate:dd-MM-yyyy}_to_{endDate:dd-MM-yyyy}.xlsx";
            
            using var stream = new MemoryStream(result.Value);
            var inputFile = InputFile.FromStream(stream, fileName);
            
            await messageService.SendDocumentAsync(
                chatId,
                inputFile,
                caption: localizationService.GetString("AttendanceReportGenerated", language),
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
                localizationService.GetString(ResourceKeys.ErrorOccurred, language),
                cancellationToken: cancellationToken);
        }
    }
}