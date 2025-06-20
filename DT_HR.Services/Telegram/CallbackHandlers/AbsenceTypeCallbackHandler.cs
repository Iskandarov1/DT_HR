using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Enumeration;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.CallbackHandlers;

public class AbsenceTypeCallbackHandler(
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService,
    IUserStateService stateService,
    ILogger<AbsenceTypeCallbackHandler> logger) : ITelegramCallbackQuery
{
    public Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(callbackQuery.Data?.StartsWith("absent_type:") ?? false);
    }

    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var type = callbackQuery.Data!.Split(':')[1];
        
        logger.LogInformation("Processing absence type selection {Type} for user {UserId}",type,userId);

        await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

        switch (type)
        {
            case "sick":
                await stateService.SetStateAsync(userId, new UserState
                {
                    CurrentAction = UserAction.ReportingAbsence,
                    AbsenceType = AbsenceType.Absent,
                    Data = new Dictionary<string, object> { ["type"] = "sick" }
                });
                await messageService.SendTextMessageAsync(chatId,
                    "Please describe your condition or reason for absence:", cancellationToken: cancellationToken);
                break;
            case "ontheway":
                await stateService.SetStateAsync(userId, new UserState
                {
                    CurrentAction = UserAction.ReportingAbsence,
                    AbsenceType = AbsenceType.OnTheWay,
                    Data = new Dictionary<string, object> { ["type"] = "ontheway" }
                });
                await messageService.SendTextMessageAsync(chatId,
                    """
                    🚗 You're on the way!

                    Please provide:
                    1. Reason for being late
                    2. Expected arrival time (format: HH:MM)

                    Example: "Traffic jam, 10:30"
                    """, cancellationToken: cancellationToken);
                break;
            case "overslept":
                var keyboard = keyboardService.GetOversleptEtaKeyboard();
                await messageService.SendTextMessageAsync(chatId,
                    """
                    😴 **Overslept**

                    When do you expect to arrive?
                    """, 
                    keyboard, cancellationToken);
                break;
            case "other":
                await stateService.SetStateAsync(userId, new UserState
                {
                    CurrentAction = UserAction.ReportingAbsence,
                    AbsenceType = AbsenceType.Custom,
                    Data = new Dictionary<string, object> { ["type"] = "other" }
                });
                await messageService.SendTextMessageAsync(chatId,
                    """
                    Please provide:
                    1. Your reason for absence
                    2. Expected arrival time if you're coming (format: HH:MM) or type "absent" if not coming

                    Example: "Doctor appointment, 14:00" or "Family emergency, absent"
                    """, 
                    cancellationToken: cancellationToken);
                break;
        }
    }
}