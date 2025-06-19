using System.Text.RegularExpressions;
using DT_HR.Application.Attendance.Commands.CheckIn;
using DT_HR.Application.Attendance.Commands.MarkAbsent;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Users.Commands;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Enumeration;
using DT_HR.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DT_HR.Services.Services;

public class TelegramBotService(
    ITelegramBotClient botClient , 
    ILogger<TelegramBotService> logger,
    IConfiguration configuration,
    IMediator mediator,
    IUserStateService userStateService,
    IUserRepository userRepository) : ITelegramBotService
{
    public async Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
            
            logger.LogInformation("Message sent to chat {chatId}",chatId);
        }
        catch (Exception e)
        {
                logger.LogError("Error sending a message to chat {chatId}",chatId);
                throw;
        }
    }

    public async Task SendLocationRequestAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestLocationButton = KeyboardButton.WithRequestLocation("📍 Share Location");
            var cancellButton = new KeyboardButton("Cancell");
            
            
            var keyboard = new ReplyKeyboardMarkup(new[]{ 
                new[] {requestLocationButton},
                new[] {cancellButton}})
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await botClient.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
            logger.LogInformation("Location request sent to chat {ChatId}", chatId);

        }
        catch (Exception e)
        {
            logger.LogError(e, "Error sending location request to chat {ChatId}", chatId);
            throw;
        }
    }

    public async Task ProcessUpdateAsync(Update update, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Processing the update {UpdateId} and type {UpdateType}",update.Id,update.Type);

            switch (update.Type)
            {
                case UpdateType.Message:
                    await ProcessMessageAsync(update.Message, cancellationToken);
                    break;
                case UpdateType.CallbackQuery:
                    await ProcessCallBackQuery(update.CallbackQuery, cancellationToken);
                    break;
                default:
                    logger.LogWarning("Unsupported update type: {UpdateType}",update.Type);
                    break;
            }
        }
        catch (Exception e)
        {
                logger.LogError("Error processing the update {UpdateType}",update.Type);
                throw;
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message == null || message.From == null) return;

        var userId = message.From.Id;
        
        logger.LogInformation("Processing message from the user {userId} and type {MessageType}",userId,message.Type);

        switch (message.Type)
        {
            case MessageType.Text:
                await ProcessTextMessageAsync(message, cancellationToken);
                break;
            case MessageType.Location:
                await ProcessLocationMessageAsync(message, cancellationToken);
                break;
            case MessageType.Contact:
                await ProcessContactMessageAsync(message, cancellationToken);
                break;
            default:
                await SendTextMessageAsync(message.Chat.Id, "Unsupported Message type , please use the given options",
                    cancellationToken);
                break;
        }
    }

    private async Task ProcessTextMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var text = message.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var userId = message.From!.Id;
        var chatid = message.Chat.Id;

        var state = await userStateService.GetStateAsync(userId);
        if (state != null)
        {
            await ProcessUserStateAsync(message, state, cancellationToken);
            return;
        }

        switch (text.ToLower())
        {
            case "/start":
                await ProcessStartCommandAsync(message, cancellationToken);
                break;
            case "/checkin":
            case "Chck in":
                await ProcessCheckInCommandAsync(message, cancellationToken);
                break;
            case "/absent":
            case "report absense":
                await ProcessAbsentCommandAsync(message, cancellationToken);
                break;
            default:
                await ShowMainMenuAsync(chatid, "Plese select and option from the menu", cancellationToken);
                break;

        }

    }

    private async Task ProcessLocationMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Location == null || message.From == null) return;

        var userId = message.From.Id;
        var chatid = message.Chat.Id;

        var state = await userStateService.GetStateAsync(userId);
        if (state !=null && state.CurrentAction == UserAction.CheckingIn)
        {
            await userStateService.RemoveStateAsync(userId);

            var command = new CheckInCommand(
                userId,
                message.Location.Latitude,
                message.Location.Longitude
                );

            var result =  await mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                await ShowMainMenuAsync(chatid, "Check in completed", cancellationToken);
            }
            else
            {
                await ShowMainMenuAsync(chatid, $"Check in failed {result.Error}", cancellationToken);
            }
        }
        else
        {
            await SendTextMessageAsync(chatid, 
                "📍 Location received but no action was pending. Use /checkin to check in.", 
                cancellationToken);
        }

    }

    private async Task ProcessContactMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if(message.Contact == null || message.From == null) return;

        var userId = message.From.Id;
        var chatId = message.Chat.Id;

        var state = await userStateService.GetStateAsync(userId);

        if (state != null && state.CurrentAction == UserAction.Registering)
        {
            await userStateService.RemoveStateAsync(userId);

            var command = new RegisterUserCommand(
                userId,
                message.Contact.PhoneNumber,
                message.Contact.FirstName ?? message.From.FirstName ?? "Unknown",
                message.Contact.LastName ?? message.From.LastName ?? "");
            
            var result = await mediator.Send(command, cancellationToken);
            
            if (result.IsSuccess)
            {
                await ShowMainMenuAsync(chatId, " Registration completed successfully!", cancellationToken);
            }
            else
            {
                await SendTextMessageAsync(chatId, $" Registration failed: {result.Error.Message}", cancellationToken);
            }
        }
        else
        {
            await SendTextMessageAsync(chatId, 
                "📞 Contact received but no registration was pending. Use /start to begin registration.", 
                cancellationToken);
        }
    }

    private async Task ProcessCallBackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if(callbackQuery == null || callbackQuery.From == null || callbackQuery.Message == null) return;

        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message.Chat.Id;
        var data = callbackQuery.Data;
        
        logger.LogInformation("Processing callback query from the user {userId}:{Data}",userId,data);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

        var parts = data?.Split(':') ?? Array.Empty<string>();
        
        if(parts.Length < 1) return;

        var action = parts[0];

        switch (action)
        {
            case "absent_type":
                if (parts.Length > 1)
                    await ProcessAbsenceTypeSelectionAsync(chatId, userId, parts[1], cancellationToken);
                break;
            case "absent_overslept_eta":
                if (parts.Length > 1)
                    await ProcessOversleptETAAsync(chatId, userId, parts[1], cancellationToken);
                break;
            default:
                logger.LogWarning("Unknow callback action: {Action}",action);
                break;
        }
    }

    private async Task ProcessStartCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;


        var user = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);

        if (user.HasNoValue)
        {
            await userStateService.SetStateAsync(userId, new UserState { CurrentAction = UserAction.Registering });

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { KeyboardButton.WithRequestContact("📱 Share Contact") },
                new[] { new KeyboardButton("❌ Cancel") }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await botClient.SendMessage(
                chatId: chatId,
                text: """
                      Welcome to DT HR Attendance System! 👋

                      To get started, I need to register you in our system.
                      Please share your contact information by clicking the button below.
                      """,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        else
        {
            await SendTextMessageAsync(chatId, $"Welcome back {user.Value.FirstName}! 👋", cancellationToken);
            await ShowMainMenuAsync(chatId, "What would you like to do today?", cancellationToken);
        }
    }

    private async Task ProcessCheckInCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;

        await userStateService.SetStateAsync(userId, new UserState { CurrentAction = UserAction.CheckingIn });
        
        await SendLocationRequestAsync(chatId, 
            """
            📍 **Check-In Process**

            Please share your current location to check in.
            Make sure you're within the office radius for successful check-in.
            """, 
            cancellationToken);
        
    }

    private async Task ProcessAbsentCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🤒 Sick/Absent", "absent_type:sick"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🚗 On the way", "absent_type:ontheway"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("😴 Overslept", "absent_type:overslept"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📝 Other reason", "absent_type:other"),
            }
        });

        await botClient.SendMessage(
            chatId:chatId,
            text: """
                  📋 **Report Absence**

                  Please select the reason for your absence:
                  """,
            parseMode:ParseMode.Markdown,
            replyMarkup:keyboard,
            cancellationToken:cancellationToken);
        
    }

    private async Task ProcessAbsenceTypeSelectionAsync(long chatId, long userId, string type,
        CancellationToken cancellationToken)
    {
        switch (type)
        {
            case "sick":
                await userStateService.SetStateAsync(userId, new UserState
                {
                    CurrentAction = UserAction.ReportingAbsence,
                    AbsenceType = AbsenceType.Absent,
                    Data = new Dictionary<string, object> { ["type"] = "sick" }
                });
                await SendTextMessageAsync(chatId,
                    "Please describe your condition or reason for absence:", cancellationToken);
                break;
            case "ontheway":
                await userStateService.SetStateAsync(userId, new UserState
                {
                    CurrentAction = UserAction.ReportingAbsence,
                    AbsenceType = AbsenceType.OnTheWay,
                    Data = new Dictionary<string, object> { ["type"] = "ontheway" }
                });
                await SendTextMessageAsync(chatId,
                    """
                    🚗 You're on the way!

                    Please provide:
                    1. Reason for being late
                    2. Expected arrival time (format: HH:MM)

                    Example: "Traffic jam, 10:30"
                    """, cancellationToken);
                break;
            case "overslept":
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("30 minutes", "absent_overslept_eta:30"),
                        InlineKeyboardButton.WithCallbackData("1 hour", "absent_overslept_eta:60"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("2 hours", "absent_overslept_eta:120"),
                        InlineKeyboardButton.WithCallbackData("Custom time", "absent_overslept_eta:custom"),
                    }
                });

                await botClient.SendMessage(
                chatId:chatId,
                text: """
                      😴 **Overslept**

                      When do you expect to arrive?
                      """,
                parseMode:ParseMode.Markdown,
                replyMarkup:keyboard,
                cancellationToken:cancellationToken);
                break;
            case "other":
                await userStateService.SetStateAsync(userId, new UserState
                {
                    CurrentAction = UserAction.ReportingAbsence,
                    AbsenceType = AbsenceType.Custom,
                    Data = new Dictionary<string, object> { ["type"] = "other" }
                });
                await SendTextMessageAsync(chatId,
                    """
                    Please provide:
                    1. Your reason for absence
                    2. Expected arrival time if you're coming (format: HH:MM) or type "absent" if not coming

                    Example: "Doctor appointment, 14:00" or "Family emergency, absent"
                    """, cancellationToken);
                break;
        }
    }

    private async Task ProcessOversleptETAAsync(long chatId, long userId, string eta,
        CancellationToken cancellationToken)
    {
        await userStateService.SetStateAsync(userId, new UserState
        {
            CurrentAction = UserAction.ReportingAbsence,
            AbsenceType = AbsenceType.Overslept,
            Data = new Dictionary<string, object> { ["type"] = "overslept" }
        });

        if (eta == "custom")
        {
            await SendTextMessageAsync(chatId, "Please enter your expected arrival time (format: HH:MM):",
                cancellationToken);
            
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

            var result = await mediator.Send(command, cancellationToken);

            await userStateService.RemoveStateAsync(userId);
            
            if (result.IsSuccess)
            {
                await ShowMainMenuAsync(chatId, "Your absence has been recorded.", cancellationToken);
            }
            else
            {
                await ShowMainMenuAsync(chatId, $"Failed to record absence: {result.Error.Message}", cancellationToken);
            }
        }
        
    }

    private async Task ProcessUserStateAsync(Message message, UserState state, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim() ?? "";

        switch (state.CurrentAction)
        {
            case UserAction.ReportingAbsence:
                await ProcessAbsenceReasonAsync(message, state, cancellationToken);
                break;
            default:
                logger.LogWarning("Unknown user state action: {Action}", state.CurrentAction);
                await userStateService.RemoveStateAsync(userId);
                await ShowMainMenuAsync(chatId, "Something went wrong. Please try again.", cancellationToken); 
                break;
    
        }
    }

    private async Task ProcessAbsenceReasonAsync(Message message, UserState userState,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim() ?? "";

        DateTime? estimatedArrivalTime = null;
        string reason = text;

        if (userState.AbsenceType == AbsenceType.OnTheWay ||
            (userState.AbsenceType == AbsenceType.Custom && !text.ToLower().Contains("absent")))
        {
            var timeMatch = Regex.Match(text, @"\b(\d{1,2}):(\d{2})\b");
            if (timeMatch.Success)
            {
                var hour = int.Parse(timeMatch.Groups[1].Value);
                var minute = int.Parse(timeMatch.Groups[2].Value);
                var today = DateTime.Today;
                estimatedArrivalTime = today.AddHours(hour).AddMinutes(minute);

                reason = text.Replace(timeMatch.Value, "").Trim().TrimEnd(',').Trim();
            }
            else if (userState.AbsenceType == AbsenceType.OnTheWay)
            {
                await SendTextMessageAsync(chatId,
                    "❌ Please provide a valid time format (HH:MM). Example: 'Traffic, 10:30'", cancellationToken);
                return;
            }

        }
        else if (userState.AbsenceType == AbsenceType.Overslept)
        {
            // For overslept with custom time
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

                reason = "Overslept";
            }
            else
            {
                await SendTextMessageAsync(chatId,
                    "❌ Please provide a valid time format (HH:MM).",
                    cancellationToken);
                return;
            }
        }

        var command = new MarkAbsentCommand(
            userId,
            reason,
            userState.AbsenceType,
            estimatedArrivalTime);

        var result = await mediator.Send(command, cancellationToken);

        await userStateService.RemoveStateAsync(userId);

        if (result.IsSuccess)
        {
            await ShowMainMenuAsync(chatId, "Your absence has been recorded.", cancellationToken);
        }
        else
        {
            await ShowMainMenuAsync(chatId, $"Failed to record absence: {result.Error.Message}", cancellationToken);
        }
    }

    private async Task CancelCurrentOperationAsync(Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;

        await userStateService.RemoveStateAsync(userId);
        await ShowMainMenuAsync(chatId, "Operation cancelled. What would you like to do?", cancellationToken);

    }


    private async Task ShowMainMenuAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("✅ Check In") },
            new[] { new KeyboardButton("🏠 Report Absence") },
        })
        {
            ResizeKeyboard = true,
            IsPersistent = true
        };

        await botClient.SendMessage(
        chatId:chatId,
        text:text,
        parseMode:ParseMode.Markdown,
        replyMarkup:keyboard,
        cancellationToken:cancellationToken);
    }
    

}