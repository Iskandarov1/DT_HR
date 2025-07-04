namespace DT_HR.Api.Models;

public class MiniAppCheckInResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
}