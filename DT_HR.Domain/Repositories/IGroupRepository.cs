using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;

namespace DT_HR.Domain.Repositories;

public interface IGroupRepository
{
    Task<List<TelegramGroup>> GetActiveSubscribersAsync(CancellationToken cancellationToken);
    Task<Maybe<TelegramGroup>> GetByChatIdAsync(long chatId, CancellationToken cancellationToken);
    void Insert(TelegramGroup group);
    void Update(TelegramGroup group);
}