using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Application.Company.Commands.UpdateCompanyWorkHours;
using DT_HR.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using DT_HR.Domain.Entities;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.MessageHandlers;

public class WorkTimeInputMessageHandler(
    ITelegramMessageService messageService,
    ICompanyRepository companyRepository,
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
            
            var companyMaybe = await companyRepository.GetAsync(cancellationToken);
            if (companyMaybe.HasNoValue) return; // or handle error

            var company = companyMaybe.Value;


            if (!TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture,DateTimeStyles.None, out var newTime))
            {
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString(ResourceKeys.InvalidTimeFormat, language),
                    cancellationToken: cancellationToken);
                return;
            }


            if (state!.CurrentAction == UserAction.SettingWorkStartTime)
            {
                await HandleWorkStartTimeInput(company, newTime, chatId, language, cancellationToken);
            }
            else if (state.CurrentAction == UserAction.SettingWorkEndTime)
            {
                await HandleWorkEndTimeInput(company, newTime, chatId, language, cancellationToken);
            }


            await stateService.RemoveStateAsync(userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling work time input for user {UserId}", userId);
            await messageService.SendTextMessageAsync(
                chatId,
                localization.GetString(ResourceKeys.ErrorOccurred, language),
                cancellationToken: cancellationToken);
            
            await stateService.RemoveStateAsync(userId);
        }
    }

    private async Task HandleWorkStartTimeInput(Company company, TimeOnly newStartTime, long chatId, string language, CancellationToken cancellationToken)
    {

        var command = new UpdateCompanyWorkHoursCommand(newStartTime, company.DefaultWorkEndTime);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {

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


        var confirmationMessage = string.Format(
            localization.GetString(ResourceKeys.WorkStartTimeSet, language), 
            newStartTime.ToString("HH:mm"));
        
        await messageService.SendTextMessageAsync(
            chatId,
            confirmationMessage,
            cancellationToken: cancellationToken);
    }

    private async Task HandleWorkEndTimeInput(Company company, TimeOnly newEndTime, long chatId, string language, CancellationToken cancellationToken)
    {

        var command = new UpdateCompanyWorkHoursCommand(company.DefaultWorkStartTime, newEndTime);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {

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

        // Success - show confirmation
        var confirmationMessage = string.Format(
            localization.GetString(ResourceKeys.WorkEndTimeSet, language), 
            newEndTime.ToString("HH:mm"));
        
        await messageService.SendTextMessageAsync(
            chatId,
            confirmationMessage,
            cancellationToken: cancellationToken);
    }
    
}