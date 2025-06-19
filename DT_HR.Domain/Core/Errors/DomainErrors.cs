using DT_HR.Domain.Core.Primitives;

namespace DT_HR.Domain.Core.Errors;

public static class DomainErrors
{
    
    
    /// <summary>
    /// Contains the user errors.
    /// </summary>
    public static class Item
    {
        private static string objectName = nameof(Item);

        public static string NullOrEmpty = $"{objectName}_NullOrEmpty";

        public static string NotFound = $"{objectName}_NotFound";

        public static string LengthIsNotEqual = $"{objectName}_LengthIsNotEqual";

        public static string LongerThanAllowed = $"{objectName}_LongerThanAllowed";

        public static string UnknownStatus = $"{objectName}_UnknownStatus";

        public static string InvalidStatusValue = $"{objectName}_InvalidStatusValue";

        public static string InvalidValue = $"{objectName}_InvalidValue";

        public static string NegativeValueError = $"{objectName}_NegativeValueError";

        public static string YouDoNotHaveAnAccess = $"{objectName}_YouDoNotHaveAnAccess";

        public static string YouHaveNotBeenVerified = $"{objectName}_YouHaveNotBeenVerified";

        public static string InvalidFormat = $"{objectName}_InvalidFormat";

        public static string AlreadyExist = $"{objectName}_AlreadyExist";

        public static string DuplicateItems = $"{objectName}_DuplicateItems";
        public static string RegionIsEmpty = $"{objectName}_RegionIsEmpty";
        public static string DistrictIsEmpty = $"{objectName}_DistrictIsEmpty";

        public static Error NullOrEmptyError => new Error($"{objectName}_NullOrEmpty", "The item is required.");
        public static Error LongerThanAllowedError(int maxLength) => new Error($"{objectName}_LongerThanAllowedError", $"The item is longer than {maxLength} is allowed.");
        public static Error LengthIsNotEqualError(int length) => new Error($"{objectName}_LengthIsNotEqual", $"The item length is not equal {length}.");


    }
    public static class Category
    {
        public static Error NotFound => new("category.not_found", "The category with the specified identifier was not found.");
        public static Error AlreadyExists => new("category.already_exists", "A category with this name already exists.");
    }
    
    public static class Location
    {
        public static Error InvalidLatitude => new("location.not_found", "The location with the specified Latitude is not valid.");
        public static Error InvalidLongitude => new("location.not_found", "A location with this Longitude is not valid.");
    }
    
    public static class Attendance
    {
        public static Error AlreadyChekedIn => new("user_already_checked_in", "The employee already checked in");
        public static Error ResonRequired => new("reason_required", "need to provide the reason");
        
        public static Error InvalidEstimatedArivalTime => new("invalid_ETA", "ETA should be higher than current time");
        public static Error EstimatedArrivalTooFar => new("invalid_ETA", "ETA Can't be more than 12 hours from now");
        
        public static Error ETARequiredForOnTheWay => new("invalid_ETA", "ETA not provided for on the way option");
        
        public static Error ETANotAllowedForAbsent => new("invalid_ETA", "ETA is No tAllowed For Absent");
        
        public static Error InvalidAbsenceType => new("invalid_ETA", "Invalid Absence Type");
        

    }
    public static class User
    {
        public static Error NotFound => new Error("User.NotFound", "The user with the specified identifier was not found.");

        public static Error InvalidPermissions => new Error(
            "User.InvalidPermissions",
            "The current user does not have the permissions to perform that operation.");
        
        public static Error DuplicateEmail => new Error("User.DuplicateEmail", "The specified email is already in use.");
        public static Error DuplicateTelegramId => new Error("User.DuplicateTelegramId", "The specified telegramId is already in use.");
        public static Error DuplicatePhoneNumber => new Error("User.DuplicatePhoneNumber", "The specified PhoneNumber is already in use.");
        public static Error NotActive => new Error("User.NotActive", "The user with the specified identifier is not active.");

        public static Error CannotChangePassword => new Error(
            "User.CannotChangePassword",
            "The password cannot be changed to the specified password.");
    }

    public static class Email
    {
        public static Error NullOrEmpty => new Error("Email.NullOrEmpty", "The email is required.");

        public static Error LongerThanAllowed => new Error("Email.LongerThanAllowed", "The email is longer than allowed.");

        public static Error InvalidFormat => new Error("Email.InvalidFormat", "The email format is invalid.");
    }

    public static class PhoneNumber
    {
        public static Error NullOrEmpty => new Error("PhoneNumber.NullOrEmpty", "The phone number is required.");
    
        public static Error InvalidLength => new Error("PhoneNumber.InvalidLength", "The phone number must be between 10 and 15 digits.");
    
        public static Error InvalidFormat => new Error("PhoneNumber.InvalidFormat", "The phone number format is invalid.");
    }

    public static class TelegramUserId
    {
        public static readonly Error Invalid = new(
            "TelegramUserId.Invalid", 
            "The Telegram user ID is invalid.");
    }

    public static class Subscriber
    {
        public static Error NotFound => new Error("Subscriber.NotFound", "The subscriber was not found.");
        public static Error AlreadyExists => new Error("Subscriber.AlreadyExists", "A subscriber with this contact method already exists.");
        public static Error InvalidContactMethod => new Error("Subscriber.InvalidContactMethod", "Exactly one contact method (email, phone, or telegram) must be provided.");
    }

    /// <summary>
    /// Contains the notification errors.
    /// </summary>
    public static class Notification
    {
        public static Error AlreadySent => new Error("Notification.AlreadySent", "The notification has already been sent.");
    }

    public static class Quote
    {
        public static class Author
        {
            public static Error NullOrEmpty => new Error("Author.NullOrEmpty", "The author is required.");
            public static Error LongerThanAllowed => new Error("Author.LongerThanAllowed", "The author is longer than allowed.");
        }

        public static class Textt
        {
            public static Error NullOrEmpty => new Error("Text.NullOrEmpty", "The quote text is required.");
            public static Error LongerThanAllowed => new Error("Text.LongerThanAllowed", "The quote text is longer than allowed.");
        }

        public static class Category
        {
            public static Error NullOrEmpty => new Error("Category.NullOrEmpty", "The category is required.");
            public static Error LongerThanAllowed => new Error("Category.LongerThanAllowed", "The category is longer than allowed.");
        }

        public static Error NotFound => new Error("Quote.NotFound", "The quote with the specified identifier was not found.");
    }

    /// <summary>
    /// Contains general errors.
    /// </summary>
    public static class General
    {
        public static Error UnProcessableRequest => new Error(
            "General.UnProcessableRequest",
            "The server could not process the request.");

        public static Error ServerError => new Error("General.ServerError", "The server encountered an unrecoverable error.");
    }

    
}
