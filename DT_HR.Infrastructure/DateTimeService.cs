using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Domain.Core;

namespace DT_HR.Infrastructure;

public class DateTimeService :IDateTime
{
    public DateTime UtcNow => TimeUtils.Now;
}