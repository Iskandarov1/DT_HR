using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Contract.FailureData;
using DT_HR.Contract.SuccessData;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Attendance.Commands.CheckIn;

public class CheckInCommandHandler(
    IUnitOfWork unitOfWork,
    IUserRepository userRepository, 
    IAttendanceRepository attendanceRepository,
    IInputHandler<CheckInCommand> inputHandler,
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
                    DateTime.UtcNow), cancellationToken);

            return Result.Failure<Guid>(validationResult.Error);
        }

        var user = await userRepository.GetByTelegramUserIdAsync(request.TelegramUserId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var attandance = await attendanceRepository.GetByUserAndDateAsync(user.Value.Id, today, cancellationToken);

        var isWithInRadius =
            await checkInCallbacks.ValidateLocationAsync(request.Latitude, request.Longitude, cancellationToken);
        
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
                        DateTime.UtcNow),cancellationToken);

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
                    DateTime.UtcNow),cancellationToken);
            throw;
        }
    }
}