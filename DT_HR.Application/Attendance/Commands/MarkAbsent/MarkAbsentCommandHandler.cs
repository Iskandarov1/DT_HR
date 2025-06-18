using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Contract.CallbackData.Attendance;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Enumeration;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Attendance.Commands.MarkAbsent;

public class MarkAbsentCommandHandler(
    IUnitOfWork unitOfWork,
    IAttendanceRepository attendanceRepository,
    IUserRepository userRepository,
    IInputHandler<MarkAbsentCommand> inputHandler,
    IMarkAbsentCallbacks callbacks) : ICommandHandler<MarkAbsentCommand,Result<Guid>>
{
    public async Task<Result<Guid>> Handle(MarkAbsentCommand request, CancellationToken cancellationToken)
    {
        // 1. neeed to validate the INPUT
        var validateResult = await inputHandler.ValidateAsync(request, cancellationToken);

        if (validateResult.IsFailure)
        {
            await callbacks.OnMarkAbsentFailureAsync(
                new MarkAbsentFailureData(
                    request.TelegramUserId,
                    validateResult.Error.Code,
                    validateResult.Error.Message,
                    DateTime.UtcNow
                ), cancellationToken);
            return Result.Failure<Guid>(validateResult.Error);
        }

        var user = await userRepository.GetByTelegramUserIdAsync(request.TelegramUserId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var attendance =
            await attendanceRepository.GetByUserAndDateAsync(user.Value.Id, today, cancellationToken);

        try
        {
            if (attendance.HasNoValue)
            {
                attendance = new Domain.Entities.Attendance(user.Value.Id, today);
                attendanceRepository.Insert(attendance.Value);
            }

            if (attendance.Value.CheckInTime.HasValue)
            {
                await callbacks.OnMarkAbsentFailureAsync(
                    new MarkAbsentFailureData(
                        request.TelegramUserId,
                        "Already_Cheked_In",
                        "You have already checked in today, Cannot mark as absent.",
                        DateTime.UtcNow
                    ),cancellationToken);

                return Result.Failure<Guid>(new Error("Attendance.AlreadyCheckedIn",
                    "Attendance already checked in, cannot mark as absent"));
            }

            if (attendance.Value.Status == AttendanceStatus.Absent.Value ||
                attendance.Value.Status == AttendanceStatus.OnTheWay.Value)
            {
                await callbacks.OnMarkAbsentFailureAsync(
                    new MarkAbsentFailureData(
                        request.TelegramUserId,
                        "Already_Marked_Absent",
                        "You have already reported your absence today",
                        DateTime.UtcNow), cancellationToken);

                return Result.Failure<Guid>(new Error("Attendance.AlreadyMarkedAbsent",
                    "Absence already reported today"));
            }

            attendance.Value.MarkAbsent(request.Reason, request.EstimatedArrivalTime);

            unitOfWork.SaveChangesAsync(cancellationToken);

            await callbacks.OnAbsenceMarkedAsync(
                new AbsenceMarkedData(
                    request.TelegramUserId,
                    $"{user.Value.FirstName} {user.Value.LastName}",
                    request.Reason,
                    request.AbsenceType,
                    request.EstimatedArrivalTime,
                    DateTime.UtcNow
                    ), cancellationToken);
            
            return Result.Success(attendance.Value.Id);
            
        }
        catch (Exception e)
        {
            await callbacks.OnMarkAbsentFailureAsync(
                new MarkAbsentFailureData(
                    request.TelegramUserId,
                    "System_Error",
                    "A system error occured while marking the absence",
                    DateTime.UtcNow
                ), cancellationToken);
            throw;
        }
        
    }
}