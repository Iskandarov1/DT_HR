namespace DT_HR.Application.Core.Abstractions.Services;

public interface IBackgroundJobRefreshService
{
    Task RefreshAllJobsAsync(CancellationToken cancellationToken = default);
}