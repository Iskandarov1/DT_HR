using Telegram.Bot.Types.ReplyMarkups;

namespace DT_HR.Application.Core.Abstractions.Services;

public interface ITelegramKeyboardService
{
    ReplyKeyboardMarkup GetMainMenuKeyboard();
    ReplyKeyboardMarkup GetLocationRequestKeyboard();
    ReplyKeyboardMarkup GetContactRequestKeyboard();
    InlineKeyboardMarkup GetAbsenceTypeKeyboard();
    InlineKeyboardMarkup GetOversleptEtaKeyboard();
    ReplyKeyboardMarkup GetCancelKeyboard();
}