using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Contract.CallbackData.Attendance;
using Microsoft.Extensions.Logging;

namespace DT_HR.Application.Attendance.Callbacks;

public class CheckInCallbacks(
    ILocationService locationService,
    ITelegramBotService telegramBotService,
    ILocalizationService localization,
    IUserStateService stateService,
    ILogger<CheckInCallbacks> logger,
    IUnitOfWork unitOfWork) : ICheckInCallbacks
{
    public async Task<bool> ValidateLocationAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        try
        {
            return await locationService.IsWithInOfficeRadiusAsync(latitude, longitude);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,"Error validating location for latitude: {latitude}, longitude: {longitude}",latitude,longitude);
            return true;
        }
    }

    public async Task OnCheckInSuccessAsync(CheckInSuccessData data, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await stateService.GetStateAsync(data.TelegramUserId);
            var language = state?.Language ?? await localization.GetUserLanguage(data.TelegramUserId);
            var message = BuildSuccesMessage(data,language);    
            await telegramBotService.SendTextMessageAsync(data.TelegramUserId, message, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Check-in success notification sent to the user {TelegramUserId}",data.TelegramUserId);
        }
        catch (Exception e)
        {
           logger.LogInformation("Error sending Check-in success notification to user {TelegramUserId}", data.TelegramUserId);
        }
    }

    public async Task OnCheckInFailureAsync(CheckInFailureDate data, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await stateService.GetStateAsync(data.TelegramUserId);
            var language = state?.Language ?? await localization.GetUserLanguage(data.TelegramUserId);
            var checkInFailPrompt = localization.GetString(ResourceKeys.CheckInFailedCall, language);
            var tryAgainPrompt = localization.GetString(ResourceKeys.TryAgain, language);
            var message = $"{checkInFailPrompt}: {data.ErrorMessage}\n {tryAgainPrompt}";
            await telegramBotService.SendTextMessageAsync(data.TelegramUserId, message, cancellationToken);
            
            logger.LogWarning("Check in failed for user {TelegramUserId} : {ErrorCode}", data.TelegramUserId,data.ErrorCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending a Check in failure notification to user {TelegramUserID}",data.TelegramUserId);
        }
    }

    private string BuildSuccesMessage(CheckInSuccessData data ,string language)
    {
        var checkInSuccessful = localization.GetString(ResourceKeys.CheckInSuccessful, language);
        var welcome = localization.GetString(ResourceKeys.Welcome, language);
        
        var locationStatus = data.IsWithInOfficeRadius 
            ? localization.GetString(ResourceKeys.OfficeLocationVerified,language) 
            : localization.GetString(ResourceKeys.OutsideOfficeRadius,language);
        
        var timeStatus = data.IsLateArrival
            ? $"{localization.GetString(ResourceKeys.LateArrival,language)} " +
              $"({data.LateBy?.ToString(@"hh\:mm")} {localization.GetString(ResourceKeys.Late,language)})"
            : localization.GetString(ResourceKeys.OnTime,language);
        
        var status = localization.GetString(ResourceKeys.Status, language);
        var haveProductiveDay = localization.GetString(ResourceKeys.HaveAProductiveDay, language);
        
        return $""" 
                {checkInSuccessful}
                
                👤{welcome}, {data.UserName}!
                📍{locationStatus}: {locationStatus}
                ⏱️ {status} : {timeStatus}
                
                {haveProductiveDay}
                """;
    }

    
}