namespace DT_HR.Application.Core.Abstractions.Services;

public interface IBackgroundTaskService
{
    Task ScheduleCheckInReminderAsync(long telegramUserId, DateTime scheduledFor,
        CancellationToken cancellationToken = default);

    Task ScheduleArrivalCheckAsync(long telegramUserId, DateTime eta, CancellationToken cancellationToken = default);
    Task ScheduleAttendanceStatsAsync(DateTime scheduledFor, CancellationToken cancellationToken = default);
    Task ScheduleEventReminderAsync(string description, DateTime eventTime, CancellationToken cancellationToken);
    
}