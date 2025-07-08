// File: CSharp/SceneEditor/Converters/EssentialConverters.cs
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SceneEditor.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SceneEditor.Converters
{
    /// <summary>
    /// Converts boolean to FontWeight (Bold/Normal)
    /// </summary>
    public class BoolToFontWeightConverter : IValueConverter
    {
        public static readonly BoolToFontWeightConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? FontWeight.Bold : FontWeight.Normal;
            return FontWeight.Normal;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts boolean to opacity (1.0/0.5)
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        public static readonly BoolToOpacityConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? 1.0 : 0.5;
            return 1.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts depth (int) to left margin for tree indentation
    /// </summary>
    public class DepthToMarginConverter : IValueConverter
    {
        public static readonly DepthToMarginConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int depth)
            {
                var indentSize = 16; // 16 pixels per level
                return new Thickness(depth * indentSize, 0, 0, 0);
            }
            return new Thickness(0);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Checks if a number is greater than zero
    /// </summary>
    public class GreaterThanZeroConverter : IValueConverter
    {
        public static readonly GreaterThanZeroConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int i)
                return i > 0;
            if (value is double d)
                return d > 0;
            if (value is float f)
                return f > 0;
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts file size in bytes to human readable format
    /// </summary>
    public class FileSizeConverter : IValueConverter
    {
        public static readonly FileSizeConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return bytes switch
                {
                    < 1024 => $"{bytes} B",
                    < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
                    < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
                    _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
                };
            }
            return "0 B";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Multi-value converter for logical AND operation
    /// </summary>
    public class MultiBooleanAndConverter : IMultiValueConverter
    {
        public static readonly MultiBooleanAndConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is bool b && !b)
                    return false;
                if (value is not bool)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Multi-value converter for logical OR operation
    /// </summary>
    public class MultiBooleanOrConverter : IMultiValueConverter
    {
        public static readonly MultiBooleanOrConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is bool b && b)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts entity type to appropriate icon
    /// </summary>
    public class EntityTypeToIconConverter : IValueConverter
    {
        public static readonly EntityTypeToIconConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string entityType)
            {
                return entityType.ToLower() switch
                {
                    "sprite" => "\uf03e", // image icon
                    "text" => "\uf031", // font icon
                    "audio" => "\uf001", // music icon
                    "light" => "\uf0eb", // lightbulb icon
                    "camera" => "\uf030", // camera icon
                    "ui" => "\uf2d0", // window icon
                    "empty" => "\uf1b2", // cube outline
                    _ => "\uf1b2" // default cube icon
                };
            }
            return "\uf1b2";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts entity type to appropriate color
    /// </summary>
    public class EntityTypeToColorConverter : IValueConverter
    {
        public static readonly EntityTypeToColorConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string entityType)
            {
                return entityType.ToLower() switch
                {
                    "sprite" => Brushes.LightBlue,
                    "text" => Brushes.LightGreen,
                    "audio" => Brushes.Orange,
                    "light" => Brushes.Yellow,
                    "camera" => Brushes.Purple,
                    "ui" => Brushes.Pink,
                    "empty" => Brushes.Gray,
                    _ => Brushes.White
                };
            }
            return Brushes.White;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? true : false; // Avalonia uses bool for IsVisible
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }
            return false;
        }
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }
    }

    /// <summary>
    /// Converts null values to visibility
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts object to string with null fallback
    /// </summary>
    public class ObjectToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return parameter?.ToString() ?? string.Empty;
            }
            return value.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString();
        }
    }

    /// <summary>
    /// Converts enum values to display strings
    /// </summary>
    public class EnumToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Enum enumValue)
            {
                return enumValue.ToString();
            }
            return value?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string stringValue && targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, stringValue);
                }
                catch
                {
                    return Enum.GetValues(targetType).GetValue(0);
                }
            }
            return value;
        }
    }

    /// <summary>
    /// Converts collections count to visibility
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is System.Collections.ICollection collection)
            {
                return collection.Count > 0;
            }
            if (value is int count)
            {
                return count > 0;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts PanelMode enum to friendly strings
    /// </summary>
    public class PanelModeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ViewModels.PanelMode mode)
            {
                return mode switch
                {
                    ViewModels.PanelMode.Scene => "Scene",
                    ViewModels.PanelMode.Prefab => "Prefab",
                    _ => value.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return stringValue switch
                {
                    "Scene" => ViewModels.PanelMode.Scene,
                    "Prefab" => ViewModels.PanelMode.Prefab,
                    _ => ViewModels.PanelMode.Scene
                };
            }
            return ViewModels.PanelMode.Scene;
        }
    }

    /// <summary>
    /// Converts boolean values to colors for status indicators
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Green" : "Red";
            }
            return "Gray";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// String utilities for converters
    /// </summary>
    public static class StringConverters
    {
        public static readonly IValueConverter IsNullOrEmpty =
            new FuncValueConverter<string?, bool>(s => string.IsNullOrEmpty(s));

        public static readonly IValueConverter IsNotNullOrEmpty =
            new FuncValueConverter<string?, bool>(s => !string.IsNullOrEmpty(s));

        public static readonly IValueConverter FromBoolean = BooleanToStringParameterConverter.Instance;
    }

    public class EngineStatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isInitialized)
            {
                return isInitialized ? "Engine Ready" : "Initializing...";
            }
            return "Unknown";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts AssetType to appropriate Font Awesome icon name
    /// </summary>
    public class AssetTypeToIconConverter : IValueConverter
    {
        public static readonly AssetTypeToIconConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AssetType assetType)
            {
                return assetType switch
                {
                    AssetType.Folder => "fa-folder",
                    AssetType.Scene => "fa-cube",
                    AssetType.Prefab => "fa-cubes",
                    AssetType.Texture => "fa-image",
                    AssetType.Audio => "fa-music",
                    AssetType.Font => "fa-font",
                    AssetType.Script => "fa-code",
                    AssetType.Shader => "fa-fire",
                    AssetType.Text => "fa-file-alt",
                    _ => "fa-file"
                };
            }
            return "fa-file";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts AssetType to appropriate color brush
    /// </summary>
    public class AssetTypeToColorConverter : IValueConverter
    {
        public static readonly AssetTypeToColorConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AssetType assetType)
            {
                return assetType switch
                {
                    AssetType.Folder => new SolidColorBrush(Color.FromRgb(255, 193, 7)),     // Orange/Yellow
                    AssetType.Scene => new SolidColorBrush(Color.FromRgb(13, 110, 253)),     // Blue
                    AssetType.Prefab => new SolidColorBrush(Color.FromRgb(111, 66, 193)),    // Purple
                    AssetType.Texture => new SolidColorBrush(Color.FromRgb(25, 135, 84)),    // Green
                    AssetType.Audio => new SolidColorBrush(Color.FromRgb(220, 53, 69)),      // Red
                    AssetType.Font => new SolidColorBrush(Color.FromRgb(102, 77, 3)),        // Brown
                    AssetType.Script => new SolidColorBrush(Color.FromRgb(13, 202, 240)),    // Cyan
                    AssetType.Shader => new SolidColorBrush(Color.FromRgb(214, 51, 132)),    // Pink
                    AssetType.Text => new SolidColorBrush(Color.FromRgb(108, 117, 125)),     // Gray
                    _ => new SolidColorBrush(Color.FromRgb(73, 80, 87))                      // Dark gray
                };
            }
            return new SolidColorBrush(Color.FromRgb(73, 80, 87));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts boolean folder state to expand/collapse icon
    /// </summary>
    public class FolderExpandIconConverter : IValueConverter
    {
        public static readonly FolderExpandIconConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
            {
                return isExpanded ? "fa-chevron-down" : "fa-chevron-right";
            }
            return "fa-chevron-right";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts asset item to display name (with truncation if needed)
    /// </summary>
    public class AssetDisplayNameConverter : IValueConverter
    {
        public static readonly AssetDisplayNameConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AssetItem asset)
            {
                var maxLength = parameter is string paramStr && int.TryParse(paramStr, out var len) ? len : 50;

                if (asset.Name.Length <= maxLength)
                    return asset.Name;

                return asset.Name.Substring(0, maxLength - 3) + "...";
            }
            return value?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts asset type to user-friendly display name
    /// </summary>
    public class AssetTypeDisplayConverter : IValueConverter
    {
        public static readonly AssetTypeDisplayConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AssetType assetType)
            {
                return assetType switch
                {
                    AssetType.Folder => "Folder",
                    AssetType.Scene => "Scene",
                    AssetType.Prefab => "Prefab",
                    AssetType.Texture => "Image",
                    AssetType.Audio => "Audio",
                    AssetType.Font => "Font",
                    AssetType.Script => "Script",
                    AssetType.Shader => "Shader",
                    AssetType.Text => "Text",
                    AssetType.Unknown => "Unknown",
                    _ => assetType.ToString()
                };
            }
            return "Unknown";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string displayName)
            {
                return displayName switch
                {
                    "Folder" => AssetType.Folder,
                    "Scene" => AssetType.Scene,
                    "Prefab" => AssetType.Prefab,
                    "Image" => AssetType.Texture,
                    "Audio" => AssetType.Audio,
                    "Font" => AssetType.Font,
                    "Script" => AssetType.Script,
                    "Shader" => AssetType.Shader,
                    "Text" => AssetType.Text,
                    _ => AssetType.Unknown
                };
            }
            return AssetType.Unknown;
        }
    }

    /// <summary>
    /// Converts asset selection state to appropriate styling
    /// </summary>
    public class AssetSelectionToBrushConverter : IValueConverter
    {
        public static readonly AssetSelectionToBrushConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                return isSelected
                    ? new SolidColorBrush(Color.FromRgb(0, 120, 215))  // Selection blue
                    : Brushes.Transparent;
            }
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts time to relative time string (e.g., "2 hours ago")
    /// </summary>
    public class RelativeTimeConverter : IValueConverter
    {
        public static readonly RelativeTimeConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var now = DateTime.Now;
                var diff = now - dateTime;

                if (diff.TotalDays > 365)
                    return $"{(int)(diff.TotalDays / 365)} year{((int)(diff.TotalDays / 365) != 1 ? "s" : "")} ago";
                if (diff.TotalDays > 30)
                    return $"{(int)(diff.TotalDays / 30)} month{((int)(diff.TotalDays / 30) != 1 ? "s" : "")} ago";
                if (diff.TotalDays > 1)
                    return $"{(int)diff.TotalDays} day{((int)diff.TotalDays != 1 ? "s" : "")} ago";
                if (diff.TotalHours > 1)
                    return $"{(int)diff.TotalHours} hour{((int)diff.TotalHours != 1 ? "s" : "")} ago";
                if (diff.TotalMinutes > 1)
                    return $"{(int)diff.TotalMinutes} minute{((int)diff.TotalMinutes != 1 ? "s" : "")} ago";

                return "Just now";
            }
            return "Unknown";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts asset depth to indentation margin
    /// </summary>
    public class AssetDepthToMarginConverter : IValueConverter
    {
        public static readonly AssetDepthToMarginConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int depth)
            {
                var indentSize = parameter is string paramStr && int.TryParse(paramStr, out var size) ? size : 16;
                return new Avalonia.Thickness(depth * indentSize, 0, 0, 0);
            }
            return new Avalonia.Thickness(0);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts asset browser view mode to boolean
    /// </summary>
    public class ViewModeToBooleanConverter : IValueConverter
    {
        public static readonly ViewModeToBooleanConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ViewModels.AssetBrowserView currentView && parameter is string targetView)
            {
                return string.Equals(currentView.ToString(), targetView, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected && parameter is string viewMode)
            {
                return Enum.TryParse<ViewModels.AssetBrowserView>(viewMode, true, out var result)
                    ? result
                    : ViewModels.AssetBrowserView.Tree;
            }
            return ViewModels.AssetBrowserView.Tree;
        }
    }

    /// <summary>
    /// Numeric converters
    /// </summary>
    public static class NumericConverters
    {
        public static readonly IValueConverter ToFileSize = FileSizeConverter.Instance;
        public static readonly IValueConverter GreaterThanZero = GreaterThanZeroConverter.Instance;
    }

    /// <summary>
    /// UI converters
    /// </summary>
    public static class UIConverters
    {
        public static readonly IValueConverter BoolToFontWeight = BoolToFontWeightConverter.Instance;
        public static readonly IValueConverter BoolToOpacity = BoolToOpacityConverter.Instance;
        public static readonly IValueConverter DepthToMargin = DepthToMarginConverter.Instance;
        public static readonly IValueConverter EntityTypeToIcon = EntityTypeToIconConverter.Instance;
        public static readonly IValueConverter EntityTypeToColor = EntityTypeToColorConverter.Instance;
    }

    /// <summary>
    /// Multi-value converters
    /// </summary>
    public static class MultiConverters
    {
        public static readonly IMultiValueConverter BooleanAnd = MultiBooleanAndConverter.Instance;
        public static readonly IMultiValueConverter BooleanOr = MultiBooleanOrConverter.Instance;
    }

    /// <summary>
    /// Multi-binding converter for string formatting
    /// </summary>
    public class StringFormatMultiConverter : IMultiValueConverter
    {
        public static readonly StringFormatMultiConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is string format && values.Count > 0)
            {
                var args = values.Where(v => v != null).ToArray();
                try
                {
                    return string.Format(format, args);
                }
                catch
                {
                    return format;
                }
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// Converts any object to its string representation with null handling
    /// </summary>
    public class SafeStringConverter : IValueConverter
    {
        public static readonly SafeStringConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return parameter?.ToString() ?? string.Empty;

            return value.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString();
        }
    }

    /// <summary>
    /// Converts collection count to boolean (true if count > 0)
    /// </summary>
    public class CollectionCountToBoolConverter : IValueConverter
    {
        public static readonly CollectionCountToBoolConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is System.Collections.ICollection collection)
                return collection.Count > 0;

            if (value is int count)
                return count > 0;

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Inverts a boolean value
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public static readonly InverseBooleanConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return false;
        }
    }

    /// <summary>
    /// Enhanced boolean to string converter with multiple option support
    /// </summary>
    public class BooleanToStringAdvancedConverter : IValueConverter
    {
        public static readonly BooleanToStringAdvancedConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string parameterString)
            {
                var parts = parameterString.Split('|');
                if (parts.Length >= 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
                if (parts.Length == 1)
                {
                    return boolValue ? parts[0] : string.Empty;
                }
            }
            return value?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is string parameterString && value is string stringValue)
            {
                var parts = parameterString.Split('|');
                if (parts.Length >= 2)
                {
                    return stringValue == parts[0];
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Converts double values to percentage strings
    /// </summary>
    public class DoubleToPercentageConverter : IValueConverter
    {
        public static readonly DoubleToPercentageConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                var percentage = d * 100;
                var decimals = parameter is string s && int.TryParse(s, out var dec) ? dec : 0;
                return $"{percentage.ToString($"F{decimals}")}%";
            }
            return "0%";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s && s.EndsWith("%"))
            {
                var numberPart = s.Substring(0, s.Length - 1);
                if (double.TryParse(numberPart, out var result))
                {
                    return result / 100.0;
                }
            }
            return 0.0;
        }
    }

    /// <summary>
    /// Math converters for common operations
    /// </summary>
    public static class MathConverters
    {
        public static readonly IValueConverter Add = new FuncValueConverter<double, double>(x => x + 1);
        public static readonly IValueConverter Multiply = new MultiplierConverter();
    }

    public class MultiplierConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d && parameter is string s && double.TryParse(s, out var multiplier))
            {
                return d * multiplier;
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d && parameter is string s && double.TryParse(s, out var multiplier) && multiplier != 0)
            {
                return d / multiplier;
            }
            return value;
        }
    }
}

public class BoolToColorConverter : IValueConverter
{
    public IBrush TrueBrush { get; set; } = new SolidColorBrush(Colors.LimeGreen);
    public IBrush FalseBrush { get; set; } = new SolidColorBrush(Colors.Gray);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueBrush : FalseBrush;
        }
        return FalseBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converter for engine status to string
/// </summary>
public class EngineStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isInitialized)
        {
            return isInitialized ? "Ready" : "Loading";
        }
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converter to check if a value is greater than zero
/// </summary>
public class GreaterThanZeroConverter : IValueConverter
{
    public static readonly GreaterThanZeroConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 0;
        }
        if (value is double doubleValue)
        {
            return doubleValue > 0;
        }
        if (value is float floatValue)
        {
            return floatValue > 0;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

// Extension class for easy access to asset converters
namespace SceneEditor.Converters
{
    public static class AssetConverters
    {
        public static readonly IValueConverter TypeToIcon = AssetTypeToIconConverter.Instance;
        public static readonly IValueConverter TypeToColor = AssetTypeToColorConverter.Instance;
        public static readonly IValueConverter FolderExpandIcon = FolderExpandIconConverter.Instance;
        public static readonly IValueConverter DisplayName = AssetDisplayNameConverter.Instance;
        public static readonly IValueConverter TypeDisplay = AssetTypeDisplayConverter.Instance;
        public static readonly IValueConverter SelectionToBrush = AssetSelectionToBrushConverter.Instance;
        public static readonly IValueConverter RelativeTime = RelativeTimeConverter.Instance;
        public static readonly IValueConverter DepthToMargin = AssetDepthToMarginConverter.Instance;
        public static readonly IValueConverter ViewModeToBoolean = ViewModeToBooleanConverter.Instance;
    }
}

namespace SceneEditor.Converters
{
    public static class ConverterExtensions
    {
        // Common converters
        public static readonly IValueConverter SafeString = SafeStringConverter.Instance;
        public static readonly IValueConverter CollectionCountToBool = CollectionCountToBoolConverter.Instance;
        public static readonly IValueConverter InverseBool = InverseBooleanConverter.Instance;
        public static readonly IValueConverter BoolToStringAdvanced = BooleanToStringAdvancedConverter.Instance;
        public static readonly IValueConverter DoubleToPercentage = DoubleToPercentageConverter.Instance;

        // Multi-value converters
        public static readonly IMultiValueConverter StringFormatMulti = StringFormatMultiConverter.Instance;
    }
}