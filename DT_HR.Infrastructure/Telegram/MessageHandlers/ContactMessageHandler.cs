using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Application.Users.Commands;
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
            await stateService.RemoveStateAsync(userId);

            var command = new RegisterUserCommand(
                userId,
                message.Contact.PhoneNumber,
                message.Contact.FirstName ?? message.From.FirstName ?? "Unknown",
                message.Contact.LastName ?? message.From.LastName ?? "",
                language);

            var result = await mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                await messageService.SendTextMessageAsync(
                    chatId,
                    localization.GetString(ResourceKeys.RegistrationSuccessful,language), 
                    cancellationToken: cancellationToken);
                await messageService.ShowMainMenuAsync(
                    chatId, 
                    localization.GetString(ResourceKeys.WhatWouldYouLikeToDo, language),
                    language,
                    cancellationToken:cancellationToken);
            }
            else
            {
                await messageService.SendTextMessageAsync(chatId,
                    $"{localization.GetString(ResourceKeys.RegistrationFailed,language)}: {result.Error.Message}",
                    cancellationToken: cancellationToken);
            }
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