using DT_HR.Domain.Entities;

namespace DT_HR.Application.Core.Abstractions.Services;

public interface IUserBackgroundJobService
{
    Task InitializeBackgroundJobsForUserAsync(User user, CancellationToken cancellationToken = default);
    Task RescheduleAllUserJobsAsync(CancellationToken cancellationToken = default);
    Task RescheduleCompanyWideJobsAsync(CancellationToken cancellationToken = default);
}