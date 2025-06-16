namespace DT_HR.Contract.SuccessData;

public record CheckInSuccessData(
    long TelegramUserId,
    string UserName,
    DateTime CheckIntime,
    bool IsWithInOfficeRadius,
    bool IsLateArrival,
    TimeSpan? LateBy = null);