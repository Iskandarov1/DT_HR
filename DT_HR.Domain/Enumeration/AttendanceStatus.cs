using DT_HR.Domain.Core.Primitives;

namespace DT_HR.Domain.Enumeration;

public sealed class AttendanceStatus : Enumeration<AttendanceStatus>
{
    
    public static readonly AttendanceStatus Pending = new AttendanceStatus(0, "Pending");
    public static readonly AttendanceStatus Present = new AttendanceStatus(1, "Present");
    public static readonly AttendanceStatus Absent = new AttendanceStatus(2,"Absent");
    public static readonly AttendanceStatus OnTheWay = new AttendanceStatus(3, "OnTheWay");
    
    
    
    private AttendanceStatus(int value, string name): base(value,name) { }


}