using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Localizations;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Domain.ValueObjects
{
    /// <summary>
    /// Represents the last name value object.
    /// </summary>
    public sealed class LastName : ValueObject
    {
        /// <summary>
        /// The last name maximum length.
        /// </summary>
        public const int MaxLength = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="LastName"/> class.
        /// </summary>
        /// <param name="value">The last name value.</param>
        private LastName(string value) => Value = value;

        /// <summary>
        /// Gets the last name value.
        /// </summary>
        public string Value { get; }

        public static implicit operator string(LastName lastName) => lastName.Value;

        /// <summary>
        /// Creates a new <see cref="FirstName"/> instance based on the specified value.
        /// </summary>
        /// <param name="lastName">The last name value.</param>
        /// <returns>The result of the last name creation process containing the last name or an error.</returns>
        public static Result<LastName> Create(string value, string field, ISharedViewLocalizer sharedViewLocalizer) =>
            Result.Create(value, new Error(CaseConverter.PascalToSnakeCase(field), sharedViewLocalizer[DomainErrors.Item.NullOrEmptyError]))
                .Ensure(n => !string.IsNullOrWhiteSpace(n), new Error(CaseConverter.PascalToSnakeCase(field), sharedViewLocalizer[DomainErrors.Item.NullOrEmptyError]))
                .Ensure(n => n.Length <= MaxLength, new Error(CaseConverter.PascalToSnakeCase(field), string.Format(sharedViewLocalizer[DomainErrors.Item.LongerThanAllowed], MaxLength)))
                .Map(f => new LastName(f));

        /// <inheritdoc />
        public override string ToString() => Value;

        /// <inheritdoc />
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }
    }
}
