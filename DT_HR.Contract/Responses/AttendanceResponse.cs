using System.Text.Json.Serialization;

namespace DT_HR.Contract.Responses;

public sealed record AttendanceResponse(
    DateOnly Date,
    [property: JsonPropertyName("total_employees")] int TotalEmployees,
    [property: JsonPropertyName("present")] int Present,
    [property: JsonPropertyName("on_time")] int OnTime,
    [property: JsonPropertyName("late")] int Late,
    [property: JsonPropertyName("absent")] int Absent,
    [property: JsonPropertyName("on_the_way")] int OnTheWay,
    [property: JsonPropertyName("not_checked_in")] int NotCheckedIn,
    [property:JsonPropertyName("worked_today")] int WorkedToday,
    [property: JsonPropertyName("full_day")] int FullDay,
    [property:JsonPropertyName("avarage_work_duration")] TimeSpan AvarageWorkDuration,
    [property:JsonPropertyName("early_departure")] int EarlyDepartures,
    [property:JsonPropertyName("total_absent")] int TotalAbsent,
    [property:JsonPropertyName("employee")] List<EmployeeAttendanceResponse> Employees);
     