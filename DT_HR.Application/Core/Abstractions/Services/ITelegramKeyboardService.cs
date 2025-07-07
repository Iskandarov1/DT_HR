using DT_HR.Application.Core.Abstractions.Enum;
using Telegram.Bot.Types.ReplyMarkups;

namespace DT_HR.Application.Core.Abstractions.Services;

public interface ITelegramKeyboardService
{
    InlineKeyboardMarkup GetLanguageSelectionKeyboard();
    ReplyKeyboardMarkup GetPhoneNumberOptionsKeyboard(string language = "uz");

    ReplyKeyboardMarkup GetMainMenuKeyboard(string language = "uz",MainMenuType menuType = MainMenuType.Default, bool isManager = false);
    ReplyKeyboardMarkup GetContactRequestKeyboard(string language = "uz");
    InlineKeyboardMarkup GetAbsenceTypeKeyboard(string language = "uz");
    InlineKeyboardMarkup GetOversleptEtaKeyboard(string language = "uz");
    ReplyKeyboardMarkup GetCancelKeyboard(string language = "uz");
    InlineKeyboardMarkup GetCancelInlineKeyboard(string language = "uz");
    InlineKeyboardMarkup GetCheckInOptionsKeyboard(string language = "uz");
    InlineKeyboardMarkup CreateDateRangeSelectionKeyboard(string language = "uz");
    InlineKeyboardMarkup GetManagerSettingsKeyboard(string language = "uz");
    InlineKeyboardMarkup GetWorkTimeSettingsKeyboard(string language = "uz");

}