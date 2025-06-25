namespace DT_HR.Application.Core.Abstractions.Services;

public interface ILocalizationService
{
    string GetString(string key, string language = "uz");
    string GetString(string key, string language, params object[] arguments);
    void SetCurrentCulture(string language);
    Task<string> GetUserLanguage(long userId);
}