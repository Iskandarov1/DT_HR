using DT_HR.Application.Attendance.Commands.MarkAbsent;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Attendance.InputHandler;

public class MarkAbsentInputHandler(
    IUserRepository userRepository,
    IAttendanceRepository attendanceRepository
    ) : IInputHandler<MarkAbsentCommand>
{
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(5);
    public async Task<Result> ValidateAsync(MarkAbsentCommand command, CancellationToken cancellationToken = default)
    {
        if (command.TelegramUserId <= 0)
            return Result.Failure(DomainErrors.TelegramUserId.Invalid);
         
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Result.Failure(DomainErrors.Attendance.ResonRequired);
        
        var user = await userRepository.GetByTelegramUserIdAsync(command.TelegramUserId, cancellationToken);
        if(user.HasNoValue)
            return Result.Failure(DomainErrors.User.NotFound);
        
        if(!user.Value.IsActive)
            return Result.Failure(DomainErrors.User.NotActive);
        
        if (command.EstimatedArrivalTime.HasValue)
        {
            var eta = command.EstimatedArrivalTime.Value;
            
            var now = TimeUtils.Now;
            var maxArrivalTime = now.AddHours(15);

            // if (eta < now)
            // {
            //     return Result.Failure(DomainErrors.Attendance.InvalidEstimatedArivalTime);
            // }
            //
            // if (eta > maxArrivalTime)
            // {
            //     return Result.Failure(DomainErrors.Attendance.EstimatedArrivalTooFar);
            // }
        }

        var validationResult = ValidateAbsenceType(command);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }
        
        var today = DateOnly.FromDateTime(TimeUtils.Now);
        var existingAttendance =
            await attendanceRepository.GetByUserAndDateAsync(user.Value.Id, today, cancellationToken);
        
        if(existingAttendance.HasValue && existingAttendance.Value.CheckInTime.HasValue)
            return Result.Failure(DomainErrors.Attendance.AlreadyChekedIn);
        
        return Result.Success();
    }

    public static Result ValidateAbsenceType(MarkAbsentCommand command)
    {
        return command.AbsenceType.Value switch
        {
            1 => command.EstimatedArrivalTime.HasValue
                ? Result.Success()
                : Result.Failure(DomainErrors.Attendance.ETARequiredForOnTheWay),

            2 => Result.Success(),
            0 => command.EstimatedArrivalTime.HasValue
                ? Result.Failure(DomainErrors.Attendance.ETANotAllowedForAbsent)
                : Result.Success(),
            
            3 => Result.Success(),
            _ => Result.Failure(DomainErrors.Attendance.InvalidAbsenceType)
        };
    }

    
}