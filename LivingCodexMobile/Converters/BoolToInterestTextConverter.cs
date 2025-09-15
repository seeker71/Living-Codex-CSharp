using System.Globalization;

namespace LivingCodexMobile.Converters;

public class BoolToInterestTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isInterested)
        {
            return isInterested ? "Interested ✓" : "Mark Interest";
        }
        return "Mark Interest";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return text.Contains("✓");
        }
        return false;
    }
}
