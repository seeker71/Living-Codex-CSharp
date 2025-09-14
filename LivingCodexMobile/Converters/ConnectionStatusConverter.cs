using System.Globalization;

namespace LivingCodexMobile.Converters;

public class ConnectionStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? "🟢 Connected" : "🔴 Disconnected";
        }
        
        return "⚪ Unknown";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


