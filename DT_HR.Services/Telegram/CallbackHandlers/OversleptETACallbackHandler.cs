using DT_HR.Application.Attendance.Commands.MarkAbsent;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Enumeration;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.CallbackHandlers;

public class OversleptETACallbackHandler (
    ITelegramMessageService messageService,
    IUserStateService stateService,
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
        
        logger.LogInformation("Processing overslept ETA selection: {ETA} for the user {UserId}",eta,userId);

        await messageService.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

        await stateService.SetStateAsync(userId, new UserState
        {
            CurrentAction = UserAction.ReportingAbsence,
            AbsenceType = AbsenceType.Overslept,
            Data = new Dictionary<string, object> { ["type"] = "overslept" }
        });

        if (eta == "custom")
        {
            await messageService.SendTextMessageAsync(chatId,
                "Please enter your expected arrival time (format: HH:MM):",
                cancellationToken: cancellationToken);
        }
        else
        {
            var minutes = int.Parse(eta);
            var expectedTime = DateTime.Now.AddMinutes(minutes);


            var command = new MarkAbsentCommand(
                userId,
                "Overslept",
                AbsenceType.Overslept,
                expectedTime);
            var result = await mediator.Send(command,cancellationToken);

            await stateService.RemoveStateAsync(userId);

            if (result.IsSuccess)
            {
                await messageService.ShowMainMenuAsync(chatId, "Your absence has been recorded.", cancellationToken);
            }
            else
            {
                await messageService.ShowMainMenuAsync(chatId, $"Failed to record absence : {result.Error.Message}",
                    cancellationToken);
            }
            
        }
    }
}