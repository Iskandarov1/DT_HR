using System.Globalization;
using System.Text.RegularExpressions;
using DT_HR.Application.Attendance.Commands.MarkAbsent;
using DT_HR.Application.Core.Abstractions.Enum;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Events.Commands;
using DT_HR.Application.Resources;
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
    ITelegramCalendarService calendarService,
    IMediator mediator,
    ILogger<StateBasedMessageHandler> logger,
    ILocalizationService localizationService,
    IUserRepository userRepository) : ITelegramService
{

    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.From == null) return false;

        var state = await stateService.GetStateAsync(message.From.Id);
        return state != null && (state.CurrentAction == UserAction.ReportingAbsenceReason ||
                                 state.CurrentAction == UserAction.ReportingAbsenceEta ||
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
            case UserAction.ReportingAbsenceReason:
                await ProcessAbsenceReasonStepAsync(message, state, language, cancellationToken);
                break;
            case UserAction.ReportingAbsenceEta:
                await ProcessAbsenceEtaStepAsync(message, state, language, cancellationToken);
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
                    menuType:MainMenuType.CheckPrompt,
                    cancellationToken:cancellationToken);
                break;
        }

    }

    private async Task ProcessRegistrationAsync(Message message, UserState state,string language, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim() ?? "";
        

        var step = state.Data.TryGetValue("step", out var s) ? s?.ToString() : "phone";
        if (step == "birthday")
        {
            if (!DateTime.TryParseExact(text,"dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
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
                    menuType: MainMenuType.CheckPrompt,
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
            // Phone number input should be handled by ContactMessageHandler
            // This handler only processes birthday step
            await messageService.SendTextMessageAsync(chatId,
                localizationService.GetString(ResourceKeys.InvalidPhoneFormat, language),
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
            await messageService.ShowMainMenuAsync(
                chatId,
                language,
                menuType: MainMenuType.CheckPrompt,
                cancellationToken: cancellationToken);
            return;
        }
        
        var text = message.Text?.Trim() ?? string.Empty;
        
        if (text.Equals(localizationService.GetString(ResourceKeys.Cancel, language), StringComparison.OrdinalIgnoreCase))
        {
            await stateService.RemoveStateAsync(userId);
            await messageService.ShowMainMenuAsync(chatId,
                language,
                menuType: MainMenuType.CheckPrompt,
                cancellationToken: cancellationToken);
            return;
        }
        
        var step = state.Data.TryGetValue("step", out var s) ? s?.ToString() : "description";

        if (step == "description")
        {
            state.Data["description"] = text;
            state.Data["step"] = "date";
            await stateService.SetStateAsync(userId, state);
            
            var calendarKeyboard = calendarService.GetCalendarKeyboard();
            
            await messageService.SendTextMessageAsync(chatId,
                localizationService.GetString(ResourceKeys.SelectEventDate, language),
                calendarKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        if (step == "time")
        {
            if (!TimeSpan.TryParseExact(text, @"hh\:mm", CultureInfo.InvariantCulture, out var timeSpan))
            {
                await messageService.SendTextMessageAsync(chatId,
                    localizationService.GetString(ResourceKeys.InvalidTimeFormat, language),
                    cancellationToken: cancellationToken);
                return;
            }

            var dateString = state.Data["selectedDate"]?.ToString();
            if (string.IsNullOrEmpty(dateString) || !DateTime.TryParse(dateString, out var selectedDate))
            {
                await messageService.SendTextMessageAsync(chatId,
                    "Error: Date not selected. Please start over.",
                    cancellationToken: cancellationToken);
                return;
            }

            var localTime = selectedDate.Date.Add(timeSpan);
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
                    var eventCreated = localizationService.GetString(ResourceKeys.EventCreated, language);
                    var date = localizationService.GetString(ResourceKeys.Date, language);
                    var eventTime = localizationService.GetString(ResourceKeys.Event, language);
                    // Display in local time (UTC+5)
                    var displayTime = utcEventTime.AddHours(5);
                    var msg = $"🔔 *{eventCreated}*\n\n" +
                             $"📅 {eventTime}: {description}\n" +
                             $"⏰ {date}: {displayTime:dd-MM-yyyy HH:mm}";
                    await messageService.SendTextMessageAsync(u.TelegramUserId, msg, cancellationToken: cancellationToken);
                }

                await messageService.ShowMainMenuAsync(
                    chatId,
                    language,
                    menuType: MainMenuType.Default,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await messageService.ShowMainMenuAsync(
                    chatId,
                    language,
                    menuType: MainMenuType.Default,
                    cancellationToken: cancellationToken);
            }
        }

    }


    private async Task ProcessAbsenceReasonStepAsync(Message message, UserState state, string language, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim() ?? "";
        
        if (text.Equals(localizationService.GetString(ResourceKeys.Cancel, language), StringComparison.OrdinalIgnoreCase))
        {
            await stateService.RemoveStateAsync(userId);
            await messageService.ShowMainMenuAsync(
                chatId,
                language,
                menuType: MainMenuType.CheckPrompt,
                cancellationToken: cancellationToken);
            return;
        }

        state.Data["reason"] = text;
        await stateService.SetStateAsync(userId, state);
        
        // Show appropriate ETA keyboard based on absence type
        if (state.AbsenceType == AbsenceType.OnTheWay)
        {
            var keyboard = keyboardService.GetOnTheWayEtaKeyboard(language);
            await messageService.SendTextMessageAsync(
                chatId,
                localizationService.GetString(ResourceKeys.OnTheWayEtaPrompt, language),
                keyboard,
                cancellationToken: cancellationToken);
        }
        else if (state.AbsenceType == AbsenceType.Custom)
        {
            var keyboard = keyboardService.GetOtherEtaKeyboard(language);
            await messageService.SendTextMessageAsync(
                chatId,
                localizationService.GetString(ResourceKeys.OtherEtaPrompt, language),
                keyboard,
                cancellationToken: cancellationToken);
        }
    }
    private async Task ProcessAbsenceEtaStepAsync(Message message, UserState state, string language, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim() ?? "";
        
        if (text.Equals(localizationService.GetString(ResourceKeys.Cancel, language), StringComparison.OrdinalIgnoreCase))
        {
            await stateService.RemoveStateAsync(userId);
            await messageService.ShowMainMenuAsync(
                chatId,
                language,
                menuType: MainMenuType.CheckPrompt,
                cancellationToken: cancellationToken);
            return;
        }

        DateTime? estimatedArrivalTime = null;
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
        }
        else
        {
            await messageService.SendTextMessageAsync(chatId,
                localizationService.GetString(ResourceKeys.TimeFormatExample, language),
                cancellationToken: cancellationToken);
            return;
        }

        var reason = state.Data.TryGetValue("reason", out var reasonObj) ? reasonObj?.ToString() : string.Empty;
        
        if (state.AbsenceType == null)
        {
            await messageService.SendTextMessageAsync(chatId,
                localizationService.GetString(ResourceKeys.ErrorOccurred, language),
                cancellationToken: cancellationToken);
            return;
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
}