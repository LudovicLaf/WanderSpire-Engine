using ReactiveUI;
using System;
using System.Text.Json;
using WanderSpire.Scripting;

namespace SceneEditor.ViewModels.ComponentEditors;

/// <summary>
/// Base class for all component editors in the inspector
/// </summary>
public abstract class ComponentEditorViewModel : ReactiveObject
{
    private bool _isExpanded = true;
    private bool _isEnabled = true;
    protected readonly Entity _entity;

    public string ComponentType { get; }
    public abstract string DisplayName { get; }
    public virtual string Description => string.Empty;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    protected ComponentEditorViewModel(string componentType, Entity entity)
    {
        ComponentType = componentType;
        _entity = entity;

        LoadFromEntity();
    }

    /// <summary>
    /// Load component data from the entity
    /// </summary>
    public virtual void LoadFromEntity()
    {
        if (!_entity.IsValid || !_entity.HasComponent(ComponentType))
            return;

        try
        {
            var json = _entity.GetComponent(ComponentType);
            if (!string.IsNullOrEmpty(json))
            {
                LoadFromJson(json);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load {ComponentType}: {ex.Message}");
        }
    }

    /// <summary>
    /// Save component data to the entity
    /// </summary>
    public virtual void SaveToEntity()
    {
        if (!_entity.IsValid)
            return;

        try
        {
            var json = SaveToJson();
            _entity.SetComponent(ComponentType, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to save {ComponentType}: {ex.Message}");
        }
    }

    /// <summary>
    /// Refresh the editor from the current entity state
    /// </summary>
    public virtual void RefreshFromEntity()
    {
        LoadFromEntity();
    }

    /// <summary>
    /// Load component properties from JSON
    /// </summary>
    protected abstract void LoadFromJson(string json);

    /// <summary>
    /// Save component properties to JSON
    /// </summary>
    protected abstract string SaveToJson();

    /// <summary>
    /// Helper to get a property value from JSON
    /// </summary>
    protected T GetJsonProperty<T>(JsonElement element, string propertyName, T defaultValue = default!)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            try
            {
                return JsonSerializer.Deserialize<T>(property.GetRawText());
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Helper to parse a JSON string safely
    /// </summary>
    protected JsonElement ParseJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch
        {
            return default;
        }
    }
}

/// <summary>
/// Generic component editor for unknown component types
/// </summary>
public class GenericComponentEditor : ComponentEditorViewModel
{
    private string _jsonText = string.Empty;

    public override string DisplayName => ComponentType;
    public override string Description => "Generic component editor";

    public string JsonText
    {
        get => _jsonText;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _jsonText, value);
            if (oldValue != value)
            {
                SaveToEntity();
            }
        }
    }


    public GenericComponentEditor(string componentType, Entity entity)
        : base(componentType, entity)
    {
    }

    protected override void LoadFromJson(string json)
    {
        _jsonText = FormatJson(json);
        this.RaisePropertyChanged(nameof(JsonText));
    }

    protected override string SaveToJson()
    {
        return _jsonText;
    }

    private string FormatJson(string json)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            return JsonSerializer.Serialize(element, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return json;
        }
    }
}