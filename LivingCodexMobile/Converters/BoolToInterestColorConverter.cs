using System.Globalization;

namespace LivingCodexMobile.Converters;

public class BoolToInterestColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isInterested)
        {
            return isInterested ? Colors.Green : Colors.LightBlue;
        }
        return Colors.LightBlue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
