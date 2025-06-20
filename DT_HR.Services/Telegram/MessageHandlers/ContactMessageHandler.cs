using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Users.Commands;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.MessageHandlers;

public class ContactMessageHandler(
    IUserStateService stateService,
    ITelegramMessageService messageService,
    IMediator mediator, 
    ILogger<ContactMessageHandler> logger)
{
    public async Task HandlerAsync(Message message, CancellationToken cancellationToken)
    {
        if(message.Contact == null || message.From == null) return;

        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        
        logger.LogInformation("Processing Contact informatio for the user {UserId}",userId);

        var state = await stateService.GetStateAsync(userId);

        if (state != null && state.CurrentAction == UserAction.Registering)
        {
            await stateService.RemoveStateAsync(userId);

            var command = new RegisterUserCommand(
                userId,
                message.Contact.PhoneNumber,
                message.Contact.FirstName ?? message.From.FirstName ?? "Unknown",
                message.Contact.LastName ?? message.From.LastName ?? "");

            var result = await mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                await messageService.SendTextMessageAsync(chatId,
                    "✅ Registration successful! Welcome to DT HR System.", 
                    cancellationToken: cancellationToken);
                await messageService.ShowMainMenuAsync(chatId, "What would you like to do?", cancellationToken);
            }
            else
            {
                await messageService.SendTextMessageAsync(chatId,
                    $"❌ Registration failed: {result.Error.Message}",
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            await messageService.SendTextMessageAsync(chatId, 
                "📱 Contact received but no action was pending.",
                cancellationToken: cancellationToken);
        }
    }
}