using DT_HR.Application.Core.Abstractions.Enum;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Core;
using DT_HR.Domain.Enumeration;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DT_HR.Infrastructure.Telegram;

public class TelegramMessageService(
    ITelegramBotClient botClient,
    ITelegramKeyboardService keyboardService,
    IUserRepository userRepository,
    IAttendanceRepository attendanceRepository,
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

    public async Task SendPlainTextMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.None, // No parsing - plain text only
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error sending plain text message to chat {ChatId}", chatId);
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

    public async Task EditMessageReplyMarkupAsync(long chatId, int messageId, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await botClient.EditMessageReplyMarkup(
                chatId: chatId,
                messageId: messageId,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            
            logger.LogInformation("Message {MessageId} reply markup edited in the chat {ChatId}", messageId, chatId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error editing message {MessageId} reply markup in the chat {ChatId}", messageId, chatId);
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

 

    public async Task ShowMainMenuAsync(long chatId,string language,bool? isManager = null,MainMenuType menuType = MainMenuType.Default, CancellationToken cancellationToken = default)
    {
        var maybeUser = await userRepository.GetByTelegramUserIdAsync(chatId, cancellationToken);
        var managerFlag = isManager ?? (maybeUser.HasValue && maybeUser.Value.IsManager());

        var finalMenuType = menuType;

        if (menuType == MainMenuType.Default && maybeUser.HasValue)
        {
            var today = DateOnly.FromDateTime(TimeUtils.Now);
            var attendance =
                await attendanceRepository.GetByUserAndDateAsync(maybeUser.Value.Id, today, cancellationToken);
            
            
            if (attendance.HasValue)
            {
                if (attendance.Value.CheckInTime.HasValue && !attendance.Value.CheckOutTime.HasValue)
                {
                    finalMenuType = MainMenuType.CheckedIn;
                }
                else if (attendance.Value.CheckInTime.HasValue && attendance.Value.CheckOutTime.HasValue)
                {
                    finalMenuType = MainMenuType.CheckedOut;
                }
                else if (attendance.Value.Status == AttendanceStatus.OnTheWay.Value)
                {
                    finalMenuType = MainMenuType.OnTheWay;
                }
                else
                {
                    finalMenuType = MainMenuType.CheckPrompt;
                }
            }
            else
            {
                finalMenuType = MainMenuType.CheckPrompt;
            }
        }
        
        var keyboard = keyboardService.GetMainMenuKeyboard(language, finalMenuType, managerFlag);
        var text = "|";
        await SendTextMessageAsync(chatId, text, replyMarkup: keyboard, cancellationToken: cancellationToken);
    }

    public async Task SendDocumentAsync(long chatId, InputFile document, string? caption = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await botClient.SendDocument(
                chatId: chatId,
                document: document,
                caption: caption,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending document to chat {ChatId}", chatId);
            throw;
        }
    }
}