using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Repositories;

namespace DT_HR.Persistence.Repositories;

internal sealed class UserRepository(IDbContext dbContext) : GenericRepository<User>(dbContext), IUserRepository
{
    public Task<Maybe<User>> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken) =>
        FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId && u.IsActive, cancellationToken);

    public Task<Maybe<User>> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken) =>
        FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.IsActive, cancellationToken);
}