using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace DT_HR.Infrastructure.Services;

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

            if (!webhookUrl.StartsWith("https://") && !webhookUrl.StartsWith("http://"))
            {
                webhookUrl = "https://" + webhookUrl;
            }

            var webhookEndpoint = $"{webhookUrl.TrimEnd('/')}/api/TelegramWebhook/update";
            
            logger.LogInformation("Setting webhook to {webhookUrl}",webhookEndpoint);

            await Task.Delay(1000, cancellationToken);

            await botClient.SetWebhook(
                url: webhookEndpoint,
                allowedUpdates: new[]
                {
                    UpdateType.Message,
                    UpdateType.CallbackQuery
                },
                dropPendingUpdates: true,
                cancellationToken: cancellationToken);

            var webhookInfo = await botClient.GetWebhookInfo(cancellationToken);

            if (string.IsNullOrEmpty(webhookInfo.Url))
            {
                logger.LogError("Failed to set webhook, Webhook url is empty");
                throw new InvalidOperationException("Failed to set webhook");
            }
            
            logger.LogInformation(
                "Webhook configured. URL: {Url}," +
                " Has custom certificate: {HasCustomCertificate}," +
                " Pending updates: {PendingUpdateCount}",
                webhookInfo.Url,
                webhookInfo.HasCustomCertificate,
                webhookInfo.PendingUpdateCount);

            if (!string.IsNullOrEmpty(webhookInfo.LastErrorMessage))
            {
                logger.LogWarning("Webhook has previous error: {LastError} at {LastErrorDate}",webhookInfo.LastErrorMessage, webhookInfo.LastErrorDate);
            }
            
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