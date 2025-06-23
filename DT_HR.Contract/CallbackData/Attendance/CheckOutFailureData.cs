namespace DT_HR.Contract.CallbackData.Attendance;

public record CheckOutFailureData(
    long TelegramUserId,
    string ErrorCode,
    string ErrorMessage,
    DateTime AttemptedAt);