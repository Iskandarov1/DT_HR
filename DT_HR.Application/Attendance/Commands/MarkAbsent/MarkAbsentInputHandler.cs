using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Enumeration;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Attendance.Commands.MarkAbsent;

public class MarkAbsentInputHandler(
    IUserRepository userRepository,
    IAttendanceRepository attendanceRepository
    ) : IInputHandler<MarkAbsentCommand>
{
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

        if (command.EstimatedArrivalTime.HasValue && command.EstimatedArrivalTime.Value <= DateTime.UtcNow)
            return Result.Failure(DomainErrors.Attendance.InvalidEstimatedArivalTime);
        
        
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingAttendance =
            await attendanceRepository.GetByUserAndDateAsync(user.Value.Id, today, cancellationToken);
        
        if(existingAttendance.HasValue && existingAttendance.Value.CheckInTime.HasValue)
            return Result.Failure(DomainErrors.Attendance.AlreadyChekedIn);
        
        return Result.Success();
    }

    
}