using System.ComponentModel.DataAnnotations.Schema;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Enumeration;

namespace DT_HR.Domain.Entities;

public class Attendance : AggregateRoot
{
    private Attendance(){}

    public Attendance(
        Guid userId,
        DateOnly date)
    {
        this.UserId = userId;
        this.Date = date;
        this.Status = AttendanceStatus.Pending.Value;
    }
    public User User { get; set; } = null!;
    [Column("user_Id")]public Guid UserId { get; set; }
    [Column("check_in_time")]public DateTime? CheckInTime { get; set; }
    
    [Column("check_out_time")] public DateTime? CheckOutTime { get; set; }
    [Column("check_in_latitude")]public double? CheckInLatitude { get; set; }
    [Column("check_in_longitude")]public double? CheckInLongitude { get; set; }
    [Column("absence_Reason")]public string? AbsenceReason { get; set; }
    [Column("estimated_arrival_time")]public DateTime? EstimatedArrivalTime { get; set; }
    [Column("is_within_office_radius")]public bool IsWithInOfficeRadius { get; set; }
    [Column("status")] public int Status { get; private set; }
    public DateOnly Date { get; set; }
    
    public Attendance CheckIn(double latitute, double longtitude, bool isWithinRadius)
    {
        this.CheckInTime = DateTime.UtcNow;
        this.CheckInLatitude = latitute;
        this.CheckInLongitude = longtitude;
        this.Status = AttendanceStatus.Present.Value;
        return this;
    }

    public Attendance MarkAbsent(string reason, DateTime? estimatedArrival = null )
    {
        this.Status = estimatedArrival.HasValue ? AttendanceStatus.OnTheWay.Value : AttendanceStatus.Absent.Value;
        this.AbsenceReason = reason;
        this.EstimatedArrivalTime = estimatedArrival?.ToUniversalTime();
        return this;
    }
    public bool IsLateArrival(TimeOnly workStartTime)
    {
        if (!CheckInTime.HasValue)
            return false;
        var checkInTimeOnly = TimeOnly.FromDateTime(CheckInTime.Value);
        return checkInTimeOnly > workStartTime;

    }






}