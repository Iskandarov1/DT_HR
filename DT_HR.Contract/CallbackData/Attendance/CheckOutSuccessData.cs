using System.Text.Json.Serialization;

namespace DT_HR.Contract.CallbackData.Attendance;

public record CheckOutSuccessData(
    [property: JsonPropertyName("telegram_user_id")]
    long TelegramUserId,
    
    [property: JsonPropertyName("username")]
    string UserName,
    
    [property: JsonPropertyName("check_out_time")]
    DateTime CheckOutTime,
    
    [property: JsonPropertyName("is_early_departure")]
    bool IsEarlyDeparture,
    
    [property: JsonPropertyName("work_duration")]
    TimeSpan? WorkDuration = null,
    
    [property: JsonPropertyName("early_by")]
    TimeSpan? EarlyBy = null);