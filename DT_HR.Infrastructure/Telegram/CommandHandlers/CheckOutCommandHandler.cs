using DT_HR.Application.Attendance.Commands.CheckOut;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class CheckOutCommandHandler(
    ITelegramMessageService messageService,
    ILocalizationService localization,
    IUserStateService stateService, 
    ILogger<CheckOutCommandHandler> logger,
    IMediator mediator) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        var state = await stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? await localization.GetUserLanguage(message.From!.Id);
        var text = message.Text.ToLower();
        var checkOutText = localization.GetString(ResourceKeys.CheckOut,language).ToLower();

        return 
        text == "/checkout" ||
        text == checkOutText ||
        text.Contains("check out");
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var currestState = await stateService.GetStateAsync(userId);
        var language = currestState?.Language ?? await localization.GetUserLanguage(userId);

        logger.LogInformation("Processing check out command for the user {UserId}",userId);

        var state = new UserState
        {
            CurrentAction = UserAction.CheckingOut,
            Language = language
        };

        await stateService.SetStateAsync(userId, state);
        var command = new CheckOutCommand(userId);

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            await messageService.ShowMainMenuAsync(
                chatId, 
                localization.GetString(ResourceKeys.CheckOutCompleted,language),
                language,
                cancellationToken:cancellationToken);
        }
        else
        {
            await messageService.ShowMainMenuAsync(
                chatId, 
                $"{localization.GetString(ResourceKeys.CheckOutFailed,language)} {result.Error.Message}",
                language,
                cancellationToken:cancellationToken);
        }
    }
}