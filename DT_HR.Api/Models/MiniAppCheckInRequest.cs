namespace DT_HR.Api.Models;

public class MiniAppCheckInRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public long Timestamp { get; set; }
}