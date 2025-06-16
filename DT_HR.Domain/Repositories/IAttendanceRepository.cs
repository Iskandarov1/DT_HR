using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;

namespace DT_HR.Domain.Repositories;

public interface IAttendanceRepository
{
    Task<Maybe<Attendance>> GetByUserAndDateAsync(Guid id, DateOnly dateTime, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default );
    void Insert(Attendance attendance);
}