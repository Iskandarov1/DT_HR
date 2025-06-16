using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Localizations;
using DT_HR.Domain.Core.Primitives;
using Quote.Domain.Core.Primitives.Result;

namespace DT_HR.Domain.ValueObjects
{
    /// <summary>
    /// Represents the first name value object.
    /// </summary>
    public sealed class FirstName : ValueObject
    {
        /// <summary>
        /// The first name maximum length.
        /// </summary>
        public const int MaxLength = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstName"/> class.
        /// </summary>
        /// <param name="value">The first name value.</param>
        private FirstName(string value) => Value = value;

        /// <summary>
        /// Gets the first name value.
        /// </summary>
        public string Value { get; }

        public static implicit operator string(FirstName firstName) => firstName.Value;

        /// <summary>
        /// Creates a new <see cref="FirstName"/> instance based on the specified value.
        /// </summary>
        /// <param name="firstName">The first name value.</param>
        /// <returns>The result of the first name creation process containing the first name or an error.</returns>
        public static Result<FirstName> Create(string value, string field, ISharedViewLocalizer sharedViewLocalizer) =>
            Result.Create(value, new Error(CaseConverter.PascalToSnakeCase(field), sharedViewLocalizer[DomainErrors.Item.NullOrEmptyError]))
                .Ensure(n => !string.IsNullOrWhiteSpace(n), new Error(CaseConverter.PascalToSnakeCase(field), sharedViewLocalizer[DomainErrors.Item.NullOrEmptyError]))
                .Ensure(n => n.Length <= MaxLength, new Error(CaseConverter.PascalToSnakeCase(field), string.Format(sharedViewLocalizer[DomainErrors.Item.LongerThanAllowed], MaxLength)))
                .Map(f => new FirstName(f));
        /// <inheritdoc />
        public override string ToString() => Value;

        /// <inheritdoc />
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}
