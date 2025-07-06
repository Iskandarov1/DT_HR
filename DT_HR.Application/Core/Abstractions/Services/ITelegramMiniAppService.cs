namespace DT_HR.Application.Core.Abstractions.Services;

public interface ITelegramMiniAppService
{
    Task<TelegramMiniAppValidationResult> ValidateInitDataAsync(string initData);
}

public sealed record TelegramMiniAppValidationResult(
    bool IsValid,
    string? Error = null,
    long UserId = 0,
    string? LanguageCode = null
);