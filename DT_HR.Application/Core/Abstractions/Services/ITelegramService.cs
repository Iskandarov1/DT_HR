using Telegram.Bot.Types;

namespace DT_HR.Application.Core.Abstractions.Services;

public interface ITelegramService
{
    Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default);
    Task HandleAsync(Message message, CancellationToken cancellationToken = default);
}