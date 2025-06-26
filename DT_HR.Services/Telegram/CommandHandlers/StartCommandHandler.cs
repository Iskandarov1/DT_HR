using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Services.Telegram.CommandHandlers;

public class StartCommandHandler(
    IUserRepository userRepository,
    ITelegramMessageService messageService,
    ITelegramKeyboardService keyboardService,
    IUserStateService stateService,
    ILocalizationService localizationService,
    ILogger<StartCommandHandler> logger) : ITelegramService
{
    public Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return Task.FromResult(false);
        return Task.FromResult(message.Text.Equals("/start", StringComparison.OrdinalIgnoreCase));
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        
        logger.LogInformation("Processing start command for the user {ChatId}",chatId);

        var user = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);

        if (user.HasNoValue)
        {
            await stateService.SetStateAsync(userId, new UserState { CurrentAction = UserAction.SelectingLanguage });

            var keyboard = keyboardService.GetLanguageSelectionKeyboard();
            
            await messageService.SendTextMessageAsync(
                chatId,
                """
                Welcome to DT HR Attendance System! 👋
                Добро пожаловать в систему учета посещаемости DT HR! 👋
                DT HR Davomat Tizimiga xush kelibsiz! 👋
                
                Please select your preferred language:
                Пожалуйста, выберите предпочитаемый язык:
                Iltimos, o'zingizga qulay tilni tanlang:
                """,
                keyboard,
                cancellationToken);
        }
        else
        {
            var state = await stateService.GetStateAsync(userId);
            var language = state?.Language ?? user.Value.Language;

            var welcomeBack = localizationService.GetString(ResourceKeys.WelcomeBack, language, user.Value.FirstName);
            
            await messageService.SendTextMessageAsync(
                chatId,
                welcomeBack,
                cancellationToken:cancellationToken);
            var whatWouldYouLikeToDo =
                localizationService.GetString(ResourceKeys.WhatWouldYouLikeToDo, language);
            await messageService.ShowMainMenuAsync(chatId, whatWouldYouLikeToDo, language,cancellationToken);
        }
    }
}