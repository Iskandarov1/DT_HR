using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Contract.CallbackData.Attendance;
using DT_HR.Domain.Enumeration;
using Microsoft.Extensions.Logging;

namespace DT_HR.Application.Attendance.Commands.MarkAbsent;

public class MarkAbsentCallbacks(
    ITelegramBotService telegramBotService,
    IBackgroundTaskService backgroundTaskService,
    ILogger<MarkAbsentCallbacks> logger) : IMarkAbsentCallbacks
{
    
    public async Task OnAbsenceMarkedAsync(AbsenceMarkedData data, CancellationToken cancellationToken)
    {
        try
        {
            var message = BuildAbsenceConfirmationMessage(data);
            await telegramBotService.SendTextMessageAsync(data.TelegramUserId, message, cancellationToken);
            
            logger.LogInformation("Absence marked for user {TelegramUserId},type: {AbsenceType}",
                data.TelegramUserId, data.AbsenceType);
        }
        catch (Exception e)
        {
                logger.LogInformation("Error sending absence confirmation to user {TelegramUserId} ",
                    data.TelegramUserId);
        }
    }

    public async Task OnEmployeeOnTheWayAsync(OnTheWayData data, CancellationToken cancellationToken)
    {
        try
        {
            var followUpTime = data.EstimatedArrivalTime.AddMinutes(1);

            await backgroundTaskService.ScheduleTaskAsync(
                taskType: "Check Arrival",
                scheduledFor : followUpTime,
                payload: new {data.TelegramUserId, data.EstimatedArrivalTime}
                ,cancellationToken);
    
            var message = $"""
                           🚗 **Got it!** You're on your way.

                           📅 Expected arrival: {data.EstimatedArrivalTime:HH:mm}
                           💬 Reason: {data.AbsenceReason}

                           We'll check with you around {data.EstimatedArrivalTime:HH:mm} to confirm your arrival.
                           Safe travels! 🚗💨
                           """;
            await telegramBotService.SendTextMessageAsync(data.TelegramUserId, message, cancellationToken);
            
            logger.LogInformation("Follow up scheduled for user {TelegramUserId} at {followUpTime}",
                data.TelegramUserId,followUpTime);
        }   
        catch (Exception e)
        {
                logger.LogError(e,"Error handling 'on the way' for the user {TelegramUserId}",data.TelegramUserId);
        }
    }
    

    public async Task OnMarkAbsentFailureAsync(MarkAbsentFailureData data, CancellationToken cancellationToken)
    {
        try
        {
            var message = $"**Unable to mark absence** \n\n{data.ErrorMessage}\n\n Please try again or contact support";
            await telegramBotService.SendTextMessageAsync(data.TelegramUserId, message, cancellationToken);
            logger.LogWarning("Mark absent failed for user {TelegramUserId}: {ErrorCode}", 
                data.TelegramUserId, data.ErrorCode);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error sending mark absent failure notification to user {TelegramUserId}", 
                data.TelegramUserId);
        }
    }
    
    

    public static string BuildAbsenceConfirmationMessage(AbsenceMarkedData data)
    {
        var marked = data.MarkedAt;
        var eta = data.EstimatedArrivalTime;

        return data.AbsenceType switch
        {
            0 => $"""
                  **Absence Recorded**

                      Date: {marked:MMM dd, yyyy}
                      Reason: {data.AbsenceReason}
                      Marked at: {marked:HH:mm}
                              
                  Hope you feel better soon! 
                  """,
            1 => $"""
                  🚗 **"On the way" status recorded**
                                 
                     📅 Date: {marked:MMM dd, yyyy}
                     💬 Reason: {data.AbsenceReason}
                     ⏰ Expected: {eta:HH:mm}
                     
                     See you soon! 🚀
                  """,
            2 => $""""
                  😴 **Overslept - No worries!**

                  📅 Date: {marked:MMM dd, yyyy}
                  ⏰ Expected: {eta:HH:mm}

                  Take your time and come when ready! ☕️

                  """",
            3 => data.EstimatedArrivalTime.HasValue
                ? $"""
                   ✅ **Custom absence recorded**

                   📅 Date: {marked:MMM dd, yyyy}
                   💬 Reason: {data.AbsenceReason}
                   ⏰ Expected: {eta:HH:mm}

                   Thanks for letting us know! 👍
                   """
                : $"""
                   ✅ **Absence recorded**

                   📅 Date: {marked:MMM dd, yyyy}
                   💬 Reason: {data.AbsenceReason}

                   Thanks for letting us know! 👍
                   """,

            _ => "✅ Absence recorded successfully."
        };
    }
    
    
}