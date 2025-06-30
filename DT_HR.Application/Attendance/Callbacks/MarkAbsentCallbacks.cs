using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Contract.CallbackData.Attendance;
using Microsoft.Extensions.Logging;

namespace DT_HR.Application.Attendance.Callbacks;

public class MarkAbsentCallbacks(
    ITelegramBotService telegramBotService,
    IUserStateService stateService,
    ILocalizationService localization,
    IBackgroundTaskService backgroundTaskService,
    ILogger<MarkAbsentCallbacks> logger) : IMarkAbsentCallbacks
{
    
    public async Task OnAbsenceMarkedAsync(AbsenceMarkedData data, CancellationToken cancellationToken)
    {
        try
        {
            var state = await stateService.GetStateAsync(data.TelegramUserId);
            var language = state?.Language ?? await localization.GetUserLanguage(data.TelegramUserId);
            var message = BuildAbsenceConfirmationMessage(data,language);
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

            await backgroundTaskService.ScheduleArrivalCheckAsync(
                data.TelegramUserId,
                followUpTime,
                cancellationToken);
    
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
            var state = await stateService.GetStateAsync(data.TelegramUserId);
            var language = state?.Language ?? await localization.GetUserLanguage(data.TelegramUserId);
            var message = $"{localization.GetString(ResourceKeys.ErrorOccurred,language)} \n\n{data.ErrorMessage}\n\n ";
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
    
    

    public string BuildAbsenceConfirmationMessage(AbsenceMarkedData data, string language)
    {

        var marked = data.MarkedAt;
        var eta = data.EstimatedArrivalTime;
        var absenceRecorded = localization.GetString(ResourceKeys.AbsenceRecorded, language);
        var date = localization.GetString(ResourceKeys.Date, language);
        var reason = localization.GetString(ResourceKeys.Reason, language);
        var markedLan = localization.GetString(ResourceKeys.Marked, language);
        var expected = localization.GetString(ResourceKeys.Expected, language);
        var thank = localization.GetString(ResourceKeys.Thanks, language);



        return data.AbsenceType switch
        {
            0 => $"""
                  {absenceRecorded}

                     📅 {date}: {marked:MMM dd, yyyy}
                     💬 {reason}: {data.AbsenceReason}
                     ⏰ {markedLan}: {marked:HH:mm}
                              
                  {thank} 👍
                  """,
            1 => $"""
                  🚗 {absenceRecorded}
                                 
                     📅 {date}: {marked:MMM dd, yyyy}
                     💬 {reason}: {data.AbsenceReason}
                     ⏰ {expected}: {eta:HH:mm}
                     
                     {thank} 👍
                  """,
            2 => $""""
                  😴 {absenceRecorded}

                  📅 {date}: {marked:MMM dd, yyyy}
                  ⏰ {expected}: {eta:HH:mm}

                  {thank} 👍

                  """",
            3 => data.EstimatedArrivalTime.HasValue
                ? $"""
                   ✅ {absenceRecorded}

                   📅 {date}: {marked:MMM dd, yyyy}
                   💬 {reason}: {data.AbsenceReason}
                   ⏰ {expected}: {eta:HH:mm}

                   {thank} 👍
                   """
                : $"""
                   ✅ {absenceRecorded}

                   📅 {date}: {marked:MMM dd, yyyy}
                   💬 {reason}: {data.AbsenceReason}

                   {thank} 👍
                   """,

            _ => $"✅ {absenceRecorded}."
        };
    }
    
    
}