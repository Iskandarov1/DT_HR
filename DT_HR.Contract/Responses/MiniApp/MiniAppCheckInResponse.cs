using System.Text.Json.Serialization;

namespace DT_HR.Contract.Responses.MiniApp;

public sealed record MiniAppCheckInResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string? Message = null,
    [property: JsonPropertyName("error")] string? Error = null
);