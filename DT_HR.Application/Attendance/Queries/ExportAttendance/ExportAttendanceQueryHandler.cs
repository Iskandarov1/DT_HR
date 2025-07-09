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
            
            if (request.StartDate > request.EndDate)
            {
                return Result.Failure<byte[]>(new Error("DateRange.Invalid", "Start date must be before end date"));
            }

            var users = await dbContext.Set<User>()
                .Where(u => u.IsActive && !u.IsDelete)
                .ToListAsync(cancellationToken);

            var attendanceRecords = await dbContext.Set<Domain.Entities.Attendance>()
                .Where(att => att.Date >= request.StartDate && att.Date <= request.EndDate)
                .ToListAsync(cancellationToken);
            
            var attendanceData = new List<EmployeeAttendanceResponse>();
            
            for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
            {
                foreach (var listUser in users)
                {
                    var att = attendanceRecords.FirstOrDefault(a => a.UserId == listUser.Id && a.Date == date);
                        
                    bool isLate = att?.IsLateArrival(listUser.WorkStartTime) ?? false;
                    bool isEarly = att?.IsEarlyDeparture(listUser.WorkEndTime) ?? false;
                    TimeSpan? workDuration = att?.GetWorkDuration();

                    attendanceData.Add(new EmployeeAttendanceResponse(
                        listUser.Id,
                        $"{listUser.FirstName} {listUser.LastName}",
                        listUser.PhoneNumber,
                        att == null ? "NoRecord" : AttendanceStatus.FromValue(att.Status).Value.Name,
                        att?.CheckInTime,
                        att?.CheckOutTime,
                        isLate,
                        isLate && att?.CheckInTime != null
                            ? (TimeOnly.FromDateTime(att!.CheckInTime!.Value) - listUser.WorkStartTime).ToString(
                                @"hh\:mm")
                            : null,
                        isEarly,
                        att?.AbsenceReason,
                        att?.EstimatedArrivalTime,
                        att?.IsWithInOfficeRadius ?? false,
                        workDuration,
                        date));
                }
            }
            
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