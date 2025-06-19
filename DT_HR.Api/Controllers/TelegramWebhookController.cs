using DT_HR.Application.Core.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace DT_HR.Api.Controllers;

[ApiController]
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
            _logger.LogInformation("Received update {UpdateId}", update.Id);
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
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
}