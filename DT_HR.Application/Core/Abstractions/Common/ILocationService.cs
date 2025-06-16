namespace DT_HR.Application.Core.Abstractions.Common;

public interface ILocationService
{
    Task<bool> IsWithInOfficeRadiusAsync(double latitude, double longitute, Guid? officeLocationId = null);
    Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2);
}