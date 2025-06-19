using System.Text.Json.Serialization;


namespace DT_HR.Contract.CallbackData.Attendance;

public sealed record AbsenceMarkedData(
    [property: JsonPropertyName("telegram_user_id")]
    long TelegramUserId,

    [property: JsonPropertyName("username")]
    string UserName,
    
    [property: JsonPropertyName("absence_reason")]
    string AbsenceReason,
    
    [property: JsonPropertyName("absence_type")]
    int AbsenceType,
    
    [property: JsonPropertyName("estimated_arrival_time")]
    DateTime? EstimatedArrivalTime,
    
    [property: JsonPropertyName("marked_at")]
    DateTime? MarkedAt
    );