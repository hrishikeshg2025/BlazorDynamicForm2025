using System.Globalization;

namespace DynamicForm.Services;

public class NumberFormatService : INumberFormatService
{
    public string FormatDecimal(decimal value, int decimalPlaces)
    {
        // Format for storage/calculation - use full precision
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public string FormatDecimalForDisplay(decimal value, int decimalPlaces)
    {
        // Format for display with specified decimal places
        return value.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
    }

    public decimal? ParseDecimal(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        return null;
    }

    public bool IsValidDecimal(string input, int maxDecimalPlaces)
    {
        if (string.IsNullOrWhiteSpace(input))
            return true;

        if (!decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            return false;

        // Check decimal places
        if (input.Contains('.'))
        {
            var decimalPart = input.Split('.')[1];
            if (decimalPart.Length > maxDecimalPlaces)
                return false;
        }

        return true;
    }
}
