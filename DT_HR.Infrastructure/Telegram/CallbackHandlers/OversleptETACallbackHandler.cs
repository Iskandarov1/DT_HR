using DT_HR.Application.Attendance.Commands.MarkAbsent;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Enumeration;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CallbackHandlers;

public class OversleptETACallbackHandler (
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    IMediator mediator,
    ILogger<OversleptETACallbackHandler>logger) : ITelegramCallbackQuery
{
    public Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(callbackQuery.Data?.StartsWith("absent_overslept_eta:") ?? false);
    }

    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var eta = callbackQuery.Data!.Split(':')[1];
        var currentState = await stateService.GetStateAsync(userId);
        var language = currentState?.Language ?? await localization.GetUserLanguage(userId);
        
        logger.LogInformation("Processing overslept ETA selection: {ETA} for the user {UserId}",eta,userId);

        await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

        var state = new UserState
        {
            CurrentAction = UserAction.ReportingAbsence,
            AbsenceType = AbsenceType.Overslept,
            Language = language,
            Data = new Dictionary<string, object> { ["type"] = "overslept" }
        };
        await stateService.SetStateAsync(userId, state);

        if (eta == "custom")
        {
            await messageService.SendTextMessageAsync(chatId,
                localization.GetString(ResourceKeys.PleaseEnterPhoneNumber,language).Replace("phone number", "arrival time"),
                cancellationToken: cancellationToken);
        }
        else
        {
            var minutes = int.Parse(eta);
            var expectedTime = TimeUtils.Now.AddMinutes(minutes);
            var reason = language switch
            {
                "ru" => "Проспал",
                "en" => "Overslept",
                _ => "Uxlab Qoldim"
            };

            var command = new MarkAbsentCommand(
                userId,
                reason,
                AbsenceType.Overslept,
                expectedTime);
            var result = await mediator.Send(command,cancellationToken);

            await stateService.RemoveStateAsync(userId);

            if (result.IsSuccess)
            {
                await messageService.ShowMainMenuAsync(
                    chatId,language,
                    cancellationToken:cancellationToken);
            }
            else
            {
                await messageService.ShowMainMenuAsync(
                    chatId, language, 
                    cancellationToken:cancellationToken);
            }
            
        }
    }
}