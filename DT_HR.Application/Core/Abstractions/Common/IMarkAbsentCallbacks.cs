using DT_HR.Contract.CallbackData.Attendance;

namespace DT_HR.Application.Core.Abstractions.Common;

public interface IMarkAbsentCallbacks
{
    Task OnAbsenceMarkedAsync(AbsenceMarkedData data, CancellationToken cancellationToken);
    Task OnEmployeeOnTheWayAsync(OnTheWayData data, CancellationToken cancellationToken);

    Task OnMarkAbsentFailureAsync(MarkAbsentFailureData data, CancellationToken cancellationToken);
}