using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Contract.Responses;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DT_HR.Infrastructure.Services;

public class ExcelExportService(
    ILocalizationService localizationService,
    ILogger<ExcelExportService> logger) : IExcelExportService
{
    public async Task<byte[]> ExportAttendanceToExcelAsync(
        IEnumerable<EmployeeAttendanceResponse> attendanceData,
        DateOnly startDate,
        DateOnly endDate,
        string language,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Attendance Report");

            // Set headers
            SetHeaders(worksheet, language);

            // Add data
            var dataRows = attendanceData.ToList();
            for (int i = 0; i < dataRows.Count; i++)
            {
                var row = dataRows[i];
                var rowIndex = i + 2; // Starting from row 2 (after header)

                worksheet.Cells[rowIndex, 1].Value = row.Name;
                worksheet.Cells[rowIndex, 2].Value = row.PhoneNumber;
                worksheet.Cells[rowIndex, 3].Value = GetLocalizedStatus(row.Status, language);
                worksheet.Cells[rowIndex, 4].Value = row.CheckInTime?.ToString("yyyy-MM-dd");
                worksheet.Cells[rowIndex, 5].Value = row.CheckInTime?.ToString("HH:mm");
                worksheet.Cells[rowIndex, 6].Value = row.CheckOutTime?.ToString("HH:mm");
                worksheet.Cells[rowIndex, 7].Value = row.WorkDuration?.ToString(@"hh\:mm");
                worksheet.Cells[rowIndex, 8].Value = row.IsLate == true ? 
                    localizationService.GetString(ResourceKeys.Yes, language) : 
                    localizationService.GetString(ResourceKeys.No, language);
                worksheet.Cells[rowIndex, 9].Value = row.LateBy ?? "";
                worksheet.Cells[rowIndex, 10].Value = row.IsEarlyDeparture ? 
                    localizationService.GetString(ResourceKeys.Yes, language) : 
                    localizationService.GetString(ResourceKeys.No, language);
                worksheet.Cells[rowIndex, 11].Value = row.AbsenceReason ?? "";
                worksheet.Cells[rowIndex, 12].Value = row.IsWithInRadius ? 
                    localizationService.GetString(ResourceKeys.Yes, language) : 
                    localizationService.GetString(ResourceKeys.No, language);
            }

            // Format worksheet
            FormatWorksheet(worksheet, dataRows.Count + 1, language, startDate, endDate);

            return await package.GetAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while exporting attendance data to Excel");
            throw;
        }
    }

    private void SetHeaders(ExcelWorksheet worksheet, string language)
    {
        worksheet.Cells[1, 1].Value = localizationService.GetString("EmployeeName", language);
        worksheet.Cells[1, 2].Value = localizationService.GetString("PhoneNumber", language);
        worksheet.Cells[1, 3].Value = localizationService.GetString(ResourceKeys.Status, language);
        worksheet.Cells[1, 4].Value = localizationService.GetString(ResourceKeys.Date, language);
        worksheet.Cells[1, 5].Value = localizationService.GetString("CheckInTime", language);
        worksheet.Cells[1, 6].Value = localizationService.GetString("CheckOutTime", language);
        worksheet.Cells[1, 7].Value = localizationService.GetString(ResourceKeys.WorkDuration, language);
        worksheet.Cells[1, 8].Value = localizationService.GetString("IsLate", language);
        worksheet.Cells[1, 9].Value = localizationService.GetString("LateBy", language);
        worksheet.Cells[1, 10].Value = localizationService.GetString("EarlyDeparture", language);
        worksheet.Cells[1, 11].Value = localizationService.GetString("AbsenceReason", language);
        worksheet.Cells[1, 12].Value = localizationService.GetString("WithinOfficeRadius", language);
    }

    private void FormatWorksheet(ExcelWorksheet worksheet, int totalRows, string language, DateOnly startDate, DateOnly endDate)
    {
        // Set title
        worksheet.InsertRow(1, 1);
        worksheet.Cells[1, 1].Value = $"{localizationService.GetString("AttendanceReport", language)} ({startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd})";
        worksheet.Cells[1, 1, 1, 12].Merge = true;
        worksheet.Cells[1, 1].Style.Font.Bold = true;
        worksheet.Cells[1, 1].Style.Font.Size = 16;
        worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Format headers
        var headerRange = worksheet.Cells[2, 1, 2, 12];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

        // Auto-fit columns
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // Add borders to all data
        var dataRange = worksheet.Cells[2, 1, totalRows + 1, 12];
        dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
    }

    private string GetLocalizedStatus(string status, string language)
    {
        return status switch
        {
            "Present" => localizationService.GetString(ResourceKeys.Present, language),
            "Absent" => localizationService.GetString(ResourceKeys.Absent, language),
            "OnTheWay" => localizationService.GetString(ResourceKeys.OnTheWay, language),
            "NoRecord" => localizationService.GetString("NoRecord", language),
            _ => status
        };
    }
}