using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Application.Resources;
using DT_HR.Contract.Responses;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting;
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
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            progress?.Report(5);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Attendance Report");

            var dataRows = attendanceData.ToList();
            var totalRows = dataRows.Count;
            
            progress?.Report(10);
            
            // Check if this is a multi-day report
            bool isMultiDay = startDate != endDate;
            
            if (isMultiDay)
            {
                // Set headers for multi-day report
                SetMultiDayHeaders(worksheet, language);
                
                int currentRow = 2;
                
                // Group data by date
                var groupedByDate = dataRows
                    .GroupBy(row => row.Date)
                    .OrderBy(g => g.Key)
                    .ToList();
                
                // Generate attendance records for each date
                int processedRows = 0;
                foreach (var dateGroup in groupedByDate)
                {
                    var date = dateGroup.Key;
                    var dateRecords = dateGroup.ToList();
                    
                    // Add date separator
                    worksheet.Cells[currentRow, 1].Value = date.ToString("dd-MM-yyyy");
                    worksheet.Cells[currentRow, 1, currentRow, 13].Merge = true;
                    worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    worksheet.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    currentRow++;
                    
                    // Add data rows for this date
                    foreach (var row in dateRecords)
                    {
                        AddDataRow(worksheet, row, currentRow, language, date);
                        currentRow++;
                        processedRows++;
                        
                        // Report progress (20% to 70% of total process)
                        if (totalRows > 0)
                        {
                            var progressPercentage = 20 + (int)((double)processedRows / totalRows * 50);
                            progress?.Report(progressPercentage);
                        }
                    }
                    
                    // Add empty row between dates
                    if (date < endDate)
                    {
                        currentRow++;
                    }
                }
                
                // Format multi-day worksheet
                progress?.Report(70);
                FormatMultiDayWorksheet(worksheet, currentRow - 1, language, startDate, endDate, dataRows);
            }
            else
            {
                // Single day report - use original format
                SetHeaders(worksheet, language);
                
                for (int i = 0; i < dataRows.Count; i++)
                {
                    var row = dataRows[i];
                    var rowIndex = i + 2;
                    AddDataRow(worksheet, row, rowIndex, language, startDate);
                    
                    // Report progress (20% to 70% of total process)
                    if (totalRows > 0)
                    {
                        var progressPercentage = 20 + (int)((double)(i + 1) / totalRows * 50);
                        progress?.Report(progressPercentage);
                    }
                }
                
                FormatWorksheet(worksheet, dataRows.Count + 1, language, startDate, endDate, dataRows);
            }

            progress?.Report(75);
            
            var result = await package.GetAsByteArrayAsync(cancellationToken);
            
            progress?.Report(100);
            
            return result;
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

    private void FormatWorksheet(ExcelWorksheet worksheet, int totalRows, string language, DateOnly startDate, DateOnly endDate, IEnumerable<EmployeeAttendanceResponse> attendanceData)
    {
        // Set title
        worksheet.InsertRow(1, 1);
        worksheet.Cells[1, 1].Value = $"{localizationService.GetString("AttendanceReport", language)} ({startDate:dd-MM-yyyy} - {endDate:dd-MM-yyyy})";
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
        
        // Add performance section below
        AddPerformanceSection(worksheet, totalRows + 2, language, attendanceData);
    }

    private void SetMultiDayHeaders(ExcelWorksheet worksheet, string language)
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

    private void AddDataRow(ExcelWorksheet worksheet, EmployeeAttendanceResponse row, int rowIndex, string language, DateOnly currentDate)
    {
        worksheet.Cells[rowIndex, 1].Value = row.Name;
        worksheet.Cells[rowIndex, 2].Value = row.PhoneNumber;
        worksheet.Cells[rowIndex, 3].Value = GetLocalizedStatus(row.Status, language);
        worksheet.Cells[rowIndex, 4].Value = currentDate.ToString("dd-MM-yyyy");
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

    private void FormatMultiDayWorksheet(ExcelWorksheet worksheet, int totalRows, string language, DateOnly startDate, DateOnly endDate, IEnumerable<EmployeeAttendanceResponse> attendanceData)
    {
        // Set title
        worksheet.InsertRow(1, 1);
        worksheet.Cells[1, 1].Value = $"{localizationService.GetString("AttendanceReport", language)} ({startDate:dd-MM-yyyy} - {endDate:dd-MM-yyyy})";
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
        
        // Add performance section below
        AddPerformanceSection(worksheet, totalRows + 2, language, attendanceData);
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
    
    private static int CalculatePerformanceScore(EmployeeAttendanceResponse attendance)
    {
        var score = 0;
        
        // Base score for being present
        if (attendance.Status == "Present")
        {
            score += 60; // 60% for showing up
            
            // On-time bonus (20%)
            if (attendance.IsLate != true)
            {
                score += 20;
            }
            
            // Full day bonus (10%)
            if (!attendance.IsEarlyDeparture)
            {
                score += 10;
            }
            
            // Office location bonus (10%)
            if (attendance.IsWithInRadius)
            {
                score += 10;
            }
        }
        else if (attendance.Status == "OnTheWay")
        {
            score += 30; // Partial credit for being on the way
        }
        // Absent = 0 score
        
        return Math.Min(score, 100); // Cap at 100%
    }
    
    private void AddPerformanceSection(ExcelWorksheet worksheet, int startRow, string language, IEnumerable<EmployeeAttendanceResponse> attendanceData)
    {
        // Add section title (bigger performance section)
        worksheet.Cells[startRow, 1].Value = localizationService.GetString(ResourceKeys.Performance, language);
        worksheet.Cells[startRow, 1, startRow, 3].Merge = true;
        worksheet.Cells[startRow, 1].Style.Font.Bold = true;
        worksheet.Cells[startRow, 1].Style.Font.Size = 18;
        worksheet.Cells[startRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells[startRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[startRow, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
        
        // Add headers
        var headerRow = startRow + 1;
        worksheet.Cells[headerRow, 1].Value = localizationService.GetString("EmployeeName", language);
        worksheet.Cells[headerRow, 2].Value = localizationService.GetString(ResourceKeys.Performance, language);
        worksheet.Cells[headerRow, 3].Value = localizationService.GetString(ResourceKeys.PerformanceBar, language);
        
        // Format headers
        var headerRange = worksheet.Cells[headerRow, 1, headerRow, 3];
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        
        // Group by employee name and calculate average performance
        var employeePerformances = attendanceData
            .GroupBy(x => x.Name)
            .Select(g => new
            {
                Name = g.Key,
                AveragePerformance = g.Average(x => CalculatePerformanceScore(x)) / 100.0
            })
            .OrderBy(x => x.Name)
            .ToList();
        
        // Add performance data
        int currentRow = headerRow + 1;
        foreach (var employee in employeePerformances)
        {
            worksheet.Cells[currentRow, 1].Value = employee.Name;
            worksheet.Cells[currentRow, 2].Value = employee.AveragePerformance;
            worksheet.Cells[currentRow, 2].Style.Numberformat.Format = "0%";
            
            // Create visual progress bar (reasonable size for Excel)
            var barLength = 20;
            var filledBars = (int)(employee.AveragePerformance * barLength);
            var emptyBars = barLength - filledBars;
            var progressBar = new string('█', filledBars) + new string('░', emptyBars);
            var displayText = $"{employee.AveragePerformance:P0} {progressBar}";
            
            worksheet.Cells[currentRow, 3].Value = displayText;
            worksheet.Cells[currentRow, 3].Style.Font.Name = "Consolas";
            worksheet.Cells[currentRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            // Color coding based on performance
            var performanceCell = worksheet.Cells[currentRow, 3];
            if (employee.AveragePerformance >= 0.8) // 80% and above - Green
            {
                performanceCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                performanceCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(198, 239, 206));
                performanceCell.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(0, 97, 0));
            }
            else if (employee.AveragePerformance >= 0.6) // 60-79% - Light Green
            {
                performanceCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                performanceCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 242, 157));
                performanceCell.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(156, 87, 0));
            }
            else if (employee.AveragePerformance >= 0.3) // 30-59% - Orange
            {
                performanceCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                performanceCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 205, 131));
                performanceCell.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(156, 39, 6));
            }
            else // Below 30% - Red
            {
                performanceCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                performanceCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 199, 206));
                performanceCell.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(156, 0, 6));
            }
            
            currentRow++;
        }
        
        // Add borders to performance section
        var performanceRange = worksheet.Cells[startRow, 1, currentRow - 1, 3];
        performanceRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        performanceRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        performanceRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        performanceRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        
        // Set reasonable column widths that won't affect main report
        if (worksheet.Column(1).Width < 20) worksheet.Column(1).Width = 20;
        if (worksheet.Column(2).Width < 15) worksheet.Column(2).Width = 15;
        if (worksheet.Column(3).Width < 30) worksheet.Column(3).Width = 30;
    }
}