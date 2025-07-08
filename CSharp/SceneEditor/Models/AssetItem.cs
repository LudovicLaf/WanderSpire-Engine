using Avalonia.Media;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace SceneEditor.Models;

/// <summary>
/// Enhanced asset item model with modern UI properties
/// </summary>
public class AssetItem : ReactiveObject
{
    private string _name = string.Empty;
    private string _fullPath = string.Empty;
    private string _relativePath = string.Empty;
    private AssetType _type = AssetType.Unknown;
    private long _size = 0;
    private DateTime _lastModified = DateTime.MinValue;
    private AssetItem? _parent;
    private bool _isExpanded = false;
    private bool _isSelected = false;

    public ObservableCollection<AssetItem> Children { get; } = new();

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string FullPath
    {
        get => _fullPath;
        set => this.RaiseAndSetIfChanged(ref _fullPath, value);
    }

    public string RelativePath
    {
        get => _relativePath;
        set => this.RaiseAndSetIfChanged(ref _relativePath, value);
    }

    public AssetType Type
    {
        get => _type;
        set => this.RaiseAndSetIfChanged(ref _type, value);
    }

    public long Size
    {
        get => _size;
        set => this.RaiseAndSetIfChanged(ref _size, value);
    }

    public DateTime LastModified
    {
        get => _lastModified;
        set => this.RaiseAndSetIfChanged(ref _lastModified, value);
    }

    public AssetItem? Parent
    {
        get => _parent;
        set => this.RaiseAndSetIfChanged(ref _parent, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    // Computed Properties for UI
    public bool IsFolder => Type == AssetType.Folder;
    public bool HasChildren => Children.Count > 0;

    public string NameWithoutExtension
    {
        get
        {
            if (IsFolder) return Name;
            return Path.GetFileNameWithoutExtension(Name);
        }
    }

    public string Extension
    {
        get
        {
            if (IsFolder) return string.Empty;
            return Path.GetExtension(Name);
        }
    }

    public string FormattedSize
    {
        get
        {
            if (IsFolder) return string.Empty;

            return Size switch
            {
                < 1024 => $"{Size} B",
                < 1024 * 1024 => $"{Size / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{Size / (1024.0 * 1024.0):F1} MB",
                _ => $"{Size / (1024.0 * 1024.0 * 1024.0):F1} GB"
            };
        }
    }

    public string Icon
    {
        get
        {
            return Type switch
            {
                AssetType.Folder => "\uf07b",      // folder
                AssetType.Scene => "\uf1b2",       // cube
                AssetType.Prefab => "\uf1b3",      // cubes
                AssetType.Texture => "\uf03e",     // image
                AssetType.Audio => "\uf001",       // music
                AssetType.Font => "\uf031",        // font
                AssetType.Script => "\uf121",      // code
                AssetType.Shader => "\uf06d",      // fire
                AssetType.Text => "\uf15c",        // file-text
                _ => "\uf15b"                      // file
            };
        }
    }

    public string TypeIcon
    {
        get
        {
            return Type switch
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
    }

    public IBrush TypeColor
    {
        get
        {
            return Type switch
            {
                AssetType.Folder => Brushes.Orange,
                AssetType.Scene => Brushes.Blue,
                AssetType.Prefab => Brushes.Purple,
                AssetType.Texture => Brushes.Green,
                AssetType.Audio => Brushes.Red,
                AssetType.Font => Brushes.Brown,
                AssetType.Script => Brushes.DarkBlue,
                AssetType.Shader => Brushes.DarkRed,
                AssetType.Text => Brushes.Gray,
                _ => Brushes.DarkGray
            };
        }
    }

    public int Depth
    {
        get
        {
            int depth = 0;
            var current = Parent;
            while (current != null)
            {
                depth++;
                current = current.Parent;
            }
            return depth;
        }
    }

    public string ToolTipText
    {
        get
        {
            var tooltip = $"Name: {Name}\nType: {Type}";

            if (!IsFolder)
            {
                tooltip += $"\nSize: {FormattedSize}";
            }

            tooltip += $"\nPath: {RelativePath}";
            tooltip += $"\nModified: {LastModified:yyyy-MM-dd HH:mm:ss}";

            return tooltip;
        }
    }

    public bool CanHaveChildren => IsFolder;

    public override string ToString()
    {
        return $"{Name} ({Type})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is AssetItem other)
        {
            return string.Equals(RelativePath, other.RelativePath, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return RelativePath?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
    }
}

/// <summary>
/// Asset type enumeration
/// </summary>
public enum AssetType
{
    Unknown,
    Folder,
    Scene,
    Prefab,
    Texture,
    Audio,
    Font,
    Script,
    Shader,
    Text
}