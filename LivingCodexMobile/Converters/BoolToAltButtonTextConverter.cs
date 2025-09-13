using System.Globalization;

namespace LivingCodexMobile.Converters
{
    public class BoolToAltButtonTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isLoginMode)
            {
                return isLoginMode ? "Don't have an account? Register" : "Already have an account? Login";
            }
            return "Don't have an account? Register";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

