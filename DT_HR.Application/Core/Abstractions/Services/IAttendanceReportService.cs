using DT_HR.Contract.Responses;

namespace DT_HR.Application.Core.Abstractions.Services;

public interface IAttendanceReportService
{
    Task<AttendanceResponse> GetDailyAttendanceReport(DateOnly date, CancellationToken cancellationToken);
    Task<AttendanceResponse> GetWeeklyAttendanceReport(DateOnly startDate, CancellationToken cancellationToken);
    Task<AttendanceResponse> GetYearlyAttendanceReport(int year, int month, CancellationToken cancellationToken);
    Task<List<EmployeeAttendanceResponse>> GetDetailedAttendance(DateOnly date, CancellationToken cancellationToken);
}