using DT_HR.Application.Core.Abstractions.Services;
using Telegram.Bot.Types.ReplyMarkups;


namespace DT_HR.Infrastructure.Telegram;

public class TelegramCalendarService(ILocalizationService localization) : ITelegramCalendarService
{


    public InlineKeyboardMarkup GetCalendarKeyboard(DateTime? selectedDate = null)
    {
        var targetDate = selectedDate ?? DateTime.Now;
        return CreateSimpleCalendar(targetDate);
    }

    public bool IsDateCallback(string callbackData)
    {
        return callbackData.StartsWith("date_") && !IsNavigationCallback(callbackData);
    }

    public bool IsNavigationCallback(string callbackData)
    {
        return callbackData.StartsWith("calendar_prev_") || 
               callbackData.StartsWith("calendar_next_");
    }

    public DateTime? ParseDateFromCallback(string callbackData)
    {
        // Try to parse date from callback data
        if (callbackData.StartsWith("date_"))
        {
            var dateStr = callbackData.Replace("date_", "");
            if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date))
            {
                return date;
            }
        }
        
        // If using calendar library format, try to extract date
        var parts = callbackData.Split('_');
        if (parts.Length >= 4 && int.TryParse(parts[1], out var year) && 
            int.TryParse(parts[2], out var month) && int.TryParse(parts[3], out var day))
        {
            try
            {
                return new DateTime(year, month, day);
            }
            catch
            {
                return null;
            }
        }
        
        return null;
    }

    public (int year, int month, bool isNext) ParseNavigationFromCallback(string callbackData)
    {
        var parts = callbackData.Split('_');
        if (parts.Length >= 4 && int.TryParse(parts[2], out var year) && int.TryParse(parts[3], out var month))
        {
            var isNext = callbackData.StartsWith("calendar_next_");
            return (year, month, isNext);
        }
        
        return (0, 0, false);
    }

    public async Task<InlineKeyboardMarkup> HandleNavigationAsync(string callbackData)
    {
        var (year, month, isNext) = ParseNavigationFromCallback(callbackData);
        
        if (year > 0 && month > 0)
        {
            var targetDate = new DateTime(year, month, 1);
            if (isNext)
            {
                targetDate = targetDate.AddMonths(1);
            }
            else
            {
                targetDate = targetDate.AddMonths(-1);
            }
            
            return GetCalendarKeyboard(targetDate);
        }
        
        return GetCalendarKeyboard();
    }

    private InlineKeyboardMarkup CreateSimpleCalendar(DateTime date)
    {
        var buttons = new List<InlineKeyboardButton[]>();
        

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("◀", $"calendar_prev_{date.Year}_{date.Month}"),
            InlineKeyboardButton.WithCallbackData($"{date:MMMM yyyy}", "ignore"),
            InlineKeyboardButton.WithCallbackData("▶", $"calendar_next_{date.Year}_{date.Month}")
        });
        
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("Mon", "ignore"),
            InlineKeyboardButton.WithCallbackData("Tue", "ignore"),
            InlineKeyboardButton.WithCallbackData("Wed", "ignore"),
            InlineKeyboardButton.WithCallbackData("Thu", "ignore"),
            InlineKeyboardButton.WithCallbackData("Fri", "ignore"),
            InlineKeyboardButton.WithCallbackData("Sat", "ignore"),
            InlineKeyboardButton.WithCallbackData("Sun", "ignore")
        });
        
        // Get first day of month and days in month
        var firstDay = new DateTime(date.Year, date.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        var startDayOfWeek = ((int)firstDay.DayOfWeek + 6) % 7; // Convert to Monday = 0
        
        // Create calendar grid
        var currentWeek = new List<InlineKeyboardButton>();
        
        // Add empty cells for days before the first day of the month
        for (int i = 0; i < startDayOfWeek; i++)
        {
            currentWeek.Add(InlineKeyboardButton.WithCallbackData(" ", "ignore"));
        }
        
        // Add days of the month
        for (int day = 1; day <= daysInMonth; day++)
        {
            var dayDate = new DateTime(date.Year, date.Month, day);
            var callbackData = $"date_{dayDate:yyyy-MM-dd}";
            currentWeek.Add(InlineKeyboardButton.WithCallbackData(day.ToString(), callbackData));
            
            // If we have 7 days in current week, add it to buttons and start new week
            if (currentWeek.Count == 7)
            {
                buttons.Add(currentWeek.ToArray());
                currentWeek = new List<InlineKeyboardButton>();
            }
        }
        
        // Add remaining empty cells and final week if needed
        if (currentWeek.Count > 0)
        {
            while (currentWeek.Count < 7)
            {
                currentWeek.Add(InlineKeyboardButton.WithCallbackData(" ", "ignore"));
            }
            buttons.Add(currentWeek.ToArray());
        }
        
        return new InlineKeyboardMarkup(buttons);
    }
}