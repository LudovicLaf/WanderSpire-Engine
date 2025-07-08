using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace SceneEditor.Converters
{
    public class IntToStringFormatConverter : IValueConverter
    {
        // Usage: ConverterParameter="{0} Components"
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int intValue && parameter is string format)
            {
                return string.Format(format, intValue);
            }
            return value?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
