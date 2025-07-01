using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Infrastructure.Services;
using DT_HR.Infrastructure.Telegram;
using DT_HR.Infrastructure.Telegram.CallbackHandlers;
using DT_HR.Infrastructure.Telegram.CommandHandlers;
using DT_HR.Infrastructure.Telegram.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace DT_HR.Infrastructure;

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
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IAttendanceReportService, AttendanceReportService>();
        services.AddScoped<IBackgroundTaskService, HangfireBackgroundTaskService>();


        
        
        services.AddScoped<IDateTime, DateTimeService>();
        //State

        services.AddSingleton<IUserStateService, InMemoryUserStateService>();
        services.AddHostedService<InMemoryUserStateService>(provider => 
            (InMemoryUserStateService)provider.GetRequiredService<IUserStateService>());

        services.AddScoped<StartCommandHandler>();
        services.AddScoped<CheckInCommandHandler>();
        services.AddScoped<CheckOutCommandHandler>();
        services.AddScoped<ReportAbsenceCommandHandler>();
        services.AddScoped<SettingsCommandHandler>();
        services.AddScoped<AttendanceStatsCommandHandler>();
        services.AddScoped<AttendanceDetailsCommandHandler>();
        services.AddScoped<StateBasedMessageHandler>();
        services.AddScoped<EventCommandHandler>();
        services.AddScoped<MyEventsCommandHandler>();



        services.AddScoped<LocationMessageHandler>();
        services.AddScoped<ContactMessageHandler>();

        services.AddScoped<LanguageSelectionCallbackHandler>();
        services.AddScoped<AbsenceTypeCallbackHandler>();
        services.AddScoped<OversleptETACallbackHandler>();
        services.AddScoped<CancelCallbackHandler>();
        
        services.AddHostedService<WebhookConfigurationService>();
        services.AddHostedService<TelegramPollingService>();
        services.AddHostedService<BackgroundTaskInitializer>();

        
        return services;
    }
}