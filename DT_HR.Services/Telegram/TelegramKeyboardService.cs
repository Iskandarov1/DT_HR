using DT_HR.Application.Core.Abstractions.Services;
using Telegram.Bot.Types.ReplyMarkups;

namespace DT_HR.Services.Telegram;

public class TelegramKeyboardService : ITelegramKeyboardService
{
    public ReplyKeyboardMarkup GetMainMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("✅ Check In") },
            new[] { new KeyboardButton("🏠 Report Absence") },
        })
        {
            ResizeKeyboard = true,
            IsPersistent = true
        };
    }

    public ReplyKeyboardMarkup GetLocationRequestKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { KeyboardButton.WithRequestLocation("📍 Share Location") },
            new[] { new KeyboardButton("❌ Cancel") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }

    public ReplyKeyboardMarkup GetContactRequestKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { KeyboardButton.WithRequestContact("📱 Share Contact") },
            new[] { new KeyboardButton("❌ Cancel") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }

    public InlineKeyboardMarkup GetAbsenceTypeKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
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
    }

    public InlineKeyboardMarkup GetOversleptEtaKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
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
    }

    public ReplyKeyboardMarkup GetCancelKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("❌ Cancel") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }
}