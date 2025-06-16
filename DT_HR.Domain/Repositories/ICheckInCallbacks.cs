using DT_HR.Contract.FailureData;
using DT_HR.Contract.SuccessData;

namespace DT_HR.Domain.Repositories;

public interface ICheckInCallbacks
{
    Task<bool> ValidateLocationAsync(double latitude, double longitude, CancellationToken cancellationToken);

    Task OnCheckInSuccessAsync(CheckInSuccessData data, CancellationToken cancellationToken = default);

    Task OnCheckInFailureAsync(CheckInFailureDate date, CancellationToken cancellationToken = default);
}