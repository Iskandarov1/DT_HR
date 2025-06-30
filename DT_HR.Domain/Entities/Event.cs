using System.ComponentModel.DataAnnotations.Schema;
using DT_HR.Domain.Core.Primitives;

namespace DT_HR.Domain.Entities;

public class Event : AggregateRoot
{
    private Event() { }

    public Event(string description, DateTime eventTime)
    {
        Description = description;
        EventTime = eventTime;
    }

    [Column("description")] public string Description { get; set; } = string.Empty;
    [Column("event_time")] public DateTime EventTime { get; set; }
}