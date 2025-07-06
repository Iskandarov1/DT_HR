using DT_HR.Application.Attendance.Commands.CheckIn;
using DT_HR.Application.Core.Abstractions.Enum;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Contract.Responses.MiniApp;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DT_HR.Application.MiniApp.Commands.VerifyLocationAndCheckIn;

public sealed class VerifyLocationAndCheckInCommandHandler(
    IUserRepository userRepository,
    ILocationService locationService,
    IMediator mediator,
    ITelegramMessageService telegramMessageService,
    ILocalizationService localizationService,
    ILogger<VerifyLocationAndCheckInCommandHandler> logger)
    : ICommandHandler<VerifyLocationAndCheckInCommand, Result<MiniAppCheckInResponse>>
{
    public async Task<Result<MiniAppCheckInResponse>> Handle(
        VerifyLocationAndCheckInCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate coordinates
            if (!IsValidCoordinates(request.Latitude, request.Longitude))
            {
                logger.LogWarning("Invalid coordinates provided: {Lat}, {Lon}", request.Latitude, request.Longitude);
                return Result.Success(new MiniAppCheckInResponse(
                    Success: false,
                    Error: GetLocalizedMessage("InvalidCoordinates", request.LanguageCode)
                ));
            }

            // Get user from database
            var user = await userRepository.GetByTelegramUserIdAsync(request.TelegramUserId, cancellationToken);
            if (!user.HasValue)
            {
                logger.LogWarning("User {UserId} not found for Mini App check-in", request.TelegramUserId);
                return Result.Success(new MiniAppCheckInResponse(
                    Success: false,
                    Error: GetLocalizedMessage("UserNotFound", request.LanguageCode)
                ));
            }

            // Check if location is within office radius
            var isWithinOfficeRadius = await locationService.IsWithInOfficeRadiusAsync(
                request.Latitude, 
                request.Longitude);

            if (!isWithinOfficeRadius)
            {
                logger.LogWarning("User {UserId} attempted Mini App check-in from outside office radius: {Lat}, {Lon}", 
                    request.TelegramUserId, request.Latitude, request.Longitude);
                return Result.Success(new MiniAppCheckInResponse(
                    Success: false,
                    Message: GetLocalizedMessage("OutsideOffice", request.LanguageCode)
                ));
            }

            // Location is valid - proceed with check-in using existing command
            var checkInCommand = new CheckInCommand(
                request.TelegramUserId, 
                request.Latitude, 
                request.Longitude);
            
            var result = await mediator.Send(checkInCommand, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Successful Mini App check-in for user {UserId}", request.TelegramUserId);
                
                // Update Telegram main menu to show checked-in status
                var language = await localizationService.GetUserLanguage(request.TelegramUserId);
                await telegramMessageService.ShowMainMenuAsync(
                    request.TelegramUserId,
                    language,
                    menuType: MainMenuType.CheckedIn,
                    cancellationToken: cancellationToken);
                
                return Result.Success(new MiniAppCheckInResponse(
                    Success: true,
                    Message: GetLocalizedMessage("CheckInSuccess", request.LanguageCode)
                ));
            }
            else
            {
                logger.LogWarning("Check-in failed for user {UserId}: {Error}", request.TelegramUserId, result.Error.Message);
                return Result.Success(new MiniAppCheckInResponse(
                    Success: false,
                    Message: result.Error.Message
                ));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Mini App check-in request for user {UserId}", request.TelegramUserId);
            return Result.Failure<MiniAppCheckInResponse>(DomainErrors.General.UnProcessableRequest);
        }
    }

    private static bool IsValidCoordinates(double latitude, double longitude)
    {
        return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
    }

    private static string GetLocalizedMessage(string key, string language)
    {
        return key switch
        {
            "UserNotFound" => language switch
            {
                "ru" => "Пользователь не найден",
                "uz" => "Foydalanuvchi topilmadi",
                _ => "User not found"
            },
            "InvalidCoordinates" => language switch
            {
                "ru" => "Неверные координаты",
                "uz" => "Noto'g'ri koordinatalar",
                _ => "Invalid coordinates"
            },
            "OutsideOffice" => language switch
            {
                "ru" => "Вы находитесь не в офисе",
                "uz" => "Siz ofis joylashuvida emassiz",
                _ => "You are not at the office location"
            },
            "CheckInSuccess" => language switch
            {
                "ru" => "Успешно зарегистрированы!",
                "uz" => "Muvaffaqiyatli ro'yxatdan o'tdingiz!",
                _ => "Successfully checked in!"
            },
            _ => "Unknown error"
        };
    }
}