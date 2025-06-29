using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Enumeration;
using DT_HR.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DT_HR.Persistence.Repositories;

internal sealed class UserRepository(IDbContext dbContext) : GenericRepository<User>(dbContext), IUserRepository
{
    public Task<Maybe<User>> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken) =>
        FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId && u.IsActive, cancellationToken);

    public Task<Maybe<User>> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken) =>
        FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.IsActive, cancellationToken);

    public Task<List<User>> GetActiveUsersAsync(CancellationToken cancellationToken) =>
        DbContext.Set<User>().Where(u => u.IsActive).ToListAsync(cancellationToken);

    public Task<List<User>> GetManagersAsync(CancellationToken cancellationToken) =>
        DbContext.Set<User>()
            .Where(u => u.IsActive && u.Role == UserRole.Manager.Name)
            .ToListAsync(cancellationToken);
}