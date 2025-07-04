using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Core;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace DT_HR.Api.Controllers;


[Route("api/[controller]")]
public class TelegramWebhookController : ControllerBase
{
    private readonly ITelegramBotService _telegramBotService;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        ITelegramBotService telegramBotService,
        ILogger<TelegramWebhookController> logger)
    {
        _telegramBotService = telegramBotService;
        _logger = logger;
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] Update update)
    {
        try
        {
            _logger.LogInformation("Received update {UpdateId} of type {UpdateType}", update.Id, update.Type);
            
            // Log detailed info for group membership updates
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.MyChatMember)
            {
                _logger.LogInformation("Bot membership update: Chat {ChatId} ({ChatTitle}), Status: {OldStatus} -> {NewStatus}",
                    update.MyChatMember.Chat.Id,
                    update.MyChatMember.Chat.Title,
                    update.MyChatMember.OldChatMember.Status,
                    update.MyChatMember.NewChatMember.Status);
            }
            
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.ChatMember)
            {
                _logger.LogInformation("User membership update: User {UserId} in Chat {ChatId}, Status: {OldStatus} -> {NewStatus}",
                    update.ChatMember.From.Id,
                    update.ChatMember.Chat.Id,
                    update.ChatMember.OldChatMember.Status,
                    update.ChatMember.NewChatMember.Status);
            }
            
            await _telegramBotService.ProcessUpdateAsync(update);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing update {UpdateId}", update.Id);
            // Return OK to prevent Telegram from retrying
            return Ok();
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "Healthy", timestamp = TimeUtils.Now });
    }
}