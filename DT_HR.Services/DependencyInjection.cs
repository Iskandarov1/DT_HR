using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Services.Services;
using DT_HR.Services.Telegram;
using DT_HR.Services.Telegram.CallbackHandlers;
using DT_HR.Services.Telegram.CommandHandlers;
using DT_HR.Services.Telegram.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace DT_HR.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddSingleton<ITelegramBotClient>(provider =>
        {
            var token = configuration["Telegram:BotToken"] 
                        ?? throw new InvalidOperationException("Telegram bot token not configured");
            return new TelegramBotClient(token);
        });

        //Core
        services.AddScoped<ITelegramBotService, TelegramBotService>();
        services.AddScoped<ITelegramMessageService, TelegramMessageService>();
        services.AddScoped<ITelegramKeyboardService, TelegramKeyboardService>();
        services.AddScoped<ILocationService, LocationService>();

        services.AddSingleton<IBackgroundTaskService, BackgroundTaskService>();
        services.AddHostedService<BackgroundTaskService>(provider => 
            (BackgroundTaskService)provider.GetRequiredService<IBackgroundTaskService>());
        
        services.AddScoped<IDateTime, DateTimeService>();
        //State

        services.AddSingleton<IUserStateService, InMemoryUserStateService>();
        services.AddHostedService<InMemoryUserStateService>(provider => 
            (InMemoryUserStateService)provider.GetRequiredService<IUserStateService>());

        services.AddScoped<StartCommandHandler>();
        services.AddScoped<CheckInCommandHandler>();
        services.AddScoped<CheckOutCommandHandler>();
        services.AddScoped<ReportAbsenceCommandHandler>();
        services.AddScoped<StateBasedMessageHandler>();

        services.AddScoped<LocationMessageHandler>();
        services.AddScoped<ContactMessageHandler>();

        services.AddScoped<AbsenceTypeCallbackHandler>();
        services.AddScoped<OversleptETACallbackHandler>();
        
        services.AddHostedService<WebhookConfigurationService>();
        services.AddHostedService<TelegramPollingService>();

        
        return services;
    }
}