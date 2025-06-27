using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Contract.Responses;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Enumeration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_HR.Infrastructure.Services;

public class AttendanceReportService(
    IDbContext dbContext,
    ILogger<AttendanceReportService> logger) : IAttendanceReportService
{
    public async Task<AttendanceResponse> GetDailyAttendanceReport(DateOnly date, CancellationToken cancellationToken)
    {
        var query = from user in dbContext.Set<User>()
                    where user.IsActive && !user.IsDelete
                    join attendance in dbContext.Set<Attendance>() on new { UserId = user.Id, Date = date } equals new
                        { attendance.UserId, attendance.Date } into attendances
                    from att in attendances.DefaultIfEmpty()
                    select new { User = user, Attendance = att };

        var data = await query.ToListAsync(cancellationToken);
        var employees = new List<EmployeeAttendanceResponse>();
        
        int 
            present = 0, onTime = 0, late = 0, absent = 0, onTheWay = 0, notChecked = 0, worked = 0, fullDay = 0, earlyDeparture = 0, totalAbsent = 0;
            
        TimeSpan totalWork = TimeSpan.Zero;

        foreach (var item in data)
        {
            var att = item.Attendance;
            string status;
            bool isLate = false;
            bool isEarly = false;
            TimeSpan? workDuration = null;

            if (att == null)
            {
                status = "NoRecord";
                notChecked++;
                totalAbsent++;
            }

            else
            {
                var statusEnum = AttendanceStatus.FromValue(att.Status).Value;
                status = statusEnum.Name;

                if (att.Status == AttendanceStatus.Present.Value || att.Status == AttendanceStatus.OnTheWay.Value)
                {
                    present++;
                    if (att.IsLateArrival(item.User.WorkStartTime))
                    {
                        late++;
                        isLate = true;
                    }
                    else
                    {
                        onTime++;
                    }

                    if (att.CheckInTime.HasValue && att.CheckOutTime.HasValue)
                    {
                        workDuration = att.GetWorkDuration();
                        if (workDuration.HasValue)
                        {
                            totalWork += workDuration.Value;
                            worked++;
                            if (workDuration.Value >= item.User.WorkEndTime - item.User.WorkStartTime)
                                fullDay++;
                        }
                    }

                    if (att.IsEarlyDeparture(item.User.WorkEndTime))
                    {
                        earlyDeparture++;
                        isEarly = true;
                    }
                }
                else
                {
                    absent++;
                    if (att.Status == AttendanceStatus.OnTheWay.Value)
                    {
                        onTheWay++;
                        totalAbsent++;
                    }
                }
                
                employees.Add(new EmployeeAttendanceResponse(
                    item.User.Id,
                    $"{item.User.FirstName}{item.User.LastName}",
                    item.User.PhoneNumber,
                    status,
                    att?.CheckInTime,
                    att?.CheckOutTime,
                    isLate,
                    isLate && att?.CheckInTime != null ? (TimeOnly.FromDateTime(att!.CheckInTime.Value)- item.User.WorkStartTime).ToString(@"hh\:mm") : null,
                    isEarly,
                    att?.AbsenceReason,
                    att?.EstimatedArrivalTime,
                    att?.IsWithInOfficeRadius ?? false,
                    workDuration));
            }
        }
        var avgWork = worked > 0 ? TimeSpan.FromTicks(totalWork.Ticks / worked) : TimeSpan.Zero;

        return new AttendanceResponse(
            date,
            data.Count,
            present,
            onTime,
            late,
            absent,
            onTheWay,
            notChecked,
            worked,
            fullDay,
            avgWork,
            earlyDeparture,
            totalAbsent,
            employees);
    }
    public async Task<List<EmployeeAttendanceResponse>> GetDetailedAttendance(DateOnly date, CancellationToken cancellationToken)
    {
        var query = from user in dbContext.Set<User>()
            where user.IsActive && !user.IsDelete
            join attendance in dbContext.Set<Attendance>() on new { UserId = user.Id, Date = date } equals new
                { attendance.UserId, attendance.Date } into attendances
            from att in attendances.DefaultIfEmpty()
            select new { User = user, Attendance = att };

        var data = await query.ToListAsync(cancellationToken);
        var list = new List<EmployeeAttendanceResponse>();

        foreach (var item in data)
        {
            var att = item.Attendance;
            bool isLate = att?.IsLateArrival(item.User.WorkStartTime) ?? false;
            bool isEarly = att?.IsEarlyDeparture(item.User.WorkEndTime) ?? false;
            TimeSpan? workDuration = att?.GetWorkDuration();
            
            
            list.Add(new EmployeeAttendanceResponse(
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

        return list;

    }
    
    
    
    public Task<AttendanceResponse> GetWeeklyAttendanceReport(DateOnly startDate, CancellationToken cancellationToken)
    {
        return Task.FromException<AttendanceResponse>(new NotImplementedException());
    }

    public Task<AttendanceResponse> GetYearlyAttendanceReport(int year, int month, CancellationToken cancellationToken)
    {
        return Task.FromException<AttendanceResponse>(new NotImplementedException());
    }

   
}