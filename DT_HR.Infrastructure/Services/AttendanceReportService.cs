using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Contract.Responses;
using DT_HR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_HR.Infrastructure.Services;
 
public class AttendanceReportService(
    IDbContext dbContext ,
    ILogger<AttendanceReportService> logger) : IAttendanceReportService
{
    public async Task<AttendanceResponse> GetDailyAttendanceReport(DateOnly date, CancellationToken cancellationToken)
    {
        var query = from user in dbContext.Set<User>()
                where user.IsActive && !user.IsDelete
                join attendance in dbContext.Set<Attendance>() on 
                    new { UserId = user.Id, Date = date } equals 
                    new { attendance.UserId, attendance.Date } into attendances
                from att in attendances.DefaultIfEmpty()
                select new { User = user, Attendance = att };

        var data = await query.ToListAsync(cancellationToken);

        var report = new AttendanceResponse(
            Date = date ,
            TotalEmployees = data.Count
        );
        foreach (var item in data)
        {
            var employee = new EmployeeAttendanceDto
            (
                UserId = item.User.Id,
                Name = $"{item.User.FirstName} {item.User.LastName}",
                PhoneNumber = item.User.PhoneNumber,
                IsWithinRadius = item.Attendance?.IsWithInOfficeRadius ?? false
            );
        







    }

    public Task<AttendanceResponse> GetWeeklyAttendanceReport(DateOnly startDate, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<AttendanceResponse> GetYearlyAttendanceReport(int year, int month, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<EmployeeAttendanceResponse>> GetDetailedAttendance(DateOnly date, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}