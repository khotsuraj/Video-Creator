using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KaraokeVideoCreator.UI.Converters
{
    public class BoolToVisConverter : IValueConverter
    {
        public bool IsInverted { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolVal = value is bool b && b;
            
            // Check parameter or property inversion
            string? paramStr = parameter as string;
            if (IsInverted || (paramStr != null && paramStr.Equals("Invert", StringComparison.OrdinalIgnoreCase)))
            {
                boolVal = !boolVal;
            }

            return boolVal ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
            {
                bool boolVal = vis == Visibility.Visible;
                string? paramStr = parameter as string;
                if (IsInverted || (paramStr != null && paramStr.Equals("Invert", StringComparison.OrdinalIgnoreCase)))
                {
                    boolVal = !boolVal;
                }
                return boolVal;
            }
            return false;
        }
    }
}
