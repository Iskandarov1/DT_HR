using System.Text.Json.Serialization;

namespace DT_HR.Contract.Responses;

public sealed record AttendanceSummary(
    [property: JsonPropertyName("total_employees")] int TotalEmployees,
    [property: JsonPropertyName("present")] int Present,
    [property: JsonPropertyName("on_time")] int OnTime,
    [property: JsonPropertyName("late")] int Late,
    [property: JsonPropertyName("absent")] int Absent,
    [property: JsonPropertyName("on_the_way")] int OnTheWay,
    [property: JsonPropertyName("not_checked_in")] int NotCheckedIn );