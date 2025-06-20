using DT_HR.Application.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.CommandHandlers;

public class CheckInCommandHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILogger<CheckInCommandHandler> logger) : ITelegramService
{
    public Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return Task.FromResult(false);
        var text = message.Text.ToLower();
        return Task.FromResult(
            text == "/checkin" ||
            text == "✅ check in");
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        
        logger.LogInformation("Processing check-in command  for the user {UserId}",userId);

        await stateService.SetStateAsync(userId, new UserState { CurrentAction = UserAction.CheckingIn });
        
        await messageService.SendLocationRequestAsync(chatId,
            """
            📍 **Check-In Process**

            Please share your current location to check in.
            Make sure you're within the office radius for successful check-in.
            """, 
            cancellationToken);
    }
}