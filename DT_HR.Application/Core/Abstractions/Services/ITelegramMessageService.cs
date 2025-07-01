using Telegram.Bot.Types.ReplyMarkups;

namespace DT_HR.Application.Core.Abstractions.Services;

public interface ITelegramMessageService
{
    Task SendTextMessageAsync(long chatId, string text, ReplyMarkup? replyMarkup=null, CancellationToken cancellationToken = default);
    Task SendLocationRequestAsync(long chatId, string text, string language = "uz",CancellationToken cancellationToken = default);
    Task EditMessageTextAsync(long chatId, int messageId, string text, InlineKeyboardMarkup? replyMarkUp = null, CancellationToken cancellationToken = default);

    Task AnswerCallbackQueryAsync(string callbackQueryId, string? text = null,
        CancellationToken cancellationToken = default);

    Task ShowMainMenuAsync(long chatId, string language ,bool? isManager = null, CancellationToken cancellationToken = default);

}