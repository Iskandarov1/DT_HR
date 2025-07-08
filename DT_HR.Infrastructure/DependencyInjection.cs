using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Repositories;
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
        services.AddScoped<IUserBackgroundJobService, UserBackgroundJobService>();
        services.AddScoped<ITelegramMiniAppService, TelegramMiniAppService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        


        
        
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
        services.AddScoped<ExportAttendanceCommandHandler>();
        
        services.AddScoped<ContactMessageHandler>();
        services.AddScoped<ExportDateInputHandler>();
        services.AddScoped<WorkTimeInputMessageHandler>();
        services.AddScoped<ManagerSettingsMessageHandler>();

        services.AddScoped<LanguageSelectionCallbackHandler>();
        services.AddScoped<AbsenceTypeCallbackHandler>();
        services.AddScoped<OversleptETACallbackHandler>();
        services.AddScoped<CancelCallbackHandler>();
        services.AddScoped<ExportDateRangeCallbackHandler>();
        services.AddScoped<WorkTimeSettingsCallbackHandler>();
        
        services.AddHostedService<WebhookConfigurationService>();
        services.AddHostedService<TelegramPollingService>();
        services.AddHostedService<BackgroundTaskInitializer>();
        services.AddHostedService<CompanySeedingService>();

        
        return services;
    }
}