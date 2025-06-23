using System.Text.Json.Serialization;

namespace DT_HR.Contract.Responses;

public record EmployeeAttendanceDetail(
    [property: JsonPropertyName("name")]string Name,
    [property: JsonPropertyName("phone_number")]string PhoneNumber,
    [property: JsonPropertyName("status")]string Status,
    [property: JsonPropertyName("check_in_time")]DateTime? CheckInTime,
    [property: JsonPropertyName("check_out_time")]DateTime? CheckOutTime,
    [property: JsonPropertyName("is_late")]bool? IsLate,
    [property: JsonPropertyName("late_by")]string? LateBy,
    [property: JsonPropertyName("absence_reason")]string? AbsenceReason,
    [property: JsonPropertyName("estimated_arrival")]DateTime? EstimatedArrival,
    [property: JsonPropertyName("is_with_in_radius")]bool IsWithInRadius);