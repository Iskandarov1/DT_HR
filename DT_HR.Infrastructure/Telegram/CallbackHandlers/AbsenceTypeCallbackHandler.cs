using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Enumeration;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CallbackHandlers;

public class AbsenceTypeCallbackHandler(
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService,
    IUserStateService stateService,
    ILocalizationService localization,
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
        var messageId = callbackQuery.Message.MessageId;
        var type = callbackQuery.Data!.Split(':')[1];
        var currentState = await stateService.GetStateAsync(userId);
        var language = currentState?.Language ?? await localization.GetUserLanguage(userId);
        
        logger.LogInformation("Processing absence type selection {Type} for user {UserId}",type,userId);

        await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

        var state = new UserState{ 
            CurrentAction = UserAction.ReportingAbsence,
            Language = language };

        switch (type)
        {
            case "ontheway":
                state.AbsenceType = AbsenceType.OnTheWay;
                state.CurrentAction = UserAction.ReportingAbsenceReason;
                state.Data = new Dictionary<string, object> { ["type"] = "ontheway" };
                await stateService.SetStateAsync(userId, state);
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString(ResourceKeys.OnTheWayReasonPrompt,language), 
                    cancellationToken: cancellationToken);
                break;
            
            case "overslept":
                state.AbsenceType = AbsenceType.Overslept;
                state.Data = new Dictionary<string, object> { ["type"] = "overslept" };
                var keyboard = keyboardService.GetOversleptEtaKeyboard(language);
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString(ResourceKeys.OversleptPrompt,language), 
                    keyboard,
                    cancellationToken);
                break;
            case "other":
                state.AbsenceType = AbsenceType.Custom;
                state.CurrentAction = UserAction.ReportingAbsenceReason;
                state.Data = new Dictionary<string, object> { ["type"] = "other" };
                await stateService.SetStateAsync(userId, state);
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString(ResourceKeys.OtherReasonPrompt,language), 
                    cancellationToken: cancellationToken);
                break;
        }

        await messageService.EditMessageTextAsync(
            chatId, 
            messageId, 
            localization.GetString(ResourceKeys.AbsenceOptionsSelected,language), 
            replyMarkUp: null,
            cancellationToken: cancellationToken);
    }
}