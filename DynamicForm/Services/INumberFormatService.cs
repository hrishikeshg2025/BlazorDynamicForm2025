namespace DynamicForm.Services;

public interface INumberFormatService
{
    string FormatDecimal(decimal value, int decimalPlaces);
    string FormatDecimalForDisplay(decimal value, int decimalPlaces);
    decimal? ParseDecimal(string input);
    bool IsValidDecimal(string input, int maxDecimalPlaces);
}
