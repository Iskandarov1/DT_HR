using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DT_HR.Persistence.Repositories;

internal sealed class EventRepository(IDbContext dbContext) : GenericRepository<Event>(dbContext), IEventRepository
{
    public Task<List<Event>> GetUpcomingEventsAsync(DateTime from, CancellationToken cancellationToken) =>
        dbContext.Set<Event>()
            .Where(e => e.EventTime >= from && !e.IsDelete)
            .OrderBy(e => e.EventTime)
            .ToListAsync(cancellationToken);
}