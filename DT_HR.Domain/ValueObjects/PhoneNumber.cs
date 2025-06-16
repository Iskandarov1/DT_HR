using System.Text.RegularExpressions;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Localizations;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Core.Primitives.Result;


namespace DT_HR.Domain.ValueObjects;

/// <summary>
/// Represents the phone number value object.
/// </summary>
public class PhoneNumber : ValueObject
{
	/// <summary>
	/// The PhoneNumber maximum length.
	/// </summary>
	public const int MaxLength = 50;

	/// <summary>
	/// Initializes a new instance of the <see cref="PhoneNumber"/> class.
	/// </summary>
	/// <param name="value">The PhoneNumber value.</param>
	private PhoneNumber(string value) => Value = value;

	/// <summary>
	/// Here is a regular expression pattern for validating Uzbek phone numbers.
	/// </summary>
	private const string PhoneNumberRegexPattern = @"^[+]?998([3781]{2}|(9[013-57-9]))\d{7}$";

	private static readonly Lazy<Regex> PhoneNumberFormatRegex = new Lazy<Regex>(() => new Regex(PhoneNumberRegexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));

	/// <summary>
	/// Gets the PhoneNumber value.
	/// </summary>
	public string Value { get; }

	public static implicit operator string(PhoneNumber name) => name.Value;

	/// <summary>
	/// Creates a new <see cref="PhoneNumber"/> instance based on the specified value.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the value creation process containing the value or an error.</returns>	
	public static Result<PhoneNumber> Create(string value, string field, ISharedViewLocalizer sharedViewLocalizer) =>
		Result.Create(value, new Error(CaseConverter.PascalToSnakeCase(field), sharedViewLocalizer[DomainErrors.Item.NullOrEmptyError]))
			.Ensure(n => !string.IsNullOrWhiteSpace(n), new Error(CaseConverter.PascalToSnakeCase(field), sharedViewLocalizer[DomainErrors.Item.NullOrEmptyError]))
			.Ensure(n => n.Length <= MaxLength, new Error(CaseConverter.PascalToSnakeCase(field), string.Format(sharedViewLocalizer[DomainErrors.Item.LongerThanAllowed], MaxLength)))
			.Ensure(e => PhoneNumberFormatRegex.Value.IsMatch(e), new Error(CaseConverter.PascalToSnakeCase(field), sharedViewLocalizer[DomainErrors.Item.InvalidFormat]))
			.Map(f => new PhoneNumber(f));
	/// <inheritdoc />
	public override string ToString() => Value;

	/// <inheritdoc />
	protected override IEnumerable<object> GetAtomicValues()
	{
		yield return Value;
	}
}
