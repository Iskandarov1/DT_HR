using System.Text.Json.Serialization;

namespace DT_HR.Contract.Requests.MiniApp;

public sealed record MiniAppCheckInRequest(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude,
    [property: JsonPropertyName("timestamp")] long Timestamp
);