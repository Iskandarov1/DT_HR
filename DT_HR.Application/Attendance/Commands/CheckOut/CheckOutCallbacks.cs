using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Contract.CallbackData.Attendance;
using Microsoft.Extensions.Logging;

namespace DT_HR.Application.Attendance.Commands.CheckOut;

public class CheckOutCallbacks(
    ITelegramBotService botService,
    ILogger<CheckOutCallbacks> logger,
    IUnitOfWork unitOfWork) : ICheckOutCallbacks
{
    public async Task OnCheckOutSuccessAsync(CheckOutSuccessData data, CancellationToken cancellationToken)
    {
        try
        {
            var message = BuildSuccessMessage(data);
            await botService.SendTextMessageAsync(data.TelegramUserId, message, cancellationToken);
            unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Check out successfull message sent to the user {TelegramUserId}",data.TelegramUserId);

        }
        catch (Exception e)
        {
            logger.LogError(e,"Error sending check out successful info to the user {TelegramUserId} ", data.TelegramUserId);
        }
    }
    

    public async Task OnCheckOutFailureAsync(CheckOutFailureData data, CancellationToken cancellationToken)
    {
        try
        {
            var message = $"Check out failed: {data.ErrorMessage} \n please try again or contact support";
            await botService.SendTextMessageAsync(data.TelegramUserId, message, cancellationToken);

        }
        catch (Exception e)
        {
            logger.LogError(e,"Error sending check out failure information to the user {TelegramUserId}", data.TelegramUserId);
        }
    }

    private static string BuildSuccessMessage(CheckOutSuccessData data)
    {
        var departureStatus = data.IsEarlyDeparture
            ? $"Early Departure ({data.EarlyBy?.ToString(@"hh\:mm")} early)"
            : "Regular departure";

        var workDurationText = data.WorkDuration.HasValue
            ? $"Work duration: {data.WorkDuration.Value:hh\\:mm}"
            : "Work duration: not available";

        return $$"""
                 ✅ **Check-out Successful!**

                 👤 Goodbye, {{data.UserName}}!
                 ⏱️  Status: {{departureStatus}}
                 🕐 {{workDurationText}}

                 Have a great day! 🌟
                 """;
        
    } 
}