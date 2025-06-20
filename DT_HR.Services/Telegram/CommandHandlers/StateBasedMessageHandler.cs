using System.Text.RegularExpressions;
using DT_HR.Application.Attendance.Commands.MarkAbsent;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Enumeration;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.CommandHandlers;

public class StateBasedMessageHandler(
    IUserStateService  stateService,
    ITelegramMessageService messageService,
    IMediator mediator,
    ILogger<StateBasedMessageHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null) return false;

        var state = await stateService.GetStateAsync(message.From.Id);
        return state != null && state.CurrentAction == UserAction.ReportingAbsence;

    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;

        var state = await stateService.GetStateAsync(userId);

        if (state == null) return;
        
        logger.LogInformation("Processing state based message for the user {UserId}, action {Action}",userId, state.CurrentAction);

        switch (state.CurrentAction)
        {
            case UserAction.ReportingAbsence:
                await ProcessAbsenceReasonAsync(message, state, cancellationToken);
                break;
            default:
                logger.LogWarning("Unknow user state action: {Action}",state.CurrentAction);
                await stateService.RemoveStateAsync(userId);
                await messageService.ShowMainMenuAsync(chatId, "Something went wrong try again", cancellationToken);
                break;
        }

    }

    public async Task ProcessAbsenceReasonAsync(Message message, UserState state, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim() ?? "";

        DateTime? estimatedArrivalTime = null;
        string reason = text;

        if (state.AbsenceType == AbsenceType.OnTheWay || 
            (state.AbsenceType == AbsenceType.Custom && !text.ToLower().Contains("absent")))
        {
            var timeMatch = Regex.Match(text, @"\b(\d{1,2}):(\d{2})\b");
            if (timeMatch.Success)
            {
                var hour = int.Parse(timeMatch.Groups[1].Value);
                var minute = int.Parse(timeMatch.Groups[2].Value);
                var today = DateTime.Today;
                estimatedArrivalTime = today.AddHours(hour).AddMinutes(minute);

                if (estimatedArrivalTime < DateTime.Now)
                {
                    estimatedArrivalTime = estimatedArrivalTime.Value.AddDays(1);
                }

                reason = text.Replace(timeMatch.Value, "").Trim().TrimEnd(',').Trim();
                
            }
            else if (state.AbsenceType == AbsenceType.OnTheWay)
            {
                await messageService.SendTextMessageAsync(chatId,
                    "❌ Please provide a valid time format (HH:MM). Example: 'Traffic, 10:30'",
                    cancellationToken: cancellationToken);
                return;
            }
        }
        else if (state.AbsenceType == AbsenceType.Overslept)
        {
            var timeMatch = Regex.Match(text, @"\b(\d{1,2}):(\d{2})\b");
            if (timeMatch.Success)
            {
                var hour = int.Parse(timeMatch.Groups[1].Value);
                var minute = int.Parse(timeMatch.Groups[2].Value);
                var today = DateTime.Today;

                estimatedArrivalTime = today.AddHours(hour).AddMinutes(minute);

                if (estimatedArrivalTime <DateTime.Now)
                {
                    estimatedArrivalTime = estimatedArrivalTime.Value.AddDays(1);
                }

                reason = "Overslept";
            }
            else
            {
                await messageService.SendTextMessageAsync(chatId, "❌ Please provide a valid time format (HH:MM).",
                    cancellationToken: cancellationToken);
                return;
            }
        }

        var command = new MarkAbsentCommand(
            userId,
            reason,
            state.AbsenceType,
            estimatedArrivalTime);

        var result = await mediator.Send(command, cancellationToken);
        await stateService.RemoveStateAsync(userId);

        if (result.IsSuccess)
        {
            await messageService.ShowMainMenuAsync(chatId, "Your absence has been recorded", cancellationToken);
        }
        else
        {
            await messageService.ShowMainMenuAsync(chatId, $"Failed to record absence: {result.Error.Message}",
                cancellationToken);
        }
    }
}