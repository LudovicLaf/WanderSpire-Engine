using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

namespace SceneEditor.Models;

/// <summary>
/// Represents a prefab definition that can be edited and saved
/// </summary>
public class PrefabDefinition : ReactiveObject
{
    private string _name = "NewPrefab";
    private bool _isDirty = false;
    private ObservableCollection<ComponentItem> _componentItems = new();

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public int PrefabId { get; set; }

    public Dictionary<string, object> Components { get; set; } = new();

    public bool IsDirty
    {
        get => _isDirty;
        private set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    /// <summary>
    /// Observable collection of component items for UI binding
    /// </summary>
    public ObservableCollection<ComponentItem> ComponentItems
    {
        get => _componentItems;
        private set => this.RaiseAndSetIfChanged(ref _componentItems, value);
    }

    /// <summary>
    /// Mark the prefab as dirty (needs saving)
    /// </summary>
    public void MarkDirty()
    {
        IsDirty = true;
    }

    /// <summary>
    /// Mark the prefab as clean (saved)
    /// </summary>
    public void MarkClean()
    {
        IsDirty = false;
    }

    /// <summary>
    /// Refresh the component items collection
    /// </summary>
    public void RefreshComponentItems()
    {
        ComponentItems.Clear();
        foreach (var kvp in Components)
        {
            var item = new ComponentItem
            {
                Type = kvp.Key,
                Data = kvp.Value,
                Parent = this
            };
            ComponentItems.Add(item);
        }
    }

    /// <summary>
    /// Add a component to the prefab
    /// </summary>
    public void AddComponent(string componentType, object componentData)
    {
        Components[componentType] = componentData;
        RefreshComponentItems();
        MarkDirty();
    }

    /// <summary>
    /// Remove a component from the prefab
    /// </summary>
    public void RemoveComponent(string componentType)
    {
        if (IsRequiredComponent(componentType))
        {
            Console.WriteLine($"Cannot remove required component: {componentType}");
            return;
        }

        if (Components.Remove(componentType))
        {
            RefreshComponentItems();
            MarkDirty();
        }
    }

    /// <summary>
    /// Check if a component is required and cannot be removed
    /// </summary>
    public bool IsRequiredComponent(string componentType)
    {
        return componentType is "PrefabIdComponent" or "TagComponent" or "GridPositionComponent" or "TransformComponent";
    }

    /// <summary>
    /// Get component data as JSON string
    /// </summary>
    public string? GetComponentJson(string componentType)
    {
        if (Components.TryGetValue(componentType, out var component))
        {
            try
            {
                return JsonSerializer.Serialize(component, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to serialize component {componentType}: {ex.Message}");
            }
        }
        return null;
    }

    /// <summary>
    /// Set component data from JSON string
    /// </summary>
    public void SetComponentJson(string componentType, string json)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            Components[componentType] = element;
            RefreshComponentItems();
            MarkDirty();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to deserialize component {componentType}: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if the prefab has a specific component
    /// </summary>
    public bool HasComponent(string componentType)
    {
        return Components.ContainsKey(componentType);
    }

    /// <summary>
    /// Get all component type names
    /// </summary>
    public string[] GetComponentTypes()
    {
        return Components.Keys.ToArray();
    }

    /// <summary>
    /// Convert to the JSON format expected by the game engine
    /// </summary>
    public object ToJsonFormat()
    {
        return new
        {
            name = Name,
            components = Components
        };
    }

    /// <summary>
    /// Create a PrefabDefinition from a JSON element
    /// </summary>
    public static PrefabDefinition? FromJsonElement(JsonElement element)
    {
        try
        {
            if (!element.TryGetProperty("name", out var nameProperty))
                return null;

            var prefab = new PrefabDefinition
            {
                Name = nameProperty.GetString() ?? "Unknown"
            };

            // Try to get prefab ID from PrefabIdComponent
            if (element.TryGetProperty("components", out var componentsProperty) &&
                componentsProperty.TryGetProperty("PrefabIdComponent", out var prefabIdComponent) &&
                prefabIdComponent.TryGetProperty("prefabId", out var prefabIdProperty))
            {
                prefab.PrefabId = prefabIdProperty.GetInt32();
            }

            // Load all components
            if (element.TryGetProperty("components", out var components))
            {
                foreach (var componentProperty in components.EnumerateObject())
                {
                    prefab.Components[componentProperty.Name] = componentProperty.Value;
                }
            }

            prefab.RefreshComponentItems();
            prefab.MarkClean();
            return prefab;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to create PrefabDefinition from JSON: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Create a copy of this prefab with a new name
    /// </summary>
    public PrefabDefinition Clone(string newName)
    {
        var clone = new PrefabDefinition
        {
            Name = newName,
            PrefabId = PrefabId + 1000, // Simple ID generation
            Components = new Dictionary<string, object>()
        };

        // Deep copy components
        foreach (var kvp in Components)
        {
            try
            {
                var json = JsonSerializer.Serialize(kvp.Value);
                var element = JsonSerializer.Deserialize<JsonElement>(json);
                clone.Components[kvp.Key] = element;
            }
            catch
            {
                clone.Components[kvp.Key] = kvp.Value;
            }
        }

        // Update the PrefabIdComponent
        if (clone.Components.ContainsKey("PrefabIdComponent"))
        {
            clone.Components["PrefabIdComponent"] = new
            {
                prefabId = clone.PrefabId,
                prefabName = clone.Name
            };
        }

        // Update the TagComponent
        if (clone.Components.ContainsKey("TagComponent"))
        {
            clone.Components["TagComponent"] = new
            {
                tag = clone.Name
            };
        }

        clone.RefreshComponentItems();
        clone.MarkDirty();
        return clone;
    }

    public override string ToString() => Name;
}

/// <summary>
/// Represents a component item for UI display
/// </summary>
public class ComponentItem : ReactiveObject
{
    private string _type = string.Empty;
    private object _data = new();
    private bool _isExpanded = false;

    public string Type
    {
        get => _type;
        set => this.RaiseAndSetIfChanged(ref _type, value);
    }

    public object Data
    {
        get => _data;
        set => this.RaiseAndSetIfChanged(ref _data, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public PrefabDefinition? Parent { get; set; }

    public bool IsRequired => Parent?.IsRequiredComponent(Type) ?? false;

    public string FormattedJson
    {
        get
        {
            try
            {
                return JsonSerializer.Serialize(Data, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return "{}";
            }
        }
        set
        {
            try
            {
                var element = JsonSerializer.Deserialize<JsonElement>(value);
                Data = element;
                if (Parent != null)
                {
                    Parent.Components[Type] = element;
                    Parent.MarkDirty();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to parse component JSON: {ex.Message}");
            }
        }
    }
}