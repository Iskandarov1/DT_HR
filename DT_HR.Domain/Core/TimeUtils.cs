namespace DT_HR.Domain.Core;

public static class TimeUtils
{
    public static readonly TimeSpan LocalOffset = TimeSpan.FromHours(5);
    public static DateTime Now => DateTime.UtcNow + LocalOffset;    
}