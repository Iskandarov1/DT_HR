using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Attendance.Commands.CheckOut;

public class CheckOutInputHandler(IUserRepository userRepository) : IInputHandler<CheckOutCommand>
{
    public async Task<Result> ValidateAsync(CheckOutCommand command, CancellationToken cancellationToken = default)
    {
        if (command.TelegramUserId <= 0)
            return Result.Failure(DomainErrors.TelegramUserId.Invalid);

        var user = await userRepository.GetByTelegramUserIdAsync(command.TelegramUserId,cancellationToken);
        if (user.HasNoValue)
            return Result.Failure(DomainErrors.TelegramUserId.Invalid);
        if(!user.Value.IsActive)
            return Result.Failure(DomainErrors.User.NotActive);
        
        return Result.Success();
    }
}