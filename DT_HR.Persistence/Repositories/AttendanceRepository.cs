using System.Data;
using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Repositories;

namespace DT_HR.Persistence.Repositories;

internal sealed class AttendanceRepository(IDbContext dbContext) : GenericRepository<Attendance>(dbContext),IAttendanceRepository
{
    public Task<Maybe<Attendance>> GetByUserAndDateAsync(Guid id, DateOnly dateTime,
        CancellationToken cancellationToken = default) =>
        FirstOrDefaultAsync(a => a.UserId == id && a.Date == dateTime, cancellationToken);

    public Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
}