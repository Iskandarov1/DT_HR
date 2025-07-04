using DT_HR.Api.Models;
using DT_HR.Application.Attendance.Commands.CheckIn;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace DT_HR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MiniAppController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILocationService _locationService;
    private readonly IUserRepository _userRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MiniAppController> _logger;

    public MiniAppController(
        IMediator mediator,
        ILocationService locationService,
        IUserRepository userRepository,
        ILocalizationService localizationService,
        IConfiguration configuration,
        ILogger<MiniAppController> logger)
    {
        _mediator = mediator;
        _locationService = locationService;
        _userRepository = userRepository;
        _localizationService = localizationService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("checkin")]
    public async Task<ActionResult<MiniAppCheckInResponse>> VerifyLocationAndCheckIn([FromBody] MiniAppCheckInRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get Telegram init data from header
            var telegramInitData = Request.Headers["X-Telegram-Init-Data"].FirstOrDefault();
            if (string.IsNullOrEmpty(telegramInitData))
            {
                _logger.LogWarning("Missing Telegram init data in Mini App request");
                return BadRequest(new MiniAppCheckInResponse
                {
                    Success = false,
                    Error = "Invalid request: Missing Telegram data"
                });
            }

            // Validate Telegram Web App signature
            var validationResult = ValidateTelegramWebAppData(telegramInitData);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid Telegram Web App signature: {Error}", validationResult.Error);
                return BadRequest(new MiniAppCheckInResponse
                {
                    Success = false,
                    Error = "Invalid request: Authentication failed"
                });
            }

            var userId = validationResult.UserId;
            var userLanguage = validationResult.LanguageCode ?? "en";

            // Get user from database
            var user = await _userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);
            if (!user.HasValue)
            {
                _logger.LogWarning("User {UserId} not found for Mini App check-in", userId);
                return BadRequest(new MiniAppCheckInResponse
                {
                    Success = false,
                    Error = GetLocalizedMessage("UserNotFound", userLanguage)
                });
            }

            // Validate location coordinates
            if (!IsValidCoordinates(request.Latitude, request.Longitude))
            {
                _logger.LogWarning("Invalid coordinates provided: {Lat}, {Lon}", request.Latitude, request.Longitude);
                return BadRequest(new MiniAppCheckInResponse
                {
                    Success = false,
                    Error = GetLocalizedMessage("InvalidCoordinates", userLanguage)
                });
            }

            // Check if location is within office radius (this is the key validation)
            var isWithinOfficeRadius = await _locationService.IsWithInOfficeRadiusAsync(request.Latitude, request.Longitude);
            
            if (!isWithinOfficeRadius)
            {
                _logger.LogWarning("User {UserId} attempted Mini App check-in from outside office radius: {Lat}, {Lon}", 
                    userId, request.Latitude, request.Longitude);
                return Ok(new MiniAppCheckInResponse
                {
                    Success = false,
                    Message = GetLocalizedMessage("OutsideOffice", userLanguage)
                });
            }

            // Location is valid - proceed with check-in
            var checkInCommand = new CheckInCommand(userId, request.Latitude, request.Longitude);
            var result = await _mediator.Send(checkInCommand);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successful Mini App check-in for user {UserId}", userId);
                return Ok(new MiniAppCheckInResponse
                {
                    Success = true,
                    Message = GetLocalizedMessage("CheckInSuccess", userLanguage)
                });
            }
            else
            {
                _logger.LogWarning("Check-in failed for user {UserId}: {Error}", userId, result.Error.Message);
                return Ok(new MiniAppCheckInResponse
                {
                    Success = false,
                    Message = result.Error.Message
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Mini App check-in request");
            return StatusCode(500, new MiniAppCheckInResponse
            {
                Success = false,
                Error = "Internal server error"
            });
        }
    }

    private TelegramValidationResult ValidateTelegramWebAppData(string initData)
    {
        try
        {
            var botToken = _configuration["Telegram:BotToken"];
            if (string.IsNullOrEmpty(botToken))
            {
                return new TelegramValidationResult { IsValid = false, Error = "Bot token not configured" };
            }

            // Parse init data
            var parameters = HttpUtility.ParseQueryString(initData);
            var hash = parameters["hash"];
            
            if (string.IsNullOrEmpty(hash))
            {
                return new TelegramValidationResult { IsValid = false, Error = "Missing hash" };
            }

            // Remove hash from parameters for validation
            parameters.Remove("hash");

            // Create data string for validation
            var dataCheckString = string.Join("\n", 
                parameters.AllKeys
                    .OrderBy(key => key)
                    .Select(key => $"{key}={parameters[key]}"));

            // Create secret key
            var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
            
            // Calculate HMAC
            var calculatedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
            var calculatedHashString = Convert.ToHexString(calculatedHash).ToLowerInvariant();

            if (hash.ToLowerInvariant() != calculatedHashString)
            {
                return new TelegramValidationResult { IsValid = false, Error = "Hash mismatch" };
            }

            // Extract user data
            var userParam = parameters["user"];
            if (!string.IsNullOrEmpty(userParam))
            {
                var userJson = JsonSerializer.Deserialize<JsonElement>(userParam);
                if (userJson.TryGetProperty("id", out var idProperty) && idProperty.TryGetInt64(out var userId))
                {
                    var languageCode = userJson.TryGetProperty("language_code", out var langProperty) ? 
                        langProperty.GetString() : "en";
                    
                    return new TelegramValidationResult 
                    { 
                        IsValid = true, 
                        UserId = userId,
                        LanguageCode = languageCode
                    };
                }
            }

            return new TelegramValidationResult { IsValid = false, Error = "Invalid user data" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Telegram Web App data");
            return new TelegramValidationResult { IsValid = false, Error = "Validation error" };
        }
    }

    private bool IsValidCoordinates(double latitude, double longitude)
    {
        return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
    }

    private string GetLocalizedMessage(string key, string language)
    {
        return key switch
        {
            "UserNotFound" => language switch
            {
                "ru" => "Пользователь не найден",
                "uz" => "Foydalanuvchi topilmadi",
                _ => "User not found"
            },
            "InvalidCoordinates" => language switch
            {
                "ru" => "Неверные координаты",
                "uz" => "Noto'g'ri koordinatalar",
                _ => "Invalid coordinates"
            },
            "OutsideOffice" => language switch
            {
                "ru" => "Вы находитесь не в офисе",
                "uz" => "Siz ofis joylashuvida emassiz",
                _ => "You are not at the office location"
            },
            "CheckInSuccess" => language switch
            {
                "ru" => "Успешно зарегистрированы!",
                "uz" => "Muvaffaqiyatli ro'yxatdan o'tdingiz!",
                _ => "Successfully checked in!"
            },
            _ => "Unknown error"
        };
    }

    private class TelegramValidationResult
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
        public long UserId { get; set; }
        public string? LanguageCode { get; set; }
    }
}