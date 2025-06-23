using DT_HR.Application.Attendance.Commands.CheckOut;
using DT_HR.Application.Core.Abstractions.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.CommandHandlers;

public class CheckOutCommandHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService, 
    ILogger<CheckOutCommandHandler> logger,
    IMediator mediator) : ITelegramService
{
    public Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return Task.FromResult(false);
        var text = message.Text.ToLower();

        return Task.FromResult(
        text == "/checkout" ||
        text == "⏰ check out" ||
        text.Contains("check out"));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        
        logger.LogInformation("Processing check out command for the user {UserId}",userId);

        await stateService.SetStateAsync(userId, new UserState { CurrentAction = UserAction.CheckingOut });
        var command = new CheckOutCommand(userId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            await messageService.ShowMainMenuAsync(chatId, 
                "Check-out completed! Have a great day!", cancellationToken);
        }
        else
        {
            await messageService.ShowMainMenuAsync(chatId, 
                $"Check-out failed: {result.Error.Message}", cancellationToken);
        }
    }
}