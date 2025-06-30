using System.Text.Json.Serialization;

namespace DT_HR.Contract.CallbackData.Attendance;

public record CheckInFailureDate(
    [property: JsonPropertyName("telegram_user_id")]
    long TelegramUserId,
    
    [property: JsonPropertyName("error_code")]
    string ErrorCode,
    
    [property: JsonPropertyName("error_message")]
    string ErrorMessage,
    
    [property: JsonPropertyName("attempted_at")]
    DateTime AttemptedAt);