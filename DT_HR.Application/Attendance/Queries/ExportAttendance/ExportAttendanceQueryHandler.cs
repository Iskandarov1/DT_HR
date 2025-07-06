using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Contract.Responses;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Enumeration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_HR.Application.Attendance.Queries.ExportAttendance;

public sealed class ExportAttendanceQueryHandler(
    IDbContext dbContext,
    IExcelExportService excelExportService,
    ILogger<ExportAttendanceQueryHandler> logger) : IQueryHandler<ExportAttendanceQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(ExportAttendanceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate user exists and is manager
            var user = await dbContext.Set<User>()
                .FirstOrDefaultAsync(u => u.TelegramUserId == request.TelegramUserId, cancellationToken);

            if (user == null)
            {
                return Result.Failure<byte[]>(new Error("User.NotFound", "User not found"));
            }
            
            if (!user.IsManager())
            {
                return Result.Failure<byte[]>(new Error("User.NotAuthorized", "Only managers can export attendance data"));
            }

            // Validate date range
            if (request.StartDate > request.EndDate)
            {
                return Result.Failure<byte[]>(new Error("DateRange.Invalid", "Start date must be before end date"));
            }

            var query = from u in dbContext.Set<User>()
                where u.IsActive && !u.IsDelete
                join attendance in dbContext.Set<Domain.Entities.Attendance>() on u.Id equals attendance.UserId into attendances
                from att in attendances.DefaultIfEmpty()
                where att == null || (att.Date >= request.StartDate && att.Date <= request.EndDate)
                select new { User = u, Attendance = att };

            var data = await query.ToListAsync(cancellationToken);
            var attendanceData = new List<EmployeeAttendanceResponse>();

            foreach (var item in data)
            {
                var att = item.Attendance;
                bool isLate = att?.IsLateArrival(item.User.WorkStartTime) ?? false;
                bool isEarly = att?.IsEarlyDeparture(item.User.WorkEndTime) ?? false;
                TimeSpan? workDuration = att?.GetWorkDuration();
                
                attendanceData.Add(new EmployeeAttendanceResponse(
                    item.User.Id,
                    $"{item.User.FirstName} {item.User.LastName}",
                    item.User.PhoneNumber,
                    att == null ? "NoRecord" : AttendanceStatus.FromValue(att.Status).Value.Name,
                    att?.CheckInTime,
                    att?.CheckOutTime,
                    isLate,
                    isLate && att?.CheckInTime != null ? (TimeOnly.FromDateTime(att!.CheckInTime!.Value) - item.User.WorkStartTime).ToString(@"hh\:mm") : null,
                    isEarly,
                    att?.AbsenceReason,
                    att?.EstimatedArrivalTime,
                    att?.IsWithInOfficeRadius ?? false,
                    workDuration));
            }

            // Export to Excel
            var excelData = await excelExportService.ExportAttendanceToExcelAsync(
                attendanceData,
                request.StartDate,
                request.EndDate,
                request.Language,
                cancellationToken);

            return Result.Success(excelData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while exporting attendance data for user {UserId}", request.TelegramUserId);
            return Result.Failure<byte[]>(new Error("Export.Failed", "Failed to export attendance data"));
        }
    }
}