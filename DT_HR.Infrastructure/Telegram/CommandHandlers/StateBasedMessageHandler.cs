using System.Text.RegularExpressions;
using DT_HR.Application.Attendance.Commands.MarkAbsent;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Events.Commands;
using DT_HR.Application.Resources;
using DT_HR.Application.Users.Commands;
using DT_HR.Domain.Core;
using DT_HR.Domain.Enumeration;
using DT_HR.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class StateBasedMessageHandler(
    IUserStateService  stateService,
    ITelegramMessageService messageService,
    IMediator mediator,
    ILogger<StateBasedMessageHandler> logger,
    ILocalizationService localizationService,
    IUserRepository userRepository) : ITelegramService
{
    private static readonly TimeSpan LocalOffset = TimeUtils.LocalOffset;
    private static readonly Regex PhoneNumberRegex = new Regex(@"^(\+?998)?([3781]{2}|(9[013-57-9]))\d{7}$", RegexOptions.Compiled);
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null) return false;

        var state = await stateService.GetStateAsync(message.From.Id);
        return state != null && (state.CurrentAction == UserAction.ReportingAbsence ||
                                 state.CurrentAction == UserAction.Registering ||
                                 state.CurrentAction == UserAction.CreatingEvent);

    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;

        var state = await stateService.GetStateAsync(userId);

        var language = state?.Language ?? await localizationService.GetUserLanguage(userId);

        if (state == null) return;
        
        logger.LogInformation("Processing state based message for the user {UserId}, action {Action}",userId, state.CurrentAction);

        switch (state.CurrentAction)
        {
            case UserAction.ReportingAbsence:
                await ProcessAbsenceReasonAsync(message, state, language,cancellationToken);
                break;
            case UserAction.Registering:
                await ProcessPhoneNumberAsync(message, language, cancellationToken);
                break;
            case UserAction.CreatingEvent:
                await ProcessEventAsync(message, state, language, cancellationToken);
                break;
            default:
                logger.LogWarning("Unknow user state action: {Action}",state.CurrentAction);
                await stateService.RemoveStateAsync(userId);
                await messageService.ShowMainMenuAsync(
                    chatId,
                    localizationService.GetString(ResourceKeys.ErrorOccurred,language), 
                    language,
                    cancellationToken:cancellationToken);
                break;
        }

    }

    private async Task ProcessPhoneNumberAsync(Message message, string language, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim() ?? "";

        if (text == localizationService.GetString(ResourceKeys.Cancel, language))
        {
            await stateService.RemoveStateAsync(userId);
            await messageService.SendTextMessageAsync(
                userId,
                localizationService.GetString(ResourceKeys.Cancel, language),
                cancellationToken: cancellationToken);
            return;
        }

        var phoneNumber = NormalizePhoneNumber(text);

        if (!IsValidPhoneNumber(phoneNumber))
        {
            await messageService.SendTextMessageAsync(chatId,
                localizationService.GetString(ResourceKeys.InvalidPhoneFormat, language),
                cancellationToken: cancellationToken);
        }

        var command = new RegisterUserCommand(
            userId,
            phoneNumber,
            message.From.FirstName ?? "Unknown",
            message.From.LastName ?? "",
            language
        );

        var result = await mediator.Send(command, cancellationToken);

        await stateService.RemoveStateAsync(userId);

        if (result.IsSuccess)
        {
            await messageService.SendTextMessageAsync(chatId,
                localizationService.GetString(ResourceKeys.RegistrationSuccessful, language),
                cancellationToken: cancellationToken);
            await messageService.ShowMainMenuAsync(
                chatId,
                localizationService.GetString(ResourceKeys.WhatWouldYouLikeToDo,language), 
                language,
                cancellationToken:cancellationToken);
        }
        else
        {
            await messageService.SendTextMessageAsync(
                chatId,
                localizationService.GetString(ResourceKeys.RegistrationFailed, language, result.Error.Message),
                cancellationToken: cancellationToken);
        }
    }


    private async Task ProcessEventAsync(Message message, UserState state, string language,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim() ?? string.Empty;
        var step = state.Data.TryGetValue("step", out var s) ? s?.ToString() : "description";

        if (step == "description")
        {
            state.Data["description"] = text;
            state.Data["step"] = "time";
            await stateService.SetStateAsync(userId, state);
            await messageService.SendTextMessageAsync(chatId,
                localizationService.GetString(ResourceKeys.EnterEventTime, language),
                cancellationToken: cancellationToken);
            return;
        }

        if (!DateTime.TryParse(text, out var localTime))
        {
            await messageService.SendTextMessageAsync(chatId,
                localizationService.GetString(ResourceKeys.InvalidTimeFormat, language),
                cancellationToken: cancellationToken);
            return;
        }

        var eventTime = DateTime.SpecifyKind(localTime - LocalOffset, DateTimeKind.Utc);
        var description = state.Data["description"]?.ToString() ?? string.Empty;
        var result = await mediator.Send(new CreateEventCommand(description, eventTime), cancellationToken);
        await stateService.RemoveStateAsync(userId);

        if (result.IsSuccess)
        {
            var users = await userRepository.GetActiveUsersAsync(cancellationToken);
            foreach (var u in users)
            {
                var lang = await localizationService.GetUserLanguage(u.TelegramUserId);
                var msg = $"{description}\n{localTime:yyyy-MM-dd HH:mm}";
                await messageService.SendTextMessageAsync(u.TelegramUserId, msg, cancellationToken: cancellationToken);
            }

            await messageService.ShowMainMenuAsync(chatId,
                localizationService.GetString(ResourceKeys.EventCreated, language),
                language,
                cancellationToken: cancellationToken);
        }
        else
        {
            await messageService.ShowMainMenuAsync(chatId,
                localizationService.GetString(ResourceKeys.ErrorOccurred, language),
                language,
                cancellationToken: cancellationToken);
        }


    }

    private string NormalizePhoneNumber(string input)
    {
        var numbers = Regex.Replace(input, @"\D", "");
        if (numbers.StartsWith("998"))
        {
            return $"+{numbers}";
        }

        // If it's 9 digits and starts with valid prefixes, add +998
        if (numbers.Length == 9 && Regex.IsMatch(numbers, @"^([3781]{2}|(9[013-57-9]))\d{7}$"))
        {
            return $"+998{numbers}";
        }

        // If already has +, return as is
        if (input.StartsWith("+"))
        {
            return $"+{numbers}";
        }

        return numbers;
    }

    private bool IsValidPhoneNumber(string phoneNumber)
    {
        return PhoneNumberRegex.IsMatch(phoneNumber);
    }

    public async Task ProcessAbsenceReasonAsync(Message message, UserState state, string language,CancellationToken cancellationToken)
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

                var localNow = TimeUtils.Now;
                var todayLocal = localNow.Date;
                var localEta = new DateTime(todayLocal.Year, todayLocal.Month, todayLocal.Day, hour, minute, 0, DateTimeKind.Utc);


                if (localEta < localNow)
                {
                    localEta = localEta.AddDays(1);
                }

                estimatedArrivalTime = DateTime.SpecifyKind(localEta, DateTimeKind.Utc);

                reason = text.Replace(timeMatch.Value, "").Trim().TrimEnd(',').Trim();
                // await messageService.ShowMainMenuAsync(
                //     chatId, 
                //     "test",
                //     language,
                //     cancellationToken);

            }
            else if (state.AbsenceType == AbsenceType.OnTheWay)
            {
                await messageService.SendTextMessageAsync(chatId,
                    localizationService.GetString(ResourceKeys.TimeFormatExample,language),
                    cancellationToken: cancellationToken);
                // await messageService.ShowMainMenuAsync(
                //     chatId, 
                //     "test",
                //     language,
                //     cancellationToken);
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

                var localNow = TimeUtils.Now;
                var todayLocal = localNow.Date;
                var localEta = new DateTime(todayLocal.Year, todayLocal.Month, todayLocal.Day, hour, minute, 0, DateTimeKind.Utc);

                if (localEta < localNow)
                {
                    localEta = localEta.AddDays(1);
                }
                estimatedArrivalTime = DateTime.SpecifyKind(localEta, DateTimeKind.Utc);
                reason = language switch
                {
                    "ru" => "Проспал",
                    "en" => "Overslept",
                    _ => "Uxlab Qoldim"
                };
            }
            else
            {
                await messageService.SendTextMessageAsync(
                    chatId, localizationService.GetString(ResourceKeys.InvalidTimeFormat,language),
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
            await messageService.ShowMainMenuAsync(
                chatId, 
                localizationService.GetString(ResourceKeys.AbsenceRecorded ,language),language,
                cancellationToken:cancellationToken);
        }
        else
        {
            await messageService.ShowMainMenuAsync(
                chatId, $"{localizationService.GetString(ResourceKeys.ErrorOccurred,language)}:{result.Error.Message}",
                language,
                cancellationToken:cancellationToken);
        }
    }
}