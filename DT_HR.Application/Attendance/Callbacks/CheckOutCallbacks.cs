using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Contract.CallbackData.Attendance;
using Microsoft.Extensions.Logging;

namespace DT_HR.Application.Attendance.Callbacks;

public class CheckOutCallbacks(
    ITelegramBotService botService,
    ILogger<CheckOutCallbacks> logger,
    IUserStateService stateService,
    ILocalizationService localization,
    IUnitOfWork unitOfWork) : ICheckOutCallbacks
{
    public async Task OnCheckOutSuccessAsync(CheckOutSuccessData data, CancellationToken cancellationToken)
    {
        try
        {
            var state = await stateService.GetStateAsync(data.TelegramUserId);
            var language = state?.Language ?? await localization.GetUserLanguage(data.TelegramUserId);
            var message = BuildSuccessMessage(data,language);
            await botService.SendTextMessageAsync(data.TelegramUserId, message, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
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
            var state = await stateService.GetStateAsync(data.TelegramUserId);
            var language = state?.Language ?? await localization.GetUserLanguage(data.TelegramUserId);

            var checkOutFailed =  localization.GetString(ResourceKeys.CheckOutFailed, language);
            var tryAgain = localization.GetString(ResourceKeys.TryAgain, language); 
            
            var message = $"{checkOutFailed}: {data.ErrorMessage} \n {tryAgain}";
            await botService.SendTextMessageAsync(data.TelegramUserId, message, cancellationToken);

        }
        catch (Exception e)
        {
            logger.LogError(e,"Error sending check out failure information to the user {TelegramUserId}", data.TelegramUserId);
        }
    }

    private string BuildSuccessMessage(CheckOutSuccessData data, string language)
    {
        var checkOutSuccessful = localization.GetString(ResourceKeys.CheckOutSuccessful, language);
        var status = localization.GetString(ResourceKeys.Status, language);
        var goodbye = localization.GetString(ResourceKeys.Goodbye, language);
        var haveGoodDay = localization.GetString(ResourceKeys.HaveAGreatDay, language);
        
        
        
        var departureStatus = data.IsEarlyDeparture
            ? $"{localization.GetString(ResourceKeys.EarlyDeparture,language)}" +
              $" ({data.EarlyBy?.ToString(@"hh\:mm")} {localization.GetString(ResourceKeys.Early,language)})"
            : localization.GetString(ResourceKeys.RegularDeparture,language);

        var workDurationText = data.WorkDuration.HasValue
            ? $"{localization.GetString(ResourceKeys.WorkDuration,language)}: {data.WorkDuration.Value:hh\\:mm}"
            : localization.GetString(ResourceKeys.WorkDurationNotAvailable,language);

        return $"""
                 {checkOutSuccessful}

                 👤 {goodbye}, {data.UserName}!
                 ⏱️  {status}: {departureStatus}
                 🕐 {workDurationText}

                 {haveGoodDay}
                 """;
        
    } 
}