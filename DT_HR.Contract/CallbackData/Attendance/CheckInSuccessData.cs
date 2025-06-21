namespace DT_HR.Contract.CallbackData.Attendance;

public record CheckInSuccessData(
    long TelegramUserId,
    string UserName,
    DateTime CheckIntime,
    bool IsWithInOfficeRadius,
    bool IsLateArrival,
    TimeSpan? LateBy = null);