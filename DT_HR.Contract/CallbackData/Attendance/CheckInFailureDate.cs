namespace DT_HR.Contract.CallbackData.Attendance;

public record CheckInFailureDate(
    long TelegramUserId,
    string ErrorCode,
    string ErrorMessage,
    DateTime AttemptedAt
    );