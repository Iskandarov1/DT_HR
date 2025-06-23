namespace DT_HR.Contract.CallbackData.Attendance;

public record CheckOutSuccessData(
    long TelegramUserId,
    string UserName,
    DateTime CheckOutTime,
    bool IsEarlyDeparture,
    TimeSpan? WorkDuration = null,
    TimeSpan? EarlyBy = null);