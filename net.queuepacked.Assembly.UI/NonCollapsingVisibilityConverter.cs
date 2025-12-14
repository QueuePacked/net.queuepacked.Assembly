using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace net.queuepacked.Assembly.UI;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class NonCollapsingVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (bool)value ? Visibility.Visible : Visibility.Hidden;

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => ((Visibility)value) == Visibility.Visible;
}