using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;

namespace DT_HR.Domain.Repositories;

public interface IUserRepository
{
    Task<Maybe<User>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Maybe<User>> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken);
    Task<Maybe<User>> GetByPhoneNumberAsync(int phoneNumber, CancellationToken cancellationToken);
    void Insert(User user);
}