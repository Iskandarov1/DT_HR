using DT_HR.Domain.Entities;

namespace DT_HR.Domain.Repositories;

public interface IEventRepository
{
    Task<List<Event>> GetUpcomingEventsAsync(DateTime from, CancellationToken cancellationToken);
    void Insert(Event evt);
}