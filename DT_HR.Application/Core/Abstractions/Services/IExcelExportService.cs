using DT_HR.Contract.Responses;

namespace DT_HR.Application.Core.Abstractions.Services;

public interface IExcelExportService
{
    Task<byte[]> ExportAttendanceToExcelAsync(
        IEnumerable<EmployeeAttendanceResponse> attendanceData,
        DateOnly startDate,
        DateOnly endDate,
        string language,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}