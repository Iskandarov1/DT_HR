using DT_HR.Application.Core.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DT_HR.Services.Services;

public class TelegramPollingService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelegramPollingService> _logger;
    private readonly IConfiguration _configuration;

    public TelegramPollingService(
        ITelegramBotClient botClient,
        IServiceProvider serviceProvider,
        ILogger<TelegramPollingService> logger,
        IConfiguration configuration)
    {
        _botClient = botClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var useWebhook = _configuration.GetValue<bool>("Telegram:UseWebhook", true);
        
        if (useWebhook)
        {
            _logger.LogInformation("Webhook mode enabled, skipping polling");
            return;
        }

        _logger.LogInformation("Starting Telegram bot polling...");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            },
            DropPendingUpdates = true
        };

        await _botClient.ReceiveAsync(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var telegramBotService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();
            
            _logger.LogInformation("Received update: {UpdateType} from {UserId}", 
                update.Type, 
                update.Message?.From?.Id ?? update.CallbackQuery?.From?.Id);

            await telegramBotService.ProcessUpdateAsync(update, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");
            await HandleErrorAsync(botClient, ex, cancellationToken);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram bot error occurred");
        return Task.CompletedTask;
    }
}