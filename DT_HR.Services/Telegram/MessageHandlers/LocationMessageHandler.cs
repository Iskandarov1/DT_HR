using DT_HR.Application.Attendance.Commands.CheckIn;
using DT_HR.Application.Core.Abstractions.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.MessageHandlers;

public class LocationMessageHandler(
    IUserStateService stateService,
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
        if (state != null && state.CurrentAction == UserAction.CheckingIn)
        {

            await stateService.RemoveStateAsync(userId);

            var command = new CheckInCommand(
                userId,
                message.Location.Latitude,
                message.Location.Longitude);

            var result = await mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                await messageService.ShowMainMenuAsync(chatId, "Check-in completed! What would you like to do next?",
                    cancellationToken);
            }
            else
            {
                await messageService.ShowMainMenuAsync(chatId, $"Check-in failed: {result.Error.Message}",
                    cancellationToken);
            }
        }
        else
        {
            await messageService.SendTextMessageAsync(chatId,
                "📍 Location received but no action was pending. Use /checkin to check in.",
                cancellationToken: cancellationToken
            );
        }
    }
}