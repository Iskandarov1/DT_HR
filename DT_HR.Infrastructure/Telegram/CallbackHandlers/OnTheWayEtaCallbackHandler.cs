using DT_HR.Application.Attendance.Commands.MarkAbsent;
using DT_HR.Application.Core.Abstractions.Enum;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Enumeration;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CallbackHandlers;

public class OnTheWayEtaCallbackHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    IMediator mediator,
    ILogger<OnTheWayEtaCallbackHandler> logger) : ITelegramCallbackQuery
{
    public Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(callbackQuery.Data?.StartsWith("absent_ontheway_eta:") ?? false);
    }

    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;
        var etaValue = callbackQuery.Data!.Split(':')[1];
        
        var currentState = await stateService.GetStateAsync(userId);
        var language = currentState?.Language ?? await localization.GetUserLanguage(userId);
        
        logger.LogInformation("Processing OnTheWay ETA selection {EtaValue} for user {UserId}", etaValue, userId);

        await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

        if (currentState == null || currentState.AbsenceType != AbsenceType.OnTheWay)
        {
            await messageService.SendTextMessageAsync(chatId,
                localization.GetString(ResourceKeys.SessionExpired, language),
                cancellationToken: cancellationToken);
            return;
        }

        if (etaValue == "custom")
        {
            var state = new UserState
            {
                CurrentAction = UserAction.ReportingAbsenceEta,
                AbsenceType = AbsenceType.OnTheWay,
                Language = language,
                Data = currentState.Data
            };
            await stateService.SetStateAsync(userId, state);
            
            await messageService.SendTextMessageAsync(chatId,
                localization.GetString(ResourceKeys.OnTheWayCustomEtaPrompt, language),
                cancellationToken: cancellationToken);
        }
        else
        {
            var minutes = int.Parse(etaValue);
            var localNow = TimeUtils.Now;
            var estimatedArrivalTime = localNow.AddMinutes(minutes);
            
            var reason = currentState.Data.TryGetValue("reason", out var reasonObj) ? reasonObj?.ToString() : string.Empty;
            
            var command = new MarkAbsentCommand(
                userId,
                reason,
                AbsenceType.OnTheWay,
                estimatedArrivalTime);

            var result = await mediator.Send(command, cancellationToken);
            await stateService.RemoveStateAsync(userId);

            if (result.IsSuccess)
            {
                await messageService.ShowMainMenuAsync(
                    chatId,
                    language,
                    menuType: MainMenuType.OnTheWay,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await messageService.ShowMainMenuAsync(
                    chatId,
                    language,
                    cancellationToken: cancellationToken);
            }
        }

        await messageService.EditMessageTextAsync(
            chatId,
            messageId,
            localization.GetString(ResourceKeys.EtaSelected, language),
            replyMarkUp: null,
            cancellationToken: cancellationToken);
    }
}