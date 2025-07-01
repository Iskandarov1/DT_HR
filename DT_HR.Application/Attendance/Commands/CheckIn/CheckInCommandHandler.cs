using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Contract.CallbackData.Attendance;
using DT_HR.Domain.Core;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Attendance.Commands.CheckIn;

public class CheckInCommandHandler(
    IUnitOfWork unitOfWork,
    IUserRepository userRepository, 
    IAttendanceRepository attendanceRepository,
    IInputHandler<CheckInCommand> inputHandler,
    ILocalizationService localizationService,
    ICheckInCallbacks checkInCallbacks) : ICommandHandler<CheckInCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await inputHandler.ValidateAsync(request, cancellationToken);

        if (validationResult.IsFailure)
        {
            await checkInCallbacks.OnCheckInFailureAsync(
                new CheckInFailureDate(
                    request.TelegramUserId,
                    validationResult.Error.Code,
                    validationResult.Error.Message,
                    TimeUtils.Now), cancellationToken);

            return Result.Failure<Guid>(validationResult.Error);
        }

        var user = await userRepository.GetByTelegramUserIdAsync(request.TelegramUserId, cancellationToken);

        var today = DateOnly.FromDateTime(TimeUtils.Now);

        var attandance = await attendanceRepository.GetByUserAndDateAsync(user.Value.Id, today, cancellationToken);

        var isWithInRadius =
            await checkInCallbacks.ValidateLocationAsync(request.Latitude, request.Longitude, cancellationToken);

        if (!isWithInRadius)
        {
            var lang = await localizationService.GetUserLanguage(request.TelegramUserId);
            await checkInCallbacks.OnCheckInFailureAsync(new CheckInFailureDate(
                request.TelegramUserId,
                "invalid_location",
                localizationService.GetString(ResourceKeys.OutsideOfficeRadius, lang),
                TimeUtils.Now),cancellationToken);

            return Result.Failure<Guid>(new Error("invalid_location", "Outside office radius"));
        }
        
        try
        {
            if (attandance.HasNoValue)
            {
                attandance = new Domain.Entities.Attendance(user.Value.Id, today);
                attendanceRepository.Insert(attandance.Value);
            }

            if (attandance.Value.CheckInTime.HasValue)
            {
                await checkInCallbacks.OnCheckInFailureAsync(
                    new CheckInFailureDate(
                        request.TelegramUserId,
                        "Already_Checked_IN",
                        "You have already checked in today",
                        TimeUtils.Now),cancellationToken);

                return Result.Failure<Guid>(DomainErrors.Attendance.AlreadyChekedIn);
            }

            attandance.Value.CheckIn(request.Latitude, request.Longitude, isWithInRadius);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var checkInTime = attandance.Value.CheckInTime!.Value;
            var isLate = attandance.Value.IsLateArrival(user.Value.WorkStartTime);
            TimeSpan? lateBy = isLate ? TimeOnly.FromDateTime(checkInTime) - user.Value.WorkStartTime : null;

            await checkInCallbacks.OnCheckInSuccessAsync(
                new CheckInSuccessData(
                request.TelegramUserId,
                $"{user.Value.FirstName} {user.Value.LastName}",
                checkInTime,
                isWithInRadius,
                isLate,
                lateBy)
                ,cancellationToken);

            return Result.Success(attandance.Value.Id);
        }
        
        catch (Exception ex)
        {
            await checkInCallbacks.OnCheckInFailureAsync(
                new CheckInFailureDate(
                    request.TelegramUserId,
                    "System_Error",
                    "A System error occured during check in",
                    TimeUtils.Now),cancellationToken);
            throw;
        }
    }
}