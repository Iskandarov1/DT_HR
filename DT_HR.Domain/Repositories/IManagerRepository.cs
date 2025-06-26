using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;

namespace DT_HR.Domain.Repositories;

public interface IManagerRepository
{
    Task<Maybe<Manager>> GetByIdAsync(Guid id, CancellationToken cancellation = default);
    Task<Maybe<Manager>> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default);
    Task<Maybe<Manager>> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<Maybe<Manager>> GetActiveManagersAsync(CancellationToken cancellationToken);
    void Insert(Manager manager);
}