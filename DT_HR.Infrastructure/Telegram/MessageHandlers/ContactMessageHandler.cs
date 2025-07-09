using System.Text.RegularExpressions;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace DT_HR.Infrastructure.Telegram.MessageHandlers;

public class ContactMessageHandler(
    IUserStateService stateService,
    ILocalizationService localization,
    ITelegramMessageService messageService,
    ILogger<ContactMessageHandler> logger)
{
    private static readonly Regex PhoneNumberRegex = new Regex(@"^(\+?998)?([3781]{2}|(9[013-57-9]))\d{7}$", RegexOptions.Compiled);
    public async Task HandlerAsync(Message message, CancellationToken cancellationToken)
    {
        if(message.Contact == null || message.From == null) return;

        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        
        logger.LogInformation("Processing Contact information for the user {UserId}",userId);

        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? "uz";

        if (state != null && state.CurrentAction == UserAction.Registering && 
            state.Data.TryGetValue("step", out var step) && step?.ToString() == "phone")
        {
            var phoneNumber = NormalizePhoneNumber(message.Contact.PhoneNumber);
            
            if (!IsValidPhoneNumber(phoneNumber))
            {
                await messageService.SendTextMessageAsync(chatId,
                    localization.GetString(ResourceKeys.InvalidPhoneFormat, language),
                    cancellationToken: cancellationToken);
                return;
            }
            
            state.Data["phone"] = phoneNumber;
            state.Data["firstName"] = message.Contact.FirstName ?? message.From.FirstName ?? "Unknown";
            state.Data["lastName"] = message.Contact.LastName ?? message.From.FirstName ?? String.Empty;
            state.Data["step"] = "birthday";
            await stateService.SetStateAsync(userId, state);

            await messageService.SendTextMessageAsync(chatId,
                localization.GetString(ResourceKeys.EnterBirthDate, language),
                new ReplyKeyboardRemove(),
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
}