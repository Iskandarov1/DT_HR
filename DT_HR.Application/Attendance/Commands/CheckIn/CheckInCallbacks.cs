using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Contract.FailureData;
using DT_HR.Contract.SuccessData;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace DT_HR.Application.Attendance.Commands.CheckIn;

public class CheckInCallbacks(
    ILocationService locationService,
    ITelegramBotService telegramBotService,
    ILogger<CheckInCallbacks> logger) : ICheckInCallbacks
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
            var message = BuildSuccesMessage(data);
            await telegramBotService.SendTextMessageAsync(data.TelegramUserId, message, cancellationToken);
            
            logger.LogInformation("Check-in success notification sent to the user {TelegramUserId}",data.TelegramUserId);
        }
        catch (Exception e)
        {
           logger.LogInformation("Error sending Check-in success notification to user {TelegramUserId}", data.TelegramUserId);
        }
    }

    public Task OnCheckInFailureAsync(CheckInFailureDate date, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private static string BuildSuccesMessage(CheckInSuccessData data)
    {
        var locationStatus = data.IsWithInOfficeRadius ? "Office location verified" : "Outside Office radius";
        var timeStatus = data.IsLateArrival
            ? $"Late Arrival ({data.LateBy?.ToString(@"hh\:mm")} late)"
            : "On time";
        
        return $"""
                ✅**Check-in Successful!**
                
                👤Welcome, {data.UserName}!
                📍Location: {locationStatus}
                ⏱️ Status : {timeStatus}
                
                Have a Productive Day! 🚀
                """;
    }
    
    
    
    
    
    
    
    
    
    
    
}