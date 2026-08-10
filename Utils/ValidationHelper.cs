using System.Globalization;
using System.Text.RegularExpressions;

namespace InventoryManagementSystem.Utils;

public static partial class ValidationHelper
{
    public static bool IsValidUsername(string value) =>
        UsernamePattern().IsMatch(value.Trim());

    public static bool IsStrongPassword(string value) =>
        value.Length >= 8 &&
        value.Any(char.IsUpper) &&
        value.Any(char.IsLower) &&
        value.Any(char.IsDigit);

    public static bool TryParseNonNegativeDecimal(string value, out decimal result) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result) && result >= 0;

    public static bool TryParseNonNegativeInteger(string value, out int result) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result) && result >= 0;

    [GeneratedRegex("^[a-zA-Z0-9_.-]{3,50}$")]
    private static partial Regex UsernamePattern();
}
