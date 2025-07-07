using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Application.Users.Commands.UpdateWorkHours;
using DT_HR.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.MessageHandlers;

public class WorkTimeInputMessageHandler(
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService,
    IUserStateService stateService,
    IUserRepository userRepository,
    ILocalizationService localization,
    IMediator mediator,
    ILogger<WorkTimeInputMessageHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        
        var state = await stateService.GetStateAsync(message.From!.Id);
        return state?.CurrentAction == UserAction.SettingWorkStartTime || 
               state?.CurrentAction == UserAction.SettingWorkEndTime;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var text = message.Text!.Trim();
        
        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? await localization.GetUserLanguage(userId);

        logger.LogInformation("Processing work time input for user {UserId}: {TimeInput}", userId, text);

        try
        {
            // Validate user is manager
            var maybeUser = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);
            if (maybeUser.HasNoValue || !maybeUser.Value.IsManager())
            {
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString(ResourceKeys.OptionNotAvailable, language),
                    cancellationToken: cancellationToken);
                await stateService.RemoveStateAsync(userId);
                return;
            }

            var user = maybeUser.Value;

            // Validate time format (HH:mm)
            if (!TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture,DateTimeStyles.None, out var newTime))
            {
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString(ResourceKeys.InvalidTimeFormat, language),
                    cancellationToken: cancellationToken);
                return;
            }

            // Process based on current action
            if (state!.CurrentAction == UserAction.SettingWorkStartTime)
            {
                await HandleWorkStartTimeInput(user, newTime, chatId, language, cancellationToken);
            }
            else if (state.CurrentAction == UserAction.SettingWorkEndTime)
            {
                await HandleWorkEndTimeInput(user, newTime, chatId, language, cancellationToken);
            }

            // Clear state after successful processing
            await stateService.RemoveStateAsync(userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling work time input for user {UserId}", userId);
            await messageService.SendTextMessageAsync(
                chatId,
                localization.GetString(ResourceKeys.ErrorOccurred, language),
                cancellationToken: cancellationToken);
            
            // Clear state on error
            await stateService.RemoveStateAsync(userId);
        }
    }

    private async Task HandleWorkStartTimeInput(Domain.Entities.User user, TimeOnly newStartTime, long chatId, string language, CancellationToken cancellationToken)
    {
        // Create command to update work hours
        var command = new UpdateWorkHoursCommand(user.Id, newStartTime, user.WorkEndTime);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            // Check if it's the specific validation error for invalid time range
            if (result.Error.Code == "invalid_work_time_range")
            {
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString(ResourceKeys.InvalidWorkTimeRange, language),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString(ResourceKeys.ErrorOccurred, language),
                    cancellationToken: cancellationToken);
            }
            return;
        }

        // Success - show confirmation and return to work time settings
        var confirmationMessage = string.Format(
            localization.GetString(ResourceKeys.WorkStartTimeSet, language), 
            newStartTime.ToString("HH:mm"));
        
        await messageService.SendTextMessageAsync(
            chatId,
            confirmationMessage,
            cancellationToken: cancellationToken);

        // Show updated work time settings
       // await ShowWorkTimeSettings(chatId, user.Id, language, cancellationToken);
    }

    private async Task HandleWorkEndTimeInput(Domain.Entities.User user, TimeOnly newEndTime, long chatId, string language, CancellationToken cancellationToken)
    {
        // Create command to update work hours
        var command = new UpdateWorkHoursCommand(user.Id, user.WorkStartTime, newEndTime);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            // Check if it's the specific validation error for invalid time range
            if (result.Error.Code == "invalid_work_time_range")
            {
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString(ResourceKeys.InvalidWorkTimeRange, language),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString(ResourceKeys.ErrorOccurred, language),
                    cancellationToken: cancellationToken);
            }
            return;
        }

        // Success - show confirmation and return to work time settings
        var confirmationMessage = string.Format(
            localization.GetString(ResourceKeys.WorkEndTimeSet, language), 
            newEndTime.ToString("HH:mm"));
        
        await messageService.SendTextMessageAsync(
            chatId,
            confirmationMessage,
            cancellationToken: cancellationToken);

        // Show updated work time settings
       // await ShowWorkTimeSettings(chatId, user.Id, language, cancellationToken);
    }

    private async Task ShowWorkTimeSettings(long chatId, Guid userId, string language, CancellationToken cancellationToken)
    {
        // Get updated user data
        var maybeUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (maybeUser.HasNoValue) return;

        var user = maybeUser.Value;
        var keyboard = keyboardService.GetWorkTimeSettingsKeyboard(language);
        var currentHoursText = localization.GetString(ResourceKeys.CurrentWorkHours, language);
        var message = string.Format(currentHoursText, 
            user.WorkStartTime.ToString("HH:mm"), 
            user.WorkEndTime.ToString("HH:mm"));
        
        await messageService.SendTextMessageAsync(
            chatId,
            message,
            keyboard,
            cancellationToken: cancellationToken);
    }
}