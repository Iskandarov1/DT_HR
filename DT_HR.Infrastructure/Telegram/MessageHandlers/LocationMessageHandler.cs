using DT_HR.Application.Attendance.Commands.CheckIn;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.MessageHandlers;

public class LocationMessageHandler(
    IUserStateService stateService,
    ILocalizationService localization,
    ITelegramMessageService messageService,
    IMediator mediator,
    ILogger<LocationMessageHandler> logger)
{
    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if(message.Location == null || message.From == null) return;

        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        
        logger.LogInformation("Processing location message from the user {UserId}",userId);

        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? await localization.GetUserLanguage(userId);
        
        if (state != null && state.CurrentAction == UserAction.CheckingIn)
        {

            await stateService.RemoveStateAsync(userId);

            var command = new CheckInCommand(
                userId,
                message.Location.Latitude,
                message.Location.Longitude);

            var result = await mediator.Send(command, cancellationToken);
            var checkInPrompt = localization.GetString(ResourceKeys.CheckInCompleted, language);
            var checkInFailPrompt = localization.GetString(ResourceKeys.CheckInFailed, language, result.Error.Message);
            if (result.IsSuccess)
            {
                await messageService.ShowMainMenuAsync(
                    chatId, 
                    checkInPrompt,
                    language,
                    cancellationToken);
            }
            else
            {
                await messageService.ShowMainMenuAsync(
                    chatId, 
                    checkInFailPrompt,
                    language,
                    cancellationToken);
            }
        }
        else
        {
            await messageService.SendTextMessageAsync(
                chatId,
                localization.GetString(ResourceKeys.LocationReceived,language),
                cancellationToken: cancellationToken
            );
        }
    }
}