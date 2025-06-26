using System.Text.Json.Serialization;

namespace DT_HR.Contract.Responses;

public sealed record EmployeeAttendanceResponse(
    [property:JsonPropertyName ("user_id")]Guid UserId,
    [property: JsonPropertyName("name")]string Name,
    [property: JsonPropertyName("phone_number")]string PhoneNumber,
    [property: JsonPropertyName("status")]string Status,
    [property: JsonPropertyName("check_in_time")]DateTime? CheckInTime,
    [property: JsonPropertyName("check_out_time")]DateTime? CheckOutTime,
    [property: JsonPropertyName("is_late")]bool? IsLate,
    [property: JsonPropertyName("late_by")]string? LateBy,
    [property:JsonPropertyName ("is_early_departure")]bool IsEarlyDeparture,
    [property: JsonPropertyName("absence_reason")]string? AbsenceReason,
    [property: JsonPropertyName("estimated_arrival")]DateTime? EstimatedArrival,
    [property: JsonPropertyName("is_with_in_radius")]bool IsWithInRadius,
    [property:JsonPropertyName ("work_duration")] TimeSpan? WorkDuration );