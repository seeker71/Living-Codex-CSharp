using System.Globalization;

namespace LivingCodexMobile.Converters;

public class BoolToLoginTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isLoggedIn)
        {
            return isLoggedIn ? "Logout" : "Login";
        }
        return "Login";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
