using Telegram.Bot.Types.ReplyMarkups;

namespace DT_HR.Application.Core.Abstractions.Services;

public interface ITelegramCalendarService
{
    InlineKeyboardMarkup GetCalendarKeyboard(DateTime? selectedDate = null);
    bool IsDateCallback(string callbackData);
    bool IsNavigationCallback(string callbackData);
    DateTime? ParseDateFromCallback(string callbackData);
    (int year, int month, bool isNext) ParseNavigationFromCallback(string callbackData);
    Task<InlineKeyboardMarkup> HandleNavigationAsync(string callbackData);
}