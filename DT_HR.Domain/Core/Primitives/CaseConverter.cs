using System.Text.RegularExpressions;

namespace DT_HR.Domain.Core.Primitives;

public static class CaseConverter
{
	public static string PascalToSnakeCase(string value)
	{
		if (string.IsNullOrEmpty(value))
			return value;

		return Regex.Replace(
			value,
			"(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])",
			"_$1",
			RegexOptions.Compiled)
			.Trim()
			.ToLower();
	}
}
