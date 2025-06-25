using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.CommandHandlers;

public class ReportAbsenceCommandHandler(
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService,
    ILocalizationService localization,
    IUserStateService stateService,
    ILogger<ReportAbsenceCommandHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        var state = await stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? "uz";
        var reportAbsence = localization.GetString(ResourceKeys.ReportAbsence, language).ToLower();
        var text = message.Text.ToLower();
        return 
            text == "/absent" ||
            text == reportAbsence || 
            text.Contains("report absence");
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? "uz";
        
        logger.LogInformation("Processing report absence command for the user {UserId}", userId);

        var keyboard = keyboardService.GetAbsenceTypeKeyboard();

        var reportAbsenceTitle = localization.GetString(ResourceKeys.ReportAbsenceTitle, language);
        var reportAbsencePromt = localization.GetString(ResourceKeys.AbsenceReasonPrompt,language);
        
        await messageService.SendTextMessageAsync(chatId,
            $"{reportAbsenceTitle}\n\n{reportAbsencePromt}",
            keyboard, cancellationToken);
    }
}