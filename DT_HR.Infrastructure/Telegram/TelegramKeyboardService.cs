using System.Diagnostics;
using DT_HR.Application.Core.Abstractions.Enum;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using Telegram.Bot.Types.ReplyMarkups;

namespace DT_HR.Infrastructure.Telegram;

public class TelegramKeyboardService(ILocalizationService localization) : ITelegramKeyboardService
{
    public InlineKeyboardMarkup GetLanguageSelectionKeyboard()
    {
        return new InlineKeyboardMarkup(new []
        {
            new []
            {
                InlineKeyboardButton.WithCallbackData("🇺🇿 O'zbek", "lang:uz"), 
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData("🇷🇺 Русский","lang:ru"), 
            },
            new []
            {
                InlineKeyboardButton.WithCallbackData("🇬🇧 English", "lang:en"), 
            }
        });
    }

    public ReplyKeyboardMarkup GetPhoneNumberOptionsKeyboard(string language = "uz")
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[]
            {
                KeyboardButton.WithRequestContact(localization.GetString(ResourceKeys.ShareContactButton, language)),
            },
            new[]
            {
                new KeyboardButton(localization.GetString(ResourceKeys.Cancel, language))
            }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
        return keyboard;
    }
    public ReplyKeyboardMarkup GetMainMenuKeyboard(string language, MainMenuType menuType = MainMenuType.Default, bool isManager = false)
    {
        var rows = new List<KeyboardButton[]>();
        
        if (isManager)
        {
            rows.Add(new[]
            {
                new KeyboardButton(localization.GetString(ResourceKeys.AttendanceStats, language).Trim()),
                new KeyboardButton(localization.GetString(ResourceKeys.AttendanceDetails, language).Trim())
            });
            rows.Add(new []
            {
                new KeyboardButton(localization.GetString(ResourceKeys.CreateEvent,language).Trim())
            });
        }

        switch (menuType)
        {
           case MainMenuType.CheckPrompt :
               rows.Add(new []
               {
                   new KeyboardButton(localization.GetString(ResourceKeys.CheckIn,language)),
                   new KeyboardButton(localization.GetString(ResourceKeys.ReportAbsence,language))
               });
               break;
           case MainMenuType.CheckedIn:
               rows.Add(new[]
               {
                   new KeyboardButton(localization.GetString(ResourceKeys.CheckOut, language)),
                   new KeyboardButton(localization.GetString(ResourceKeys.MyEvents, language))
               });
               rows.Add(new []
               {
                   new KeyboardButton(localization.GetString(ResourceKeys.Settings,language))
               });
               break;
           case MainMenuType.CheckedOut:
               rows.Add(new[]
               {
                   new KeyboardButton(localization.GetString(ResourceKeys.MyEvents, language))
               });
               break;
           case MainMenuType.OnTheWay:
               rows.Add(new[]
               {
                   new KeyboardButton(localization.GetString(ResourceKeys.CheckIn, language)),
                   new KeyboardButton(localization.GetString(ResourceKeys.ReportAbsence, language))
               });
               break;
           case MainMenuType.Custom:
               rows.Add(new[]
               {
                   new KeyboardButton(localization.GetString(ResourceKeys.CheckIn, language)),
                   new KeyboardButton(localization.GetString(ResourceKeys.ReportAbsence, language))
               });
               break;
           default:
               if (!isManager)
               {
                   rows.Add(new[]
                   {
                       new KeyboardButton(localization.GetString(ResourceKeys.CheckIn, language)),
                       new KeyboardButton(localization.GetString(ResourceKeys.CheckOut, language))
                   });
                   rows.Add(new[]
                   {
                       new KeyboardButton(localization.GetString(ResourceKeys.ReportAbsence, language))
                   });
                   rows.Add(new[]
                   {
                       new KeyboardButton(localization.GetString(ResourceKeys.MyEvents, language))
                   });
               }
               break;
        }
       
        
        return new ReplyKeyboardMarkup(rows)
        {
            ResizeKeyboard = true,
            IsPersistent = true
        };
    }

    public ReplyKeyboardMarkup GetLocationRequestKeyboard(string language)
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[]
            {
                KeyboardButton.WithRequestLocation(localization.GetString(ResourceKeys.ShareLocation,language))
            },
            new[]
            {
                new KeyboardButton(localization.GetString(ResourceKeys.Cancel,language))
            }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }

    public ReplyKeyboardMarkup GetContactRequestKeyboard(string language)
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { KeyboardButton.WithRequestContact(localization.GetString(ResourceKeys.ShareContactButton, language)) },
            new[] { new KeyboardButton(localization.GetString( ResourceKeys.Cancel)) }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }

    public InlineKeyboardMarkup GetAbsenceTypeKeyboard(string language)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(localization.GetString(ResourceKeys.Sick, language), "absent_type:sick"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(localization.GetString(ResourceKeys.OnTheWay,language), "absent_type:ontheway"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(localization.GetString(ResourceKeys.Overslept,language), "absent_type:overslept"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(localization.GetString(ResourceKeys.OtherReason,language), "absent_type:other"),
            }
            // new[]
            // {
            //     InlineKeyboardButton.WithCallbackData(localization.GetString(ResourceKeys.Cancel, language), "action:cancel"),
            // }
        });
    }

    public InlineKeyboardMarkup GetOversleptEtaKeyboard(string language)
    {
        var minutesText = language switch
        {
            "ru" => "минут",
            "en" => "minutes",
            _ => "daqiqa"
        };
        var hourText = language switch
        {
            "ru" => "час",
            "en" => "hour",
            _ => "soat"
        };
        var hoursText = language switch
        {
            "ru" => "часа",
            "en" => "hours",
            _ => "soat"
        };
        var customTimeText = language switch
        {
            "ru" => "Другое время",
            "en" => "Custom time",
            _ => "Boshqa vaqt"
        };
        
        
        
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData($"30 {minutesText}", "absent_overslept_eta:30"),
                InlineKeyboardButton.WithCallbackData($"1 {hourText}", "absent_overslept_eta:60"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData($"2 {hoursText}", "absent_overslept_eta:120"),
                InlineKeyboardButton.WithCallbackData(customTimeText, "absent_overslept_eta:custom"),
            }
        });
    }

    public ReplyKeyboardMarkup GetCancelKeyboard(string language)
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton(localization.GetString(ResourceKeys.Cancel, language)) }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }

    public InlineKeyboardMarkup GetCancelInlineKeyboard(string language = "uz")
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(localization.GetString(ResourceKeys.Cancel, language),
                    "action:cancel")
            }
        });
    }
}