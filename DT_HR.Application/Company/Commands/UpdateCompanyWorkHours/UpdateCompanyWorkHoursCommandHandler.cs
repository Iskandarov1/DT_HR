using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Company.Commands.UpdateCompanyWorkHours;

public class UpdateCompanyWorkHoursCommandHandler(
    ICompanyRepository companyRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IUserBackgroundJobService userBackgroundJobService) : ICommandHandler<UpdateCompanyWorkHoursCommand, Result> 
{
    public async Task<Result> Handle(UpdateCompanyWorkHoursCommand request, CancellationToken cancellationToken)
    {
        var companyMaybe = await companyRepository.GetAsync(cancellationToken);
        if (companyMaybe.HasNoValue)
        {
            return Result.Failure(DomainErrors.User.NotFound);
        }

        var company = companyMaybe.Value;
        
        // Update company work hours
        company.UpdateWorkHours(request.WorkStartTime, request.WorkEndTime);
        
        // Update all active users' work hours to match the company's new work hours
        var activeUsers = await userRepository.GetActiveUsersAsync(cancellationToken);
        foreach (var user in activeUsers)
        {
            user.UpdateWorkHours(request.WorkStartTime, request.WorkEndTime);
        }
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await userBackgroundJobService.RescheduleAllUserJobsAsync(cancellationToken);
        await userBackgroundJobService.RescheduleCompanyWideJobsAsync(cancellationToken);
        
        return Result.Success();
    }
}