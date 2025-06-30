using DT_HR.Contract.CallbackData.Attendance;

namespace DT_HR.Application.Core.Abstractions.Common;

public interface ICheckInCallbacks
{
    Task<bool> ValidateLocationAsync(double latitude, double longitude, CancellationToken cancellationToken);

    Task OnCheckInSuccessAsync(CheckInSuccessData data, CancellationToken cancellationToken = default);

    Task OnCheckInFailureAsync(CheckInFailureDate date, CancellationToken cancellationToken = default);
}