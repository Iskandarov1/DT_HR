using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace DT_HR.Services.Services;

public class WebhookConfigurationService(
    ITelegramBotClient botClient,
    IConfiguration configuration,
    ILogger<WebhookConfigurationService> logger
    ,IHostEnvironment environment) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var webhookUrl = configuration["Telegram:WebhookUrl"];
            var useWebhook = configuration.GetValue<bool>("Telegram:UseWebhook", true);

            if (!useWebhook)
            {
                await botClient.DeleteWebhook(cancellationToken: cancellationToken);
                logger.LogInformation("Webhook deleted. Bot will use long polling.");
                return;
            }

            if (string.IsNullOrEmpty(webhookUrl))
            {
                logger.LogWarning("Webhook URL not configured. Bot will use long polling.");
                await botClient.DeleteWebhook(cancellationToken: cancellationToken);
                return;
            }

            var webhookEndpoint = $"{webhookUrl}/api/TelegramWebhook/update";
            
            logger.LogInformation("Setting webhook to {webhookUrl}",webhookEndpoint);

            await botClient.SetWebhook(
                url: webhookEndpoint,
                allowedUpdates: new[]
                {
                    UpdateType.Message,
                    UpdateType.CallbackQuery
                },
                cancellationToken: cancellationToken);

            var webhookInfo = await botClient.GetWebhookInfo(cancellationToken);
            
            logger.LogInformation(
                "Webhook configured. URL: {Url}, Has custom certificate: {HasCustomCertificate}, Pending updates: {PendingUpdateCount}",
                webhookInfo.Url,
                webhookInfo.HasCustomCertificate,
                webhookInfo.PendingUpdateCount);
            
        }
        catch (Exception e)
        {
            logger.LogError(e,"Error configuring webhook");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Webhook configuration service stopped");
        return Task.CompletedTask;
    }
}