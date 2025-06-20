using Telegram.Bot.Types;

namespace DT_HR.Application.Core.Abstractions.Services;

public interface ITelegramCallbackQuery
{
    Task<bool> CanHandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default);
    Task HandleAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default);
    
}