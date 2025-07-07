using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Contract.Requests.UserRequest;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Localizations;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Repositories;
using DT_HR.Domain.ValueObjects;

namespace DT_HR.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler(
    IUserRepository userRepository,
    ICompanyRepository companyRepository,
    IUnitOfWork unitOfWork,
    ISharedViewLocalizer sharedViewLocalizer,
    IUserBackgroundJobService userBackgroundJobService) : ICommandHandler<RegisterUserCommand , Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetByTelegramUserIdAsync(request.TelegramUserId,cancellationToken);
        if (existingUser.HasValue)
            return Result.Failure<Guid>(DomainErrors.User.DuplicateTelegramId);

        var existingTelegramUser = await userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        if (existingTelegramUser.HasValue)
            return Result.Failure<Guid>(DomainErrors.User.DuplicatePhoneNumber);
        
        var phoneNumberResult = PhoneNumber.Create(request.PhoneNumber, CaseConverter.PascalToSnakeCase(nameof(CreateUserRequest.PhoneNumber)), sharedViewLocalizer);
        if (phoneNumberResult.IsFailure)
            return Result.Failure<Guid>(phoneNumberResult.Error);
        
        // Get company's default work hours
        var companyMaybe = await companyRepository.GetAsync(cancellationToken);
        TimeOnly workStartTime = new TimeOnly(21, 05); // Default fallback
        TimeOnly workEndTime = new TimeOnly(23, 0);    // Default fallback
        
        if (companyMaybe.HasValue)
        {
            workStartTime = companyMaybe.Value.DefaultWorkStartTime;
            workEndTime = companyMaybe.Value.DefaultWorkEndTime;
        }

        var user = new User(
            request.TelegramUserId,
            phoneNumberResult.Value,
            request.FirstName,
            request.LastName,
            request.BirthDate,
            workStartTime,
            workEndTime,
            request.Language);
        
        userRepository.Insert(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await userBackgroundJobService.InitializeBackgroundJobsForUserAsync(user, cancellationToken);

        return Result.Success(user.Id);


    }
}