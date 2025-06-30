using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Users.Commands.UserRole;

public class ChangeUserRoleCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ChangeUserRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var userMaybe = await userRepository.GetByIdAsync(request.UsedId, cancellationToken);
        if (userMaybe.HasNoValue)
            return Result.Failure(DomainErrors.User.NotFound);

        var user = userMaybe.Value;
        user.SetRole(request.Role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();

    }
}