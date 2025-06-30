using DT_HR.Application.Attendance.Commands.CheckIn;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Attendance.InputHandler;

public class CheckInInputHandler(IUserRepository userRepository) : IInputHandler<CheckInCommand>
{
    public async Task<Result> ValidateAsync(CheckInCommand command, CancellationToken cancellationToken)
    {
        if (command.TelegramUserId <= 0)
            return Result.Failure(DomainErrors.TelegramUserId.Invalid);

        if (command.Latitude < -90 || command.Latitude > 90)
            return Result.Failure(DomainErrors.Location.InvalidLatitude);

        if (command.Longitude < -180 || command.Longitude > 180)
            return Result.Failure(DomainErrors.Location.InvalidLongitude);


        var user = await userRepository.GetByTelegramUserIdAsync(command.TelegramUserId, cancellationToken);
        if (user.HasNoValue)
            return Result.Failure(DomainErrors.TelegramUserId.Invalid);

        if (!user.Value.IsActive)
            return Result.Failure(DomainErrors.User.NotActive);

        return Result.Success();
    }
}