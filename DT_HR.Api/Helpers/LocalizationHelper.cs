using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace DT_HR.Api.Helpers;

public static class LocalizationHelper
{
	public static IServiceCollection AddDtHrLocalization(this IServiceCollection services)
	{
		services.AddLocalization(options => options.ResourcesPath = "Resources");
		services.Configure<RequestLocalizationOptions>(options =>
		{
			var supportedCultures = new[]
			{
				new CultureInfo("uz"),
				new CultureInfo("ru"),
				new CultureInfo("en")
			};

			options.DefaultRequestCulture = new RequestCulture("uz");
			options.SupportedCultures = supportedCultures;
			options.SupportedUICultures = supportedCultures;
			options.RequestCultureProviders = new[] { new CookieRequestCultureProvider() };
		});

		services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
		return services;
	}
}
