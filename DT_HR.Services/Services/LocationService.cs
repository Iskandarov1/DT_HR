using DT_HR.Application.Core.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DT_HR.Services.Services;

public class LocationService : ILocationService
{

    private readonly IConfiguration _configuration;
    private readonly ILogger<LocationService> _logger;

    private readonly double _defaultOfficeLatitude;
    private readonly double _defaultOfficeLangitude;
    private readonly double _officeRadiusInMeters;

    public LocationService(IConfiguration configuration, ILogger<LocationService> logger)
    {
        _configuration = configuration;
        _logger = logger;


        _defaultOfficeLatitude = configuration.GetValue<double>("Office:Latitude", 41.332073);
        _defaultOfficeLangitude = configuration.GetValue<double>("Office:Longitude", 69.340047);
        _officeRadiusInMeters = configuration.GetValue<double>("Office:RadiusInMeters", 200);
    }
    public async Task<bool> IsWithInOfficeRadiusAsync(double latitude, double longitute)
    {
        try
        {
            var distance =
                await CalculateDistanceAsync(latitude, longitute, _defaultOfficeLatitude, _defaultOfficeLangitude);

            var isWithInRadius = distance <= _officeRadiusInMeters;
        
            _logger.LogInformation(
                "Location check: ({Latitude}, {Longitude}) is {Distance}m from office. Within radius: {IsWithinRadius}",
                latitude, longitute, distance, isWithInRadius);

            return isWithInRadius;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error checking if location is within office radius");
            return true;
        }
        
    }

    public Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2)
    {
        
        // Haversine formula to calculate distance between two points on Earth
        const double R = 6371000; 
        
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        var a = 
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        var distance = R * c;
        
        return Task.FromResult(Math.Round(distance, 2));
    }
    
    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
    
}