using DT_HR.Application.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.CommandHandlers;

public class ReportAbsenceCommandHandler(
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService,
    ILogger<ReportAbsenceCommandHandler> logger) : ITelegramService
{
    public Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return Task.FromResult(false);

        var text = message.Text.ToLower();
        return Task.FromResult(
            text == "/absent" ||
            text == "report absence" || text.Contains("report absence"));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        
        
        logger.LogInformation("Processing report absence command for the user {UserId}", userId);

        var keyboard = keyboardService.GetAbsenceTypeKeyboard();

        await messageService.SendTextMessageAsync(chatId,
            """
            📋 **Report Absence**

            Please select the reason for your absence:
            """,
            keyboard, cancellationToken);
    }
}