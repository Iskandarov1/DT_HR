using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;

namespace DT_HR.Domain.Repositories;

public interface IManagerPhoneNumberRepository
{
    Task<Maybe<ManagerPhoneNumber>> GetByPhoneNumberAsync(string phoneNumber,
        CancellationToken cancellationToken = default);

    void Insert(ManagerPhoneNumber managerPhone);
}