namespace DT_HR.Contract.FailureData;

public record CheckInFailureDate(
    long TelegramUserId,
    string ErrorCode,
    string ErrorMessage,
    DateTime AttemptedAt
    );