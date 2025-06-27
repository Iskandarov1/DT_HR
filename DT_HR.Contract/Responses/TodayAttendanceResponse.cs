using System.Text.Json.Serialization;

namespace DT_HR.Contract.Responses;

public sealed record DailyAttendanceResponse(
   
    [property: JsonPropertyName("date")]DateOnly Date,
    [property: JsonPropertyName("summary")] AttendanceResponse Response,
    [property: JsonPropertyName("employeeList")]List<EmployeeAttendanceResponse> Employees
    );