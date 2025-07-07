using System.ComponentModel.DataAnnotations.Schema;
using DT_HR.Domain.Core.Primitives;

namespace DT_HR.Domain.Entities;

public class Company : AggregateRoot
{
    [Column("name")] 
    public string Name { get; private set; }
    
    [Column("default_work_start_time")] 
    public TimeOnly DefaultWorkStartTime { get; private set; }
    
    [Column("default_work_end_time")] 
    public TimeOnly DefaultWorkEndTime { get; private set; }
    
    [Column("time_zone")]
    public string TimeZone { get; private set; }

    [Column("is_active")] public bool IsActive { get; private set; } = true;
    
    private Company(){}

    public Company(string name, TimeOnly defaultWorkStartTime, TimeOnly defaultWorkEndTime, string timeZone = "UTC+5")
    {
        Name = name;
        DefaultWorkStartTime = defaultWorkStartTime;
        DefaultWorkEndTime = defaultWorkEndTime;
        TimeZone = timeZone;
    }

    public void UpdateWorkHours(TimeOnly startTime, TimeOnly endTime)
    {
        if (startTime >= endTime)
            throw new ArgumentException("Start time must be earlier than end time");
        DefaultWorkStartTime = startTime;
        DefaultWorkEndTime = endTime;
    }
    
    
}