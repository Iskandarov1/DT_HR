using System.Text;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace DT_HR.Infrastructure.Telegram.CommandHandlers;

public class AttendanceDetailsCommandHandler(
    ITelegramMessageService messageService,
    IUserStateService stateService,
    ILocalizationService localization,
    IUserRepository repository,
    IAttendanceReportService reportService, 
    ILogger<AttendanceDetailsCommandHandler> logger) : ITelegramService
{
    public async Task<bool> CanHandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Text == null) return false;
        var state = await stateService.GetStateAsync(message.From!.Id);
        var language = state?.Language ?? await localization.GetUserLanguage(message.From!.Id);
        var text = message.Text.ToLower();
        var startsText = localization.GetString(ResourceKeys.AttendanceDetails, language).ToLower();

        return text == "/details" || text == startsText;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var state = await stateService.GetStateAsync(userId);
        var language = state?.Language ?? await localization.GetUserLanguage(userId);

        var user = await repository.GetByTelegramUserIdAsync(userId, cancellationToken);

        if (user.HasNoValue || !user.Value.IsManager())
        {
            await messageService.SendTextMessageAsync(chatId,
                localization.GetString(ResourceKeys.OptionNotAvailable, language),
                cancellationToken: cancellationToken);
            return;
        }

        var list = await reportService.GetDetailedAttendance(DateOnly.FromDateTime(TimeUtils.Now), cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine($"*{TimeUtils.Now:yyyy-MM-dd}*\n");

        foreach (var item in list)
        {
            var checkIn = item.CheckInTime?.ToString("HH:mm") ?? "-";
            var checkOut = item.CheckOutTime?.ToString("HH:mm") ?? "-";
            var late = item.IsLate == true ? "(late)" : string.Empty;
            sb.AppendLine($"{item.Name} - {item.Status} {late} - in:{checkIn} out:{checkOut}");
        }

        await messageService.SendTextMessageAsync(
            chatId,
            sb.ToString(),
            cancellationToken: cancellationToken);
        await messageService.ShowMainMenuAsync(
            chatId, 
            localization.GetString(ResourceKeys.PleaseSelectFromMenu,language),
            language, 
            isManager:true,
            cancellationToken:cancellationToken);

    }
}