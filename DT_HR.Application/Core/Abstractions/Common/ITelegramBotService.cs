using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace DT_HR.Application.Core.Abstractions.Common;

public interface ITelegramBotService
{
    Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
    Task SendLocationRequestAsync(long chatId, string text, CancellationToken cancellationToken = default);
    Task ProcessUpdateAsync(Update update, CancellationToken cancellationToken = default);
}