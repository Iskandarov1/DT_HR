using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CallbackHandlers;

public class CalendarCallbackHandler(
    ITelegramMessageService messageService,
    ITelegramCalendarService calendarService,
    IUserStateService stateService,
    ILocalizationService localizationService,
    ITelegramKeyboardService keyboardService,
    IUserRepository userRepository,
    ILogger<CalendarCallbackHandler> logger) : ITelegramCallbackQuery
{
    public async Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        if (callbackQuery.Data == null) return false;
        
        var state = await stateService.GetStateAsync(callbackQuery.From.Id);
        if (state?.CurrentAction != UserAction.CreatingEvent) return false;

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

        logger.LogInformation("Processing calendar callback for user {UserId}: {CallbackData}", userId, callbackData);

        // Handle date selection
        if (calendarService.IsDateCallback(callbackData))
        {
            logger.LogInformation("Detected date callback: {CallbackData}", callbackData);
            var selectedDate = calendarService.ParseDateFromCallback(callbackData);
            if (selectedDate.HasValue)
            {
                state.Data["selectedDate"] = selectedDate.Value.ToString("yyyy-MM-dd");
                state.Data["step"] = "time";
                await stateService.SetStateAsync(userId, state);

                await messageService.EditMessageTextAsync(
                    chatId,
                    messageId,
                    $"Selected date: {selectedDate.Value:dd-MM-yyyy}\n\n" +
                    localizationService.GetString(ResourceKeys.EnterEventTime, language),
                    keyboardService.GetCancelInlineKeyboard(language),
                    cancellationToken: cancellationToken);
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
        else
        {
            logger.LogWarning("Unknown calendar callback: {CallbackData}", callbackData);
        }
    }
}