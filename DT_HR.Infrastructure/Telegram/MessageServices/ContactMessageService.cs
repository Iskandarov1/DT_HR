using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Application.Users.Commands;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.MessageServices;

public class ContactMessageService(
    IUserStateService stateService,
    ITelegramMessageService messageService,
    IMediator mediator,
    ILocalizationService localization,
    ILogger<ContactMessageService> logger)
{
    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if(message.Contact == null || message.From == null) return;

        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        
        logger.LogInformation("Processing contact information for the user {UserId}",userId);

        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? await localization.GetUserLanguage(userId);
        if (state != null && state.CurrentAction == UserAction.Registering)
        {
            await stateService.RemoveStateAsync(userId);

            var command = new RegisterUserCommand(
                userId,
                message.Contact.PhoneNumber,
                message.From.FirstName ?? message.From.FirstName ?? "Unknown",
                message.From.LastName ?? message.From.LastName ?? "",
                language);

            var result = await mediator.Send(command,cancellationToken);

            if (result.IsSuccess)
            {
                await messageService.SendTextMessageAsync(chatId,localization.GetString(ResourceKeys.RegistrationSuccessful,language) ,
                    cancellationToken: cancellationToken);
                await messageService.ShowMainMenuAsync(chatId, localization.GetString(ResourceKeys.WhatWouldYouLikeToDo,language),language,false, cancellationToken);
                
            }
            else
            {
                await messageService.SendTextMessageAsync(chatId, localization.GetString(ResourceKeys.RegistrationFailed,language),
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            await messageService.SendTextMessageAsync(chatId, localization.GetString(ResourceKeys.ContactReceived,language),
                cancellationToken: cancellationToken);
        }
    }
}