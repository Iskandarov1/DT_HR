using Microsoft.Extensions.Localization;

namespace DT_HR.Domain.Core.Localizations;

public interface ISharedViewLocalizer
{
	public const string LanguageShortNameUz = "uz", LanguageShortNameRu = "ru", LanguageShortNameEn = "en";
	public LocalizedString this[string key]
	{
		get;
	}

	LocalizedString GetLocalizedString(string key);

	IStringLocalizer Localizer { get; }
}
