using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Application.Users.Commands;
using DT_HR.Application.Users.Commands.RegisterUser;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.MessageHandlers;

public class ContactMessageHandler(
    IUserStateService stateService,
    ILocalizationService localization,
    ITelegramMessageService messageService,
    IMediator mediator, 
    ILogger<ContactMessageHandler> logger)
{
    public async Task HandlerAsync(Message message, CancellationToken cancellationToken)
    {
        if(message.Contact == null || message.From == null) return;

        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        
        logger.LogInformation("Processing Contact information for the user {UserId}",userId);

        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? "uz";

        if (state != null && state.CurrentAction == UserAction.Registering)
        {
            state.Data["phone"] = message.Contact.PhoneNumber;
            state.Data["firstName"] = message.Contact.FirstName ?? message.From.FirstName ?? "Unknown";
            state.Data["lastName"] = message.Contact.LastName ?? message.From.FirstName ?? "";
            state.Data["step"] = "birthday";
            await stateService.SetStateAsync(userId, state);

            await messageService.SendTextMessageAsync(chatId,
                localization.GetString(ResourceKeys.EnterBirthDate, language),
                cancellationToken: cancellationToken);


        }
        else
        {
            await messageService.SendTextMessageAsync(
                chatId, 
                localization.GetString(ResourceKeys.ContactReceived,language),
                cancellationToken: cancellationToken);
        }
    }
}