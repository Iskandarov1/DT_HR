using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DT_HR.Persistence.Repositories;

internal sealed class GroupRepository(IDbContext dbContext) : GenericRepository<TelegramGroup>(dbContext), IGroupRepository
{
    public Task<Maybe<TelegramGroup>> GetByChatIdAsync(long chatId, CancellationToken cancellationToken)
        => FirstOrDefaultAsync(g => g.ChatId == chatId, cancellationToken);
    
    public Task<List<TelegramGroup>> GetActiveSubscribersAsync(CancellationToken cancellationToken)
        => DbContext.Set<TelegramGroup>().Where(g => g.IsActive).ToListAsync(cancellationToken);
}