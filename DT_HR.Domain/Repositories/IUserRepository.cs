using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;

namespace DT_HR.Domain.Repositories;

public interface IUserRepository
{
    Task<Maybe<User>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Maybe<User>> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken);
    Task<Maybe<User>> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken);
    Task<List<User>> GetActiveUsersAsync(CancellationToken cancellationToken);
    Task<List<User>> GetUsersWithBirthdayAsync(DateOnly date, CancellationToken cancellation = default);
    Task<List<User>> GetManagersAsync(CancellationToken cancellationToken);
    void Insert(User user);
    void Update(User user);
}