using DT_HR.Application.Core.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DT_HR.Infrastructure.Telegram;

public class TelegramMessageService(
    ITelegramBotClient botClient,
    ITelegramKeyboardService keyboardService,
    ILogger<TelegramMessageService> logger) : ITelegramMessageService
{
    public async Task SendTextMessageAsync(long chatId, string text, ReplyMarkup? replyMarkup=null,CancellationToken cancellationToken = default)
    {
        try
        {
            await botClient.SendMessage(
                chatId:chatId ,
                text: text,
                parseMode:ParseMode.Markdown,
                replyMarkup:replyMarkup,
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error sending Message to chat {ChatId}",chatId);
            throw;
        }
    }

    public async Task SendLocationRequestAsync(long chatId, string text,string language, CancellationToken cancellationToken = default)
    {
        try
        {
            var keyboard = keyboardService.GetLocationRequestKeyboard(language);

            await botClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode:ParseMode.Markdown,
            replyMarkup:keyboard,
            cancellationToken:cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e,"Error sending location request to chat {ChatId}",chatId);
            throw;
        }
    }

    public async Task EditMessageTextAsync(long chatId, int messageId, string text, InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await botClient.EditMessageText(
                chatId: chatId, 
                messageId:messageId,
                text:text,
                parseMode:ParseMode.Markdown,
                replyMarkup:replyMarkup,
                cancellationToken:cancellationToken);
            
            logger.LogInformation("Message {MessageId} edited in the chat {ChatId}",messageId,chatId);
        }
        catch (Exception e)
        {
            logger.LogError(e,"Error editing message {MessageId} in the chat {ChatId}",messageId,chatId);
            throw;
        }
    }

    public async Task AnswerCallbackQueryAsync(string callbackQueryId, string? text = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await botClient.AnswerCallbackQuery(
                callbackQueryId:callbackQueryId,
                text:text,
                cancellationToken:cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e,"Error answering the CallbackQuery {CallbackQueryId}",callbackQueryId);
        }
    }

    public async Task ShowMainMenuAsync(long chatId, string text,string language, CancellationToken cancellationToken = default)
    {
        var keyboard = keyboardService.GetMainMenuKeyboard(language);
        await SendTextMessageAsync(chatId, text,replyMarkup:keyboard,cancellationToken:cancellationToken);
    }
}