using System.Text.Json.Serialization;
using DT_HR.Domain.Enumeration;

namespace DT_HR.Contract.CallbackData.Attendance;

public sealed record AbsenceMarkedData(
    [property: JsonPropertyName("telegram_user_id")]
    long TelegramUserId,
    
    string UserName,
    
    [property: JsonPropertyName("absence_reason")]
    string AbsenceReason,
    
    [property: JsonPropertyName("absence_type")]
    AbsenceType AbsenceType,
    
    [property: JsonPropertyName("estimated_arrival_time")]
    DateTime? EstimatedArrivalTime,
    
    [property: JsonPropertyName("marked_at")]
    DateTime? MarkedAt
    );