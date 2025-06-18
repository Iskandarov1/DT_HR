using System.Text.Json.Serialization;

namespace DT_HR.Contract.CallbackData.Attendance;

public record OnTheWayData(
    
    [property: JsonPropertyName("telegram_user_id")]
    long TelegramUserId,
    
    [property: JsonPropertyName("estimated_arrival_time")]
    DateTime EstimatedArrivalTime,
    
    [property: JsonPropertyName("absence_reason")]
    string AbsenceReason
    );