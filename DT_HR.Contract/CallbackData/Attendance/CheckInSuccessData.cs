using System.Text.Json.Serialization;

namespace DT_HR.Contract.CallbackData.Attendance;

public record CheckInSuccessData(
    [property: JsonPropertyName("telegram_user_id")]
    long TelegramUserId,
    
    [property: JsonPropertyName("username")]
    string UserName,
    
    [property: JsonPropertyName("check_in_time")]
    DateTime CheckIntime,
    
    [property: JsonPropertyName("is_within_office_radius")]
    bool IsWithInOfficeRadius,
    
    [property: JsonPropertyName("is_late_arrival")]
    bool IsLateArrival,
    
    [property: JsonPropertyName("late_by")]
    TimeSpan? LateBy = null);