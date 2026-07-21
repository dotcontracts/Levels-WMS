using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using wms_android.shared.Models;

namespace wms_android.Converters
{
    public class UserFullNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is User user)
            {
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                return string.IsNullOrEmpty(fullName) ? (parameter?.ToString() ?? "—") : fullName;
            }
            return parameter?.ToString() ?? "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
