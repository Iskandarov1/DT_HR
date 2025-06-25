using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Services.Telegram.CallbackHandlers;
using DT_HR.Services.Telegram.CommandHandlers;
using DT_HR.Services.Telegram.MessageHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DT_HR.Services.Telegram;

public class TelegramBotService : ITelegramBotService 
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramMessageService _messageService;
    private readonly ILocalizationService _localization;
    private readonly IUserStateService _stateService;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly List<ITelegramService> _command;
    private readonly List<ITelegramCallbackQuery> _callbackHandlers;

    public TelegramBotService(
        IServiceProvider serviceProvider,
        ITelegramMessageService messageService,
        ILocalizationService localization,
        IUserStateService stateService,
        ILogger<TelegramBotService> logger)
    {
        _serviceProvider = serviceProvider;
        _messageService = messageService;
        _localization = localization;
        _stateService = stateService;
        _logger = logger;

        _command = new List<ITelegramService>
        {
            serviceProvider.GetRequiredService<StartCommandHandler>(),
            serviceProvider.GetRequiredService<CheckInCommandHandler>(),
            serviceProvider.GetRequiredService<CheckOutCommandHandler>(),
            serviceProvider.GetRequiredService<ReportAbsenceCommandHandler>(),
            serviceProvider.GetRequiredService<StateBasedMessageHandler>()
        };
        _callbackHandlers = new List<ITelegramCallbackQuery>
        {
            serviceProvider.GetRequiredService<LanguageSelectionCallbackHandler>(),
            serviceProvider.GetRequiredService<AbsenceTypeCallbackHandler>(),
            serviceProvider.GetRequiredService<OversleptETACallbackHandler>()
        };

    }
    public Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        return _messageService.SendTextMessageAsync(chatId, text, cancellationToken: cancellationToken);
    }

    public Task SendLocationRequestAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        return _messageService.SendLocationRequestAsync(chatId, text,  "uz", cancellationToken);
    }

    public async Task ProcessUpdateAsync(Update update, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing update {UpdateId} of type {UpdateType}",update.Id,update.Type);

            switch (update.Type)
            {
                case UpdateType.Message:
                    await ProcessMessageAsync(update.Message, cancellationToken);
                    break;
                case UpdateType.CallbackQuery:
                    await ProcessCallbackQueryAsync(update.CallbackQuery, cancellationToken);
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error processing the update {UpdateId}",update.Id);

            long? chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message.Chat.Id;
            if (chatId.HasValue)
            {
                await TrySendErrorMessageAsync(chatId.Value, cancellationToken);
            }

        }
    }


    public async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if(message == null || message.From == null) return;
        
        var userId = message.From.Id;
        
        _logger.LogInformation("Processing message for the user {UserId}:{MessageType} ",userId,message.Type);

        switch (message.Type)
        {
            case MessageType.Text:
                await ProcessTextMessageAsync(message, cancellationToken);
                break;
            case MessageType.Location :
                var locationHandler = _serviceProvider.GetRequiredService<LocationMessageHandler>();
                await locationHandler.HandleAsync(message, cancellationToken);
                break;
            case MessageType.Contact:
                var contactHandler = _serviceProvider.GetRequiredService<ContactMessageHandler>();
                await contactHandler.HandlerAsync(message, cancellationToken);
                break;
            default:
                var state =await _stateService.GetStateAsync(userId);
                var language = state?.Language ?? "uz";
                await _messageService.SendTextMessageAsync(message.Chat.Id,
                    _localization.GetString(ResourceKeys.UnsupportedMessageType,language),
                    cancellationToken: cancellationToken);
                break;
        }
    }

    public async Task ProcessTextMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var text = message.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        foreach (var handler in _command)
        {
            if (await handler.CanHandleAsync(message, cancellationToken))
            {
                await handler.HandleAsync(message, cancellationToken);
                        return;
            }
        }

        var state = await _stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? "uz";
        await _messageService.ShowMainMenuAsync(
            message.Chat.Id,
            _localization.GetString(ResourceKeys.PleaseSelectFromMenu,language),
            language,
            cancellationToken);
    }

    public async Task ProcessCallbackQueryAsync(CallbackQuery? callbackQuery, CancellationToken cancellationToken)
    {
        if(callbackQuery == null || callbackQuery.From == null || callbackQuery.Message == null) return;

        var userId = callbackQuery.From.Id;
        var data = callbackQuery.Data;
        
        _logger.LogInformation("Processing callback query from the user {UserId} : {Data}",userId,data);

        foreach (var handler in _callbackHandlers)
        {
            if (await handler.CanHandleAsync(callbackQuery,cancellationToken))
            {
                await handler.HandleAsync(callbackQuery, cancellationToken);
                return;
            }
        }
        
        _logger.LogWarning("No handler found for callback data : {Data}",data);

        var state = await _stateService.GetStateAsync(userId);
        var language = state?.Language ?? "uz";
        await _messageService.AnswerCallbackQueryAsync(
            callbackQuery.Id, 
            _localization.GetString(ResourceKeys.OptionNotAvailable,language),
            cancellationToken);
    }

    public async Task TrySendErrorMessageAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var state =await _stateService.GetStateAsync(chatId);
            var language = state?.Language ?? "uz";
            await _messageService.ShowMainMenuAsync(
                chatId, 
                _localization.GetString(ResourceKeys.ErrorOccurred,language),
                language,
                cancellationToken);

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send error message to chat {ChatId}", chatId);
        }
    }
}