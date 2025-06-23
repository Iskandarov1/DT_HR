using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Contract.CallbackData.Attendance;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;


namespace DT_HR.Application.Attendance.Commands.CheckOut;

public class CheckOutCommandHandler(
    IUserRepository userRepository,
    IAttendanceRepository attendanceRepository,
    IInputHandler<CheckOutCommand> inputHandler,
    IUnitOfWork unitOfWork,
    ICheckOutCallbacks callbacks) : ICommandHandler<CheckOutCommand, Result<Guid>> 
{
    public async Task<Result<Guid>> Handle(CheckOutCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await inputHandler.ValidateAsync(request, cancellationToken);

        if (validationResult.IsFailure)
        {
            await callbacks.OnCheckOutFailureAsync(new CheckOutFailureData
            (request.TelegramUserId,
                validationResult.Error.Code,
                validationResult.Error.Message,
                DateTime.UtcNow),cancellationToken);
            return Result.Failure<Guid>(validationResult.Error);
        }

        var user = await userRepository.GetByTelegramUserIdAsync(request.TelegramUserId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));
        var attendance = await attendanceRepository.GetByUserAndDateAsync(user.Value.Id, today, cancellationToken);

        try
        {
            if (attendance.HasNoValue)
            {
                await callbacks.OnCheckOutFailureAsync(
                    new CheckOutFailureData(
                        request.TelegramUserId, 
                        "No_Check_In_Record",
                        "You must check in first before checking out",
                        DateTime.UtcNow), cancellationToken);
                return Result.Failure<Guid>(DomainErrors.Attendance.NoCheckInRecord);
            }

            if (!attendance.Value.CheckInTime.HasValue)
            {
                await callbacks.OnCheckOutFailureAsync(
                    new CheckOutFailureData(
                        request.TelegramUserId, 
                        "no_check_in_today",
                        "Please check in first",
                        DateTime.UtcNow), cancellationToken);

                return Result.Failure<Guid>(DomainErrors.Attendance.NoCheckInRecord);
            }

            if (attendance.Value.CheckOutTime.HasValue)
            {
                await callbacks.OnCheckOutFailureAsync(
                    new CheckOutFailureData(
                        request.TelegramUserId, 
                        "Already_checked_out",
                        "you alrady checked out today", 
                        DateTime.UtcNow), cancellationToken);

                return Result.Failure<Guid>(DomainErrors.Attendance.AlreadyChekedOut);
            }

            attendance.Value.CheckOut();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var checkOutTime = attendance.Value.CheckOutTime!.Value;
            var isEarlyDeparture = attendance.Value.IsEarlyDeparture(user.Value.WorkEndTime);
            var workDuration = attendance.Value.GetWorkDuration();

            TimeSpan? earlyBy = null;
            if (isEarlyDeparture)
            {
                var checkOutTimeOnly = TimeOnly.FromDateTime(checkOutTime);
                earlyBy = user.Value.WorkEndTime - checkOutTimeOnly;
            }

            await callbacks.OnCheckOutSuccessAsync(
                new CheckOutSuccessData(
                    request.TelegramUserId, 
                    $"{user.Value.FirstName}{user.Value.LastName}",
                    checkOutTime,
                    isEarlyDeparture,
                    workDuration,
                    earlyBy), cancellationToken);

            return Result.Success(attendance.Value.Id);
        }
        catch (Exception e)
        {
            await callbacks.OnCheckOutFailureAsync(
                new CheckOutFailureData(
                    request.TelegramUserId,
                    "System_error",
                    "a system error occured during check ou", 
                    DateTime.UtcNow), cancellationToken);
                throw;
        }

    }
}