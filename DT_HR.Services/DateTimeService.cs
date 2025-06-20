using DT_HR.Application.Core.Abstractions.Common;

namespace DT_HR.Services;

public class DateTimeService :IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}