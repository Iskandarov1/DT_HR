using DT_HR.Application.Core.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace DT_HR.Infrastructure.Services;

public sealed class TelegramMiniAppService(
    IConfiguration configuration,
    ILogger<TelegramMiniAppService> logger) : ITelegramMiniAppService
{
    public async Task<TelegramMiniAppValidationResult> ValidateInitDataAsync(string initData)
    {
        try
        {
            var botToken = configuration["Telegram:BotToken"];
            if (string.IsNullOrEmpty(botToken))
            {
                return new TelegramMiniAppValidationResult(IsValid: false, Error: "Bot token not configured");
            }

            // Parse init data
            var parameters = HttpUtility.ParseQueryString(initData);
            var hash = parameters["hash"];
            
            if (string.IsNullOrEmpty(hash))
            {
                return new TelegramMiniAppValidationResult(IsValid: false, Error: "Missing hash");
            }

            // Remove hash from parameters for validation
            parameters.Remove("hash");

            // Create data string for validation
            var dataCheckString = string.Join("\n", 
                parameters.AllKeys
                    .OrderBy(key => key)
                    .Select(key => $"{key}={parameters[key]}"));

            // Create secret key
            var secretKey = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes("WebAppData"), 
                Encoding.UTF8.GetBytes(botToken));
            
            // Calculate HMAC
            var calculatedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
            var calculatedHashString = Convert.ToHexString(calculatedHash).ToLowerInvariant();

            if (hash.ToLowerInvariant() != calculatedHashString)
            {
                return new TelegramMiniAppValidationResult(IsValid: false, Error: "Hash mismatch");
            }


            var userParam = parameters["user"];
            if (!string.IsNullOrEmpty(userParam))
            {
                var userJson = JsonSerializer.Deserialize<JsonElement>(userParam);
                if (userJson.TryGetProperty("id", out var idProperty) && idProperty.TryGetInt64(out var userId))
                {
                    var languageCode = userJson.TryGetProperty("language_code", out var langProperty) ? 
                        langProperty.GetString() : "en";
                    
                    return new TelegramMiniAppValidationResult(
                        IsValid: true, 
                        UserId: userId,
                        LanguageCode: languageCode
                    );
                }
            }

            return new TelegramMiniAppValidationResult(IsValid: false, Error: "Invalid user data");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating Telegram Web App data");
            return new TelegramMiniAppValidationResult(IsValid: false, Error: "Validation error");
        }
    }
}