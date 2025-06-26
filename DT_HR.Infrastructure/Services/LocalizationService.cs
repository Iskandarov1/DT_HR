using System.Globalization;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Core.Localizations;
using DT_HR.Domain.Repositories;

namespace DT_HR.Infrastructure.Services;

public class LocalizationService(
    ISharedViewLocalizer localizer,
    IUserStateService userStateService,
    IUserRepository userRepository) : ILocalizationService
{
    public string GetString(string key, string language = "uz")
    {
        SetCurrentCulture(language);
        return localizer[key].Value;
    }

    public string GetString(string key, string language, params object[] arguments)
    {
        SetCurrentCulture(language);
        var localizedString = localizer[key].Value;
        return string.Format(localizedString, arguments);
    }

    public void SetCurrentCulture(string language)
    {
        var culture = language switch
        {
            "ru" => new CultureInfo("ru"),
            "en" => new CultureInfo("en"),
            _ => new CultureInfo("uz")
        };
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

    }

    public async Task<string> GetUserLanguage(long userId)
    {
        var state = await userStateService.GetStateAsync(userId);
        if (state != null)
            return state.Language;

        var user = await userRepository.GetByTelegramUserIdAsync(userId, CancellationToken.None);
        if (user.HasValue)
            return user.Value.Language;
        return "uz";
    }
}