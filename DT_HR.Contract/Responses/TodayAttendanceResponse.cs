using System.Text.Json.Serialization;

namespace DT_HR.Contract.Responses;

public record TodayAttendanceResponse(
   
    [property: JsonPropertyName("date")]DateOnly Date,
    [property: JsonPropertyName("summary")] AttendanceSummary Summary,
    [property: JsonPropertyName("employeeList")]List<EmployeeAttendanceDetail> Employees
    );