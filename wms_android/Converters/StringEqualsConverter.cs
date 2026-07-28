using System.Globalization;

namespace wms_android.Converters
{
    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string str || parameter is not string param)
                return false;
            var options = param.Split('|');
            foreach (var option in options)
            {
                if (string.Equals(str, option, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
