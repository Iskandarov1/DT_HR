using System.Text.RegularExpressions;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Localizations;
using DT_HR.Domain.Core.Primitives;
using Quote.Domain.Core.Primitives.Result;

namespace DT_HR.Domain.ValueObjects
{
    /// <summary>
    /// Represents the email value object.
    /// </summary>
    public sealed class Email : ValueObject
    {
    
        public const int MaxLength = 256;

        private const string EmailRegexPattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";

        private static readonly Lazy<Regex> EmailFormatRegex =
            new Lazy<Regex>(() => new Regex(EmailRegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
        
        private Email(string value) => Value = value;

        /// <summary>
        /// Gets the email value.
        /// </summary>
        public string Value { get; }

        public static implicit operator string(Email email) => email.Value;

        /// <summary>
        /// Creates a new <see cref="Email"/> instance based on the specified value.
        /// </summary>
        /// <param name="email">The email value.</param>
        /// <returns>The result of the email creation process containing the email or an error.</returns>
        public static Result<Email> Create(string value, string field, ISharedViewLocalizer sharedViewLocalizer) =>
            Result.Create(value, new Error(CaseConverter.PascalToSnakeCase(field), sharedViewLocalizer[DomainErrors.Item.NullOrEmptyError]))
                .Ensure(n => !string.IsNullOrWhiteSpace(n), new Error(CaseConverter.PascalToSnakeCase(field), sharedViewLocalizer[DomainErrors.Item.NullOrEmptyError]))
                .Ensure(n => n.Length <= MaxLength, new Error(CaseConverter.PascalToSnakeCase(field), string.Format(sharedViewLocalizer[DomainErrors.Item.LongerThanAllowed], MaxLength)))
                .Map(f => new Email(f));

        /// <inheritdoc />
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}