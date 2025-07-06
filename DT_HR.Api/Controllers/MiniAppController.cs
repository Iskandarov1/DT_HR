using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.MiniApp.Commands.VerifyLocationAndCheckIn;
using DT_HR.Contract.Requests.MiniApp;
using DT_HR.Contract.Responses.MiniApp;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DT_HR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MiniAppController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITelegramMiniAppService _telegramMiniAppService;
    private readonly ILogger<MiniAppController> _logger;

    public MiniAppController(
        IMediator mediator,
        ITelegramMiniAppService telegramMiniAppService,
        ILogger<MiniAppController> logger)
    {
        _mediator = mediator;
        _telegramMiniAppService = telegramMiniAppService;
        _logger = logger;
    }

    [HttpPost("checkin")]
    public async Task<ActionResult<MiniAppCheckInResponse>> VerifyLocationAndCheckIn(
        [FromBody] MiniAppCheckInRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get Telegram init data from header
            var telegramInitData = Request.Headers["X-Telegram-Init-Data"].FirstOrDefault();
            if (string.IsNullOrEmpty(telegramInitData))
            {
                _logger.LogWarning("Missing Telegram init data in Mini App request");
                return BadRequest(new MiniAppCheckInResponse(
                    Success: false,
                    Error: "Invalid request: Missing Telegram data"
                ));
            }

            // Validate Telegram Web App signature
            var validationResult = await _telegramMiniAppService.ValidateInitDataAsync(telegramInitData);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid Telegram Web App signature: {Error}", validationResult.Error);
                return BadRequest(new MiniAppCheckInResponse(
                    Success: false,
                    Error: "Invalid request: Authentication failed"
                ));
            }

            // Create and send command
            var command = new VerifyLocationAndCheckInCommand(
                TelegramUserId: validationResult.UserId,
                Latitude: request.Latitude,
                Longitude: request.Longitude,
                LanguageCode: validationResult.LanguageCode ?? "en",
                Timestamp: request.Timestamp
            );

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            _logger.LogWarning("Mini App check-in command failed: {Error}", result.Error.Message);
            return StatusCode(500, new MiniAppCheckInResponse(
                Success: false,
                Error: "Internal server error"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Mini App check-in request");
            return StatusCode(500, new MiniAppCheckInResponse(
                Success: false,
                Error: "Internal server error"
            ));
        }
    }
}