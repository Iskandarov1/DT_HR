using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.CommandHandlers;

public class StartCommandHandler(
    IUserRepository userRepository,
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService,
    IUserStateService stateService,
    ILogger<StartCommandHandler> logger) : ITelegramService
{
    public Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return Task.FromResult(false);
        return Task.FromResult(message.Text.Equals("/start", StringComparison.OrdinalIgnoreCase));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        
        logger.LogInformation("Processing start command for the user {ChatId}",chatId);

        var user = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);

        if (user.HasNoValue)
        {
            await stateService.SetStateAsync(userId, new UserState { CurrentAction = UserAction.Registering });

            var keyboard = keyboardService.GetContactRequestKeyboard();
            
            await messageService.SendTextMessageAsync(
                chatId,
                """
                Welcome to DT HR Attendance System! 👋

                To get started, I need to register you in our system.
                Please share your contact information by clicking the button below.
                """,
                keyboard,
                cancellationToken);
        }
        else
        {
            await messageService.SendTextMessageAsync(
                chatId,
                $"Welcome back,{user.Value.FirstName!}! 👋",
                cancellationToken:cancellationToken);

            await messageService.ShowMainMenuAsync(chatId, "What would you like to do today?", cancellationToken);
        }
    }
}