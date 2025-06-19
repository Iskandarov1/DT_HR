using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace DT_HR.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Telegram Bot Client
        services.AddSingleton<ITelegramBotClient>(provider =>
        {
            var token = configuration["Telegram:BotToken"] 
                        ?? throw new InvalidOperationException("Telegram bot token not configured");
            return new TelegramBotClient(token);
        });

        // Register Services
        services.AddScoped<ITelegramBotService, TelegramBotService>();
        services.AddScoped<ILocationService, LocationService>();
        //services.AddSingleton<IBackgroundTaskService, BackgroundTaskService>();
        
        // Register State Management
        services.AddSingleton<IUserStateService, InMemoryUserStateService>();
        services.AddHostedService<InMemoryUserStateService>(provider => 
            (InMemoryUserStateService)provider.GetRequiredService<IUserStateService>());

        return services;
    }
}