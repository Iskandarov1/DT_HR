using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Users.Commands.UpdateWorkHourss;

public class UpdateWorkHoursCommandHandler(
    IUnitOfWork unitOfWork,
    IUserRepository userRepository) : ICommandHandler<UpdateWorkHoursCommand, Result>
{
    public async Task<Result> Handle(UpdateWorkHoursCommand request, CancellationToken cancellationToken)
    {
        if (request.WorkStartTime >= request.WorkEndTime)
        {
            return Result.Failure(DomainErrors.User.InvalidWorkTimeRange);
        }

        var maybeUser = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (maybeUser.HasNoValue)
        {
            return Result.Failure(DomainErrors.User.NotFound);
        }

        var user = maybeUser.Value;
        
        user.UpdateWorkHours(request.WorkStartTime,request.WorkEndTime);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}