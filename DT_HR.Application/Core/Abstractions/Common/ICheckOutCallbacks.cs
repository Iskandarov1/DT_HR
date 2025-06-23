using DT_HR.Contract.CallbackData.Attendance;

namespace DT_HR.Application.Core.Abstractions.Common;

public interface ICheckOutCallbacks
{
    Task OnCheckOutSuccessAsync(CheckOutSuccessData data, CancellationToken cancellationToken);
    Task OnCheckOutFailureAsync(CheckOutFailureData data, CancellationToken cancellationToken);
}