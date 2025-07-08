using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Repositories;
using DT_HR.Infrastructure.Telegram.CallbackHandlers;
using DT_HR.Infrastructure.Telegram.CommandHandlers;
using DT_HR.Infrastructure.Telegram.MessageHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DT_HR.Infrastructure.Telegram;

public class TelegramBotService : ITelegramBotService 
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramMessageService _messageService;
    private readonly ILocalizationService _localization;
    private readonly IUserStateService _stateService;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly List<ITelegramService> _command;
    private readonly List<ITelegramCallbackQuery> _callbackHandlers;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMembershipRepository _groupMembership;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TelegramBotService(
        IServiceProvider serviceProvider,
        ITelegramMessageService messageService,
        ILocalizationService localization,
        IGroupRepository groupRepository,
        IGroupMembershipRepository groupMembership,
        IUserRepository userRepository,
        IUserStateService stateService,
        IUnitOfWork unitOfWork,
        ILogger<TelegramBotService> logger)
    {
        _serviceProvider = serviceProvider;
        _messageService = messageService;
        _localization = localization;
        _stateService = stateService;
        _groupRepository = groupRepository;
        _groupMembership = groupMembership;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;

        
        _logger = logger;

        _command = new List<ITelegramService>
        {
            serviceProvider.GetRequiredService<StartCommandHandler>(),
            serviceProvider.GetRequiredService<CheckInCommandHandler>(),
            serviceProvider.GetRequiredService<CheckOutCommandHandler>(),
            serviceProvider.GetRequiredService<ReportAbsenceCommandHandler>(),
            serviceProvider.GetRequiredService<ManagerSettingsMessageHandler>(),
            serviceProvider.GetRequiredService<SettingsCommandHandler>(),
            serviceProvider.GetRequiredService<StateBasedMessageHandler>(),
            serviceProvider.GetRequiredService<AttendanceStatsCommandHandler>(),
            serviceProvider.GetRequiredService<AttendanceDetailsCommandHandler>(),
            serviceProvider.GetRequiredService<EventCommandHandler>(),
            serviceProvider.GetRequiredService<MyEventsCommandHandler>(),
            serviceProvider.GetRequiredService<ExportAttendanceCommandHandler>(),
            serviceProvider.GetRequiredService<ExportDateInputHandler>(),
            serviceProvider.GetRequiredService<WorkTimeInputMessageHandler>()
        };
        _callbackHandlers = new List<ITelegramCallbackQuery>
        {
            serviceProvider.GetRequiredService<LanguageSelectionCallbackHandler>(),
            serviceProvider.GetRequiredService<AbsenceTypeCallbackHandler>(),
            serviceProvider.GetRequiredService<OversleptETACallbackHandler>(),
            serviceProvider.GetRequiredService<OversleptETACallbackHandler>(),
            serviceProvider.GetRequiredService<CancelCallbackHandler>(),
            serviceProvider.GetRequiredService<ExportDateRangeCallbackHandler>(),
            serviceProvider.GetRequiredService<WorkTimeSettingsCallbackHandler>(),
            serviceProvider.GetRequiredService<CalendarCallbackHandler>()
        };

    }
    public Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        return _messageService.SendTextMessageAsync(chatId, text, cancellationToken: cancellationToken);
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
                case UpdateType.MyChatMember:
                    await ProcessMyChatMemberAsync(update.MyChatMember, cancellationToken);
                    break;
                // ChatMember tracking disabled - not needed for simple group notifications

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

    private async Task ProcessChatMemberAsync(ChatMemberUpdated? chatMember, CancellationToken cancellationToken)
    {
        if (chatMember == null || chatMember.Chat.Type == ChatType.Private) return;

        try
        {
            var userId = chatMember.From.Id;
            var chatId = chatMember.Chat.Id;

            var user = await _userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);
            if (user.HasNoValue)
            {
                _logger.LogDebug("User {UserId} not registered in system, skipping membership tracking", userId);
                return;
            }

            var group = await _groupRepository.GetByChatIdAsync(chatId, cancellationToken);
            if (group.HasNoValue)
            {
                _logger.LogDebug("Group {ChatId} not tracked in system, skipping membership update", chatId);
                return;
            }

            var existingMembership = await _groupMembership.GetByUserAndGroupAsync(user.Value.Id, group.Value.Id,cancellationToken);

            if (chatMember.NewChatMember.Status == ChatMemberStatus.Member ||
                chatMember.NewChatMember.Status == ChatMemberStatus.Administrator)
            {
                var isAdmin = chatMember.NewChatMember.Status == ChatMemberStatus.Administrator;
                if (existingMembership.HasNoValue)
                {
                    var membership = new GroupMembership(user.Value.Id, group.Value.Id, isAdmin);
                    _groupMembership.Insert(membership);
                    _logger.LogInformation("User {UserId} joined group {GroupTitle} as {Role}",
                        userId, group.Value.Title, isAdmin ? "admin" : "member");
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    existingMembership.Value.Activate();
                    if(isAdmin) existingMembership.Value.MakeAdmin();
                    else existingMembership.Value.RemoveAdmin();
                    _groupMembership.Update(existingMembership.Value);
                    _logger.LogInformation("User {UserId} status updated in group {GroupTitle} as {Role}",
                        userId, group.Value.Title, isAdmin ? "admin" : "member");
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            else if (chatMember.NewChatMember.Status == ChatMemberStatus.Kicked || 
                     chatMember.NewChatMember.Status == ChatMemberStatus.Left)
            {
                if (existingMembership.HasValue)
                {
                    existingMembership.Value.Deactivate();
                    _groupMembership.Update(existingMembership.Value);
                    _logger.LogInformation("User {UserId} left group {GroupTitle}", userId, group.Value.Title);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat member change for user {UserId} in chat {ChatId}",
                chatMember.From.Id, chatMember.Chat.Id);
        }
        
        
        
        
        
        
        
        
        
        
        
        
        
    }

    private async Task ProcessMyChatMemberAsync(ChatMemberUpdated myChatMember, CancellationToken cancellationToken)
    {
        if(myChatMember.Chat.Type == ChatType.Private) return;
        
        try
        {
            var chatId = myChatMember.Chat.Id;
            var chatTitle = myChatMember.Chat.Title ?? "Unknown group";
            if (myChatMember.NewChatMember.Status == ChatMemberStatus.Member ||
                myChatMember.NewChatMember.Status == ChatMemberStatus.Administrator )
            {
                var existingGroup = await _groupRepository.GetByChatIdAsync(chatId, cancellationToken);
                if (existingGroup.HasNoValue)
                {
                    var group = new TelegramGroup(chatId, chatTitle);
                    _groupRepository.Insert(group);
                    _logger.LogInformation("Bot added to the new group: {GroupTitle} (ID: {ChatId} )",chatTitle,chatId);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    existingGroup.Value.Activate();
                    _groupRepository.Update(existingGroup.Value);
                    _logger.LogInformation("Bot re-added to existing group: {GroupTitle} (ID: {ChatId})",chatTitle,chatId);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            else if(myChatMember.NewChatMember.Status == ChatMemberStatus.Left ||
                    myChatMember.NewChatMember.Status == ChatMemberStatus.Kicked)
            {
                var existingGroup = await _groupRepository.GetByChatIdAsync(chatId, cancellationToken);
                if (existingGroup.HasValue)
                {
                    existingGroup.Value.DeActivate();
                    _groupRepository.Update(existingGroup.Value);
                    _logger.LogInformation("Bot removed from group: {GroupTitle} (ID: {ChatId})", chatTitle, chatId);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bot membership change in chat {ChatId}", myChatMember.Chat.Id);
        }
    }

    private async Task ProcessNewChatMembersAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.NewChatMembers == null) return;
        
        try
        {
            var group = await _groupRepository.GetByChatIdAsync(message.Chat.Id, cancellationToken);
            if (group.HasNoValue)
            {
                _logger.LogDebug("Group {ChatId} not tracked in system, skipping new member processing", message.Chat.Id);
                return;
            }
            
            foreach (var newMember in message.NewChatMembers)
            {
                // Skip if it's the bot itself (already handled by MyChatMember)
                if (newMember.IsBot) continue;
                
                // Check if this user is registered in our system
                var user = await _userRepository.GetByTelegramUserIdAsync(newMember.Id, cancellationToken);
                if (user.HasNoValue)
                {
                    _logger.LogDebug("User {UserId} ({Username}) not registered in system, skipping membership tracking", 
                        newMember.Id, newMember.Username);
                    continue;
                }
                
                // Check if membership already exists
                var existingMembership = await _groupMembership.GetByUserAndGroupAsync(user.Value.Id, group.Value.Id, cancellationToken);
                
                if (existingMembership.HasNoValue)
                {
                    // Create new membership
                    var membership = new GroupMembership(user.Value.Id, group.Value.Id, false);
                    _groupMembership.Insert(membership);
                    _logger.LogInformation("User {UserId} ({FirstName} {LastName}) joined group {GroupTitle}", 
                        newMember.Id, user.Value.FirstName, user.Value.LastName, group.Value.Title);
                }
                else
                {
                    // Reactivate existing membership
                    existingMembership.Value.Activate();
                    _groupMembership.Update(existingMembership.Value);
                    _logger.LogInformation("User {UserId} ({FirstName} {LastName}) rejoined group {GroupTitle}", 
                        newMember.Id, user.Value.FirstName, user.Value.LastName, group.Value.Title);
                }
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing new chat members in group {ChatId}", message.Chat.Id);
        }
    }
    
    private async Task ProcessLeftChatMemberAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.LeftChatMember == null) return;
        
        try
        {
            var leftMember = message.LeftChatMember;
            
            // Skip if it's the bot itself (already handled by MyChatMember)
            if (leftMember.IsBot) return;
            
            var group = await _groupRepository.GetByChatIdAsync(message.Chat.Id, cancellationToken);
            if (group.HasNoValue)
            {
                _logger.LogDebug("Group {ChatId} not tracked in system, skipping left member processing", message.Chat.Id);
                return;
            }
            
            var user = await _userRepository.GetByTelegramUserIdAsync(leftMember.Id, cancellationToken);
            if (user.HasNoValue)
            {
                _logger.LogDebug("User {UserId} not registered in system, skipping membership deactivation", leftMember.Id);
                return;
            }
            
            var existingMembership = await _groupMembership.GetByUserAndGroupAsync(user.Value.Id, group.Value.Id, cancellationToken);
            if (existingMembership.HasValue)
            {
                existingMembership.Value.Deactivate();
                _groupMembership.Update(existingMembership.Value);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("User {UserId} ({FirstName} {LastName}) left group {GroupTitle}", 
                    leftMember.Id, user.Value.FirstName, user.Value.LastName, group.Value.Title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing left chat member in group {ChatId}", message.Chat.Id);
        }
    }

    public async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if(message == null || message.From == null) return;
        

        if (message.Chat.Type != ChatType.Private)
        {
            _logger.LogDebug("Ignoring message type {MessageType} from group chat {ChatId} ({ChatTitle})", 
                message.Type, message.Chat.Id, message.Chat.Title);
            return;
        }
        
        var userId = message.From.Id;
        
        _logger.LogInformation("Processing message for the user {UserId}:{MessageType} ",userId,message.Type);

        switch (message.Type)
        {
            case MessageType.Text:
                await ProcessTextMessageAsync(message, cancellationToken);
                break;
            case MessageType.Contact:
                var contactHandler = _serviceProvider.GetRequiredService<ContactMessageHandler>();
                await contactHandler.HandlerAsync(message, cancellationToken);
                break;
            default:
                var state =await _stateService.GetStateAsync(userId);
                var language = state?.Language ?? await _localization.GetUserLanguage(userId);
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

        var userId = message.From!.Id;
        var state = await _stateService.GetStateAsync(userId);
        var language = state?.Language ?? await _localization.GetUserLanguage(userId);
        
        // Check if user is in a critical state that requires specific action
        if (state?.CurrentAction == UserAction.SelectingLanguage)
        {
            await _messageService.SendTextMessageAsync(
                message.Chat.Id,
                """
                Please select your language from the buttons above.
                Пожалуйста, выберите язык из кнопок выше.
                Iltimos, yuqoridagi tugmalardan tilni tanlang.
                """,
                cancellationToken: cancellationToken);
            return;
        }
        
        if (state?.CurrentAction == UserAction.Registering)
        {
            var step = state.Data.TryGetValue("step", out var s) ? s?.ToString() : "phone";
            if (step == "phone")
            {
                await _messageService.SendTextMessageAsync(
                    message.Chat.Id,
                    _localization.GetString(ResourceKeys.PleaseEnterPhoneNumber, language),
                    cancellationToken: cancellationToken);
            }
            else if (step == "birthday")
            {
                await _messageService.SendTextMessageAsync(
                    message.Chat.Id,
                    _localization.GetString(ResourceKeys.EnterBirthDate, language),
                    cancellationToken: cancellationToken);
            }
            return;
        }
        
        await _messageService.ShowMainMenuAsync(
            message.Chat.Id,
            language,
            cancellationToken:cancellationToken);
    }

    public async Task ProcessCallbackQueryAsync(CallbackQuery? callbackQuery, CancellationToken cancellationToken)
    {
        if(callbackQuery == null || callbackQuery.From == null || callbackQuery.Message == null) return;

        // IGNORE callback queries in groups - bot should only respond in private chats
        if (callbackQuery.Message.Chat.Type != ChatType.Private)
        {
            _logger.LogDebug("Ignoring callback query from group chat {ChatId} ({ChatTitle})", 
                callbackQuery.Message.Chat.Id, callbackQuery.Message.Chat.Title);
            return;
        }

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
        var language = state?.Language ?? await _localization.GetUserLanguage(userId);
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
            var language = state?.Language ?? await _localization.GetUserLanguage(chatId);
            await _messageService.ShowMainMenuAsync(
                chatId, 
                language,
                cancellationToken:cancellationToken);

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send error message to chat {ChatId}", chatId);
        }
    }
}