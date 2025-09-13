using System.Globalization;

namespace LivingCodexMobile.Converters
{
    public class BoolToButtonTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isLoginMode)
            {
                return isLoginMode ? "Login" : "Register";
            }
            return "Login";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

