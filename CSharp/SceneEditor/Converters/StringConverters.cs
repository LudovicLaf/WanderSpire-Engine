// File: CSharp/SceneEditor/Converters/StringConverters.cs
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace SceneEditor.Converters
{
    public class BooleanToStringParameterConverter : IValueConverter
    {
        public static readonly BooleanToStringParameterConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b && parameter is string paramStr)
            {
                var parts = paramStr.Split('|');
                if (parts.Length == 2)
                    return b ? parts[0] : parts[1];
            }
            return value?.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
