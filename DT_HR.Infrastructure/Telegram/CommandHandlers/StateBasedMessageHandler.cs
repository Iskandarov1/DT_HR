using System.Text.RegularExpressions;
using DT_HR.Application.Attendance.Commands.MarkAbsent;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Events.Commands;
using DT_HR.Application.Resources;
using DT_HR.Application.Users.Commands;
using DT_HR.Application.Users.Commands.RegisterUser;
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
    ITelegramKeyboardService keyboardService,
    IMediator mediator,
    ILogger<StateBasedMessageHandler> logger,
    ILocalizationService localizationService,
    IUserRepository userRepository) : ITelegramService
{

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
                await ProcessRegistrationAsync(message, state!, language, cancellationToken);
                break;
            case UserAction.CreatingEvent:
                await ProcessEventAsync(message, state, language, cancellationToken);
                break;
            default:
                logger.LogWarning("Unknow user state action: {Action}",state.CurrentAction);
                await stateService.RemoveStateAsync(userId);
                await messageService.ShowMainMenuAsync(
                    chatId,
                    language,
                    cancellationToken:cancellationToken);
                break;
        }

    }

    private async Task ProcessRegistrationAsync(Message message, UserState state,string language, CancellationToken cancellationToken)
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

        var step = state.Data.TryGetValue("step", out var s) ? s?.ToString() : "phone";
        if (step == "birthday")
        {
            if (!DateTime.TryParse(text,out var birthDate))
            {
                await messageService.SendTextMessageAsync(chatId,
                    localizationService.GetString(ResourceKeys.InvalidDateFormat, language),
                    cancellationToken: cancellationToken);
                return;
            }

            var phone = state.Data["phone"]?.ToString() ?? String.Empty;
            var firstName = state.Data["firstName"]?.ToString() ?? message.From.FirstName ?? "Unknown";
            var lastName = state.Data["lastName"]?.ToString() ?? message.From.LastName ?? string.Empty;

            var command = new RegisterUserCommand(
                userId,
                phone,
                firstName,
                lastName,
                DateOnly.FromDateTime(birthDate),
                language);

            var result = await mediator.Send(command, cancellationToken);

            await stateService.RemoveStateAsync(userId);
            
            if (result.IsSuccess)
            {
                await messageService.SendTextMessageAsync(chatId,
                    localizationService.GetString(ResourceKeys.RegistrationSuccessful, language),
                    cancellationToken: cancellationToken);
                await messageService.ShowMainMenuAsync(
                    chatId,
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
        else
        {
            var phoneNumber = NormalizePhoneNumber(text);

            if (!IsValidPhoneNumber(phoneNumber))
            {
                await messageService.SendTextMessageAsync(chatId,
                    localizationService.GetString(ResourceKeys.InvalidPhoneFormat, language),
                    cancellationToken: cancellationToken);
            }
            
            state.Data["phone"] = phoneNumber;
            state.Data["firstName"] = message.From.FirstName ?? "Unknown";
            state.Data["lastName"] = message.From.LastName ?? "";
            state.Data["step"] = "birthday";
            await stateService.SetStateAsync(userId, state);

            await messageService.SendTextMessageAsync(chatId,
                localizationService.GetString(ResourceKeys.EnterBirthDate, language),
                cancellationToken: cancellationToken);
            
        }
        
       
    }


    private async Task ProcessEventAsync(Message message, UserState state, string language,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var maybeUser = await userRepository.GetByTelegramUserIdAsync(message.From!.Id,cancellationToken);
        if (maybeUser.HasNoValue || !maybeUser.Value.IsManager())
        {
            await stateService.RemoveStateAsync(userId);
            await messageService.ShowMainMenuAsync(chatId
                , language,
                cancellationToken: cancellationToken);
            return;
        }
        
        var text = message.Text?.Trim() ?? string.Empty;
        
        if (text.Equals(localizationService.GetString(ResourceKeys.Cancel, language), StringComparison.OrdinalIgnoreCase))
        {
            await stateService.RemoveStateAsync(userId);
            await messageService.ShowMainMenuAsync(chatId,
                language,
                cancellationToken: cancellationToken);
            return;
        }
        
        var step = state.Data.TryGetValue("step", out var s) ? s?.ToString() : "description";

        if (step == "description")
        {
            state.Data["description"] = text;
            state.Data["step"] = "time";
            await stateService.SetStateAsync(userId, state);
            await messageService.SendTextMessageAsync(chatId,
                localizationService.GetString(ResourceKeys.EnterEventTime, language),
                keyboardService.GetCancelKeyboard(language),
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
        

        var utcEventTime = localTime.AddHours(-5);
        
        var description = state.Data["description"]?.ToString() ?? string.Empty;
        var result = await mediator.Send(new CreateEventCommand(description, utcEventTime), cancellationToken);
        await stateService.RemoveStateAsync(userId);

        if (result.IsSuccess)
        {
            var users = await userRepository.GetActiveUsersAsync(cancellationToken);
            foreach (var u in users)
            {
                var lang = await localizationService.GetUserLanguage(u.TelegramUserId);
                // Display in local time (UTC+5)
                var displayTime = utcEventTime.AddHours(5);
                var msg = $"🔔 *New Event Created*\n\n" +
                         $"📅 Event: {description}\n" +
                         $"⏰ Time: {displayTime:yyyy-MM-dd HH:mm}";
                await messageService.SendTextMessageAsync(u.TelegramUserId, msg, cancellationToken: cancellationToken);
            }

            await messageService.ShowMainMenuAsync(chatId,
                language,
                cancellationToken: cancellationToken);
        }
        else
        {
            await messageService.ShowMainMenuAsync(chatId,
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
        
        if (text.Equals(localizationService.GetString(ResourceKeys.Cancel, language), StringComparison.OrdinalIgnoreCase))
        {
            await stateService.RemoveStateAsync(userId);
            await messageService.ShowMainMenuAsync(chatId,
                language,
                cancellationToken: cancellationToken);
            return;
        }

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

            }
            else if (state.AbsenceType == AbsenceType.OnTheWay)
            {
                await messageService.SendTextMessageAsync(chatId,
                    localizationService.GetString(ResourceKeys.TimeFormatExample,language),
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
                language,
                cancellationToken:cancellationToken);
        }
        else
        {
            await messageService.ShowMainMenuAsync(
                chatId, 
                language,
                cancellationToken:cancellationToken);
        }
    }
}