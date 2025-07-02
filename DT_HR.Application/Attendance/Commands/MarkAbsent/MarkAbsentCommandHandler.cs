using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Contract.CallbackData.Attendance;
using DT_HR.Domain.Core;
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
    ILocalizationService localization,
    IMarkAbsentCallbacks callbacks) : ICommandHandler<MarkAbsentCommand,Result<Guid>>
{
    public async Task<Result<Guid>> Handle(MarkAbsentCommand request, CancellationToken cancellationToken)
    {

        var validateResult = await inputHandler.ValidateAsync(request, cancellationToken);

        if (validateResult.IsFailure)
        {
            await callbacks.OnMarkAbsentFailureAsync(
                new MarkAbsentFailureData(
                    request.TelegramUserId,
                    validateResult.Error.Code,
                    validateResult.Error.Message,
                    TimeUtils.Now
                ), cancellationToken);
            return Result.Failure<Guid>(validateResult.Error);
        }

        var user = await userRepository.GetByTelegramUserIdAsync(request.TelegramUserId, cancellationToken);
        var today = DateOnly.FromDateTime(TimeUtils.Now);

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
                var lang = await localization.GetUserLanguage(request.TelegramUserId);
                await callbacks.OnMarkAbsentFailureAsync(
                    new MarkAbsentFailureData(
                        request.TelegramUserId,
                        "Already_Cheked_In",
                        localization.GetString(ResourceKeys.AlreadyCheckedIn,lang),
                        TimeUtils.Now
                    ),cancellationToken);

                return Result.Failure<Guid>(new Error("Attendance.AlreadyCheckedIn",
                    "Attendance already checked in, cannot mark as absent"));
            }

            if (attendance.Value.Status == AttendanceStatus.Absent.Value ||
                attendance.Value.Status == AttendanceStatus.OnTheWay.Value)
            {
                var lang = await localization.GetUserLanguage(request.TelegramUserId);
                await callbacks.OnMarkAbsentFailureAsync(
                    new MarkAbsentFailureData(
                        request.TelegramUserId,
                        "Already_Marked_Absent",
                        localization.GetString(ResourceKeys.AlreadyReportedAbsence,lang) + "\n"+localization.GetString(ResourceKeys.PleaseCheckInFirst,lang),
                        TimeUtils.Now), cancellationToken);

                return Result.Failure<Guid>(new Error("Attendance.AlreadyMarkedAbsent",
                    "Absence already reported today"));
            }

            attendance.Value.MarkAbsent(request.Reason, request.EstimatedArrivalTime);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            

            if (request.EstimatedArrivalTime.HasValue)
            {
                await callbacks.OnEmployeeOnTheWayAsync(
                    new OnTheWayData(
                        request.TelegramUserId,
                        request.EstimatedArrivalTime.Value,
                        request.Reason), cancellationToken);
            }
            else
            {
                await callbacks.OnAbsenceMarkedAsync(
                new AbsenceMarkedData(
                    request.TelegramUserId,
                    $"{user.Value.FirstName} {user.Value.LastName}",
                    request.Reason,
                    request.AbsenceType.Value,
                    request.EstimatedArrivalTime,
                    TimeUtils.Now
                    ), cancellationToken);
            }
            return Result.Success(attendance.Value.Id);
            
        }
        catch (Exception e)
        {
            var lang = await localization.GetUserLanguage(request.TelegramUserId);
            await callbacks.OnMarkAbsentFailureAsync(
                new MarkAbsentFailureData(
                    request.TelegramUserId,
                    "System_Error",
                    localization.GetString(ResourceKeys.ErrorOccurred,lang),
                    TimeUtils.Now
                ), cancellationToken);
            throw;
        }
        
    }
}