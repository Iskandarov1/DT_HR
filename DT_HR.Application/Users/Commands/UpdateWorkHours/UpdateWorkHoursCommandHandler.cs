using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Users.Commands.UpdateWorkHours;

public class UpdateWorkHoursCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateWorkHoursCommand, Result>
{
    public async Task<Result> Handle(UpdateWorkHoursCommand request, CancellationToken cancellationToken)
    {
        // Business rule: Work start time must be before work end time
        if (request.WorkStartTime >= request.WorkEndTime)
        {
            return Result.Failure(DomainErrors.User.InvalidWorkTimeRange);
        }

        // Get the user from repository
        var userMaybe = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (userMaybe.HasNoValue)
        {
            return Result.Failure(DomainErrors.User.NotFound);
        }

        var user = userMaybe.Value;
        
        // Update work hours using domain method
        user.UpdateWorkHours(request.WorkStartTime, request.WorkEndTime);
        
        // Save changes to database
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}