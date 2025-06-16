using System.Text.Json.Serialization;

namespace DT_HR.Contract.Requests.UserRequest;

public record CreateUserRequest
( 
    [property: JsonPropertyName("first_name")]
    string FirstName ,

    [property: JsonPropertyName("last_name")]
    string LastName ,

    [property: JsonPropertyName("phone_number")]
    string PhoneNumber ,

    [property: JsonPropertyName("telegramUser_id")]
    long? TelegramUserId 
);