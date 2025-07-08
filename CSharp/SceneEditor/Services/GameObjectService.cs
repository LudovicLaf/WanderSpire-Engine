using SceneEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using WanderSpire.Components;
using WanderSpire.Scripting;
using static WanderSpire.Scripting.EngineInterop;

namespace SceneEditor.Services;

/// <summary>
/// Unified service that manages both GameObjects in the scene and prefab definitions
/// Replaces both SceneService and PrefabEditorService with a Unity-like approach
/// </summary>
public class GameObjectService
{
    private readonly EditorEngine _engine;
    private readonly AssetService _assetService;
    private readonly ObservableCollection<SceneNode> _rootGameObjects = new();
    private readonly Dictionary<string, PrefabDefinition> _prefabLibrary = new();
    private string? _currentScenePath;
    private int _nextPrefabId = 1000;

    public IReadOnlyList<SceneNode> RootGameObjects => _rootGameObjects;
    public IReadOnlyCollection<PrefabDefinition> PrefabLibrary => _prefabLibrary.Values;
    public string? CurrentScenePath => _currentScenePath;
    public bool HasUnsavedChanges { get; private set; }

    public event EventHandler? SceneChanged;
    public event EventHandler? HierarchyChanged;
    public event EventHandler<SceneNode>? GameObjectSelected;
    public event EventHandler? PrefabLibraryChanged;

    public GameObjectService(EditorEngine engine, AssetService assetService)
    {
        _engine = engine;
        _assetService = assetService;

        // Subscribe to asset changes for prefab hot-reload
        _assetService.PrefabChanged += OnPrefabFileChanged;
        _assetService.PrefabAdded += OnPrefabFileAdded;
        _assetService.PrefabRemoved += OnPrefabFileRemoved;
    }

    #region Scene Management

    /// <summary>
    /// Create a new empty scene
    /// </summary>
    public void NewScene()
    {
        _rootGameObjects.Clear();
        _currentScenePath = null;
        HasUnsavedChanges = false;

        SceneChanged?.Invoke(this, EventArgs.Empty);
        HierarchyChanged?.Invoke(this, EventArgs.Empty);
        Console.WriteLine("[GameObject] Created new scene");
    }

    /// <summary>
    /// Load a scene from file
    /// </summary>
    public bool LoadScene(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"[GameObject] Scene file not found: {filePath}");
            return false;
        }

        try
        {
            _rootGameObjects.Clear();

            bool success = SceneManager_LoadScene(
                _engine.Context,
                filePath,
                out uint playerId,
                out float playerX,
                out float playerY,
                out uint tilemapId);

            if (!success)
            {
                Console.Error.WriteLine($"[GameObject] Failed to load scene: {filePath}");
                return false;
            }

            RefreshHierarchy();
            _currentScenePath = filePath;
            HasUnsavedChanges = false;

            SceneChanged?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"[GameObject] Scene loaded: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] Error loading scene: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Save the current scene
    /// </summary>
    public bool SaveScene(string? filePath = null)
    {
        string targetPath = filePath ?? _currentScenePath;
        if (string.IsNullOrEmpty(targetPath))
        {
            Console.Error.WriteLine("[GameObject] No file path specified for save");
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            SceneManager_SaveScene(_engine.Context, targetPath);

            _currentScenePath = targetPath;
            HasUnsavedChanges = false;

            Console.WriteLine($"[GameObject] Scene saved: {targetPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] Error saving scene: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Refresh the hierarchy from the engine
    /// </summary>
    public void RefreshHierarchy()
    {
        _rootGameObjects.Clear();

        var entities = _engine.GetAllEntities();
        var nodeMap = new Dictionary<int, SceneNode>();

        // First pass: create all nodes
        foreach (var entity in entities)
        {
            var node = CreateGameObjectNode(entity);
            nodeMap[entity.Id] = node;
        }

        // Second pass: build hierarchy
        const int maxChildren = 256;
        var childBuffer = new uint[maxChildren];

        foreach (var kvp in nodeMap)
        {
            var entityId = new EntityId { id = (uint)kvp.Key };
            var node = kvp.Value;

            int childCount = SceneHierarchy_GetChildren(_engine.Context, entityId, childBuffer, maxChildren);
            for (int i = 0; i < childCount; i++)
            {
                if (nodeMap.TryGetValue((int)childBuffer[i], out var childNode))
                {
                    node.Children.Add(childNode);
                    childNode.Parent = node;
                }
            }

            var parentId = SceneHierarchy_GetParent(_engine.Context, entityId);
            if (!parentId.IsValid)
            {
                _rootGameObjects.Add(node);
            }
        }

        HierarchyChanged?.Invoke(this, EventArgs.Empty);
    }

    private SceneNode CreateGameObjectNode(Entity entity)
    {
        var node = new SceneNode(entity);

        try
        {
            var tagComponent = entity.GetComponent<TagComponent>(nameof(TagComponent));
            if (tagComponent != null && !string.IsNullOrEmpty(tagComponent.Tag))
            {
                node.Name = tagComponent.Tag;
            }
            else
            {
                var prefabComponent = entity.GetComponent<PrefabIdComponent>(nameof(PrefabIdComponent));
                node.Name = prefabComponent?.PrefabName ?? $"GameObject_{entity.Id}";
            }
        }
        catch
        {
            node.Name = $"GameObject_{entity.Id}";
        }

        node.IsVisible = true;
        return node;
    }

    #endregion

    #region GameObject Operations

    /// <summary>
    /// Create an empty GameObject
    /// </summary>
    public SceneNode? CreateEmptyGameObject(string name = "GameObject")
    {
        try
        {
            var entity = _engine.CreateEntity();
            if (!entity.IsValid)
            {
                Console.Error.WriteLine("[GameObject] Failed to create entity");
                return null;
            }

            // Add required components
            entity.SetComponent("TagComponent", JsonSerializer.Serialize(new { tag = name }));
            entity.SetComponent("TransformComponent", GetDefaultTransformJson());
            entity.SetComponent("GridPositionComponent", JsonSerializer.Serialize(new { tile = new[] { 0, 0 } }));

            RefreshHierarchy();
            MarkDirty();

            Console.WriteLine($"[GameObject] Created empty GameObject: {name}");
            return FindGameObjectByEntityId(entity.Id);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] CreateEmptyGameObject error: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Create a GameObject from a prefab
    /// </summary>
    public SceneNode? InstantiatePrefab(string prefabName, int tileX = 0, int tileY = 0, SceneNode? parent = null)
    {
        try
        {
            var entity = Game.Prefabs.PrefabRegistry.SpawnAtTile(prefabName, tileX, tileY);
            if (entity == null || !entity.IsValid)
            {
                Console.Error.WriteLine($"[GameObject] Failed to instantiate prefab: {prefabName}");
                return null;
            }

            if (parent != null)
            {
                SceneHierarchy_SetParent(_engine.Context,
                    new EntityId { id = (uint)entity.Id },
                    new EntityId { id = (uint)parent.Entity.Id });
            }

            RefreshHierarchy();
            MarkDirty();

            Console.WriteLine($"[GameObject] Instantiated prefab: {prefabName} at ({tileX}, {tileY})");
            return FindGameObjectByEntityId(entity.Id);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] InstantiatePrefab error: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Duplicate a GameObject
    /// </summary>
    public SceneNode? DuplicateGameObject(SceneNode node)
    {
        if (node.Entity?.IsValid != true) return null;

        try
        {
            var prefabComponent = node.Entity.GetComponent<PrefabIdComponent>(nameof(PrefabIdComponent));
            if (prefabComponent != null && !string.IsNullOrEmpty(prefabComponent.PrefabName))
            {
                var gridPos = node.Entity.GetComponent<GridPositionComponent>(nameof(GridPositionComponent));
                int newX = 0, newY = 0;
                if (gridPos != null)
                {
                    var (currentX, currentY) = gridPos.AsTuple();
                    newX = currentX + 1;
                    newY = currentY;
                }

                return InstantiatePrefab(prefabComponent.PrefabName, newX, newY, node.Parent);
            }
            else
            {
                Console.WriteLine("[GameObject] Cannot duplicate GameObject without prefab information");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] DuplicateGameObject error: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Delete a GameObject
    /// </summary>
    public void DeleteGameObject(SceneNode node)
    {
        if (node.Entity?.IsValid != true) return;

        try
        {
            if (node.Parent != null)
            {
                node.Parent.Children.Remove(node);
            }
            else
            {
                _rootGameObjects.Remove(node);
            }

            _engine.DestroyEntity(node.Entity);
            MarkDirty();

            HierarchyChanged?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"[GameObject] Deleted GameObject: {node.Name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] DeleteGameObject error: {ex}");
        }
    }

    /// <summary>
    /// Reparent a GameObject
    /// </summary>
    public void ReparentGameObject(SceneNode node, SceneNode? newParent)
    {
        if (node.Entity?.IsValid != true) return;

        try
        {
            if (node.Parent != null)
            {
                node.Parent.Children.Remove(node);
            }
            else
            {
                _rootGameObjects.Remove(node);
            }

            if (newParent != null)
            {
                newParent.Children.Add(node);
                node.Parent = newParent;

                SceneHierarchy_SetParent(_engine.Context,
                    new EntityId { id = (uint)node.Entity.Id },
                    new EntityId { id = (uint)newParent.Entity.Id });
            }
            else
            {
                _rootGameObjects.Add(node);
                node.Parent = null;

                SceneHierarchy_SetParent(_engine.Context,
                    new EntityId { id = (uint)node.Entity.Id },
                    EntityId.Invalid);
            }

            MarkDirty();
            HierarchyChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] ReparentGameObject error: {ex}");
        }
    }

    /// <summary>
    /// Select a GameObject
    /// </summary>
    public void SelectGameObject(SceneNode node)
    {
        GameObjectSelected?.Invoke(this, node);
    }

    #endregion

    #region Prefab Management

    /// <summary>
    /// Load all prefabs from the assets folder
    /// </summary>
    public void LoadPrefabLibrary()
    {
        _prefabLibrary.Clear();
        var prefabsPath = Path.Combine(ContentPaths.Root, "Assets/Prefabs");

        if (!Directory.Exists(prefabsPath))
        {
            Console.WriteLine("[GameObject] Prefabs directory not found, creating...");
            Directory.CreateDirectory(prefabsPath);
            return;
        }

        foreach (var file in Directory.GetFiles(prefabsPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var prefabData = JsonSerializer.Deserialize<JsonElement>(json);
                var prefab = PrefabDefinition.FromJsonElement(prefabData);

                if (prefab != null)
                {
                    _prefabLibrary[prefab.Name] = prefab;
                    if (prefab.PrefabId >= _nextPrefabId)
                    {
                        _nextPrefabId = prefab.PrefabId + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[GameObject] Failed to load prefab from {file}: {ex.Message}");
            }
        }

        PrefabLibraryChanged?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"[GameObject] Loaded {_prefabLibrary.Count} prefabs");
    }

    /// <summary>
    /// Create a prefab from an existing GameObject
    /// </summary>
    public PrefabDefinition? CreatePrefabFromGameObject(SceneNode node, string? prefabName = null)
    {
        if (node.Entity?.IsValid != true) return null;

        try
        {
            var name = prefabName ?? node.Name;
            if (_prefabLibrary.ContainsKey(name))
            {
                name = $"{name}_{DateTime.Now:HHmmss}";
            }

            var components = ExtractComponentsFromEntity(node.Entity);

            var prefab = new PrefabDefinition
            {
                Name = name,
                PrefabId = _nextPrefabId++,
                Components = components
            };

            // Update PrefabIdComponent to match the new prefab
            prefab.Components["PrefabIdComponent"] = new { prefabId = prefab.PrefabId, prefabName = prefab.Name };

            _prefabLibrary[prefab.Name] = prefab;
            SavePrefab(prefab);

            PrefabLibraryChanged?.Invoke(this, EventArgs.Empty);
            Console.WriteLine($"[GameObject] Created prefab from GameObject: {name}");
            return prefab;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] CreatePrefabFromGameObject error: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Create a new empty prefab
    /// </summary>
    public PrefabDefinition CreateNewPrefab(string name)
    {
        if (_prefabLibrary.ContainsKey(name))
        {
            name = $"{name}_{DateTime.Now:HHmmss}";
        }

        var prefab = new PrefabDefinition
        {
            Name = name,
            PrefabId = _nextPrefabId++,
            Components = new Dictionary<string, object>()
        };

        AddRequiredComponents(prefab);
        _prefabLibrary[prefab.Name] = prefab;

        PrefabLibraryChanged?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"[GameObject] Created new prefab: {name}");
        return prefab;
    }

    /// <summary>
    /// Save a prefab to file
    /// </summary>
    public bool SavePrefab(PrefabDefinition prefab)
    {
        try
        {
            var prefabsPath = Path.Combine(ContentPaths.Root, "prefabs");
            if (!Directory.Exists(prefabsPath))
            {
                Directory.CreateDirectory(prefabsPath);
            }

            var filePath = Path.Combine(prefabsPath, $"{prefab.Name}.json");
            var json = JsonSerializer.Serialize(prefab.ToJsonFormat(), new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(filePath, json);
            prefab.MarkClean();

            Console.WriteLine($"[GameObject] Saved prefab: {prefab.Name}");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] SavePrefab error: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Delete a prefab
    /// </summary>
    public bool DeletePrefab(string prefabName)
    {
        try
        {
            var prefabsPath = Path.Combine(ContentPaths.Root, "prefabs");
            var filePath = Path.Combine(prefabsPath, $"{prefabName}.json");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _prefabLibrary.Remove(prefabName);
            PrefabLibraryChanged?.Invoke(this, EventArgs.Empty);

            Console.WriteLine($"[GameObject] Deleted prefab: {prefabName}");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] DeletePrefab error: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Get prefab by name
    /// </summary>
    public PrefabDefinition? GetPrefab(string name)
    {
        return _prefabLibrary.TryGetValue(name, out var prefab) ? prefab : null;
    }

    /// <summary>
    /// Get all available prefab names
    /// </summary>
    public string[] GetAvailablePrefabNames()
    {
        return _prefabLibrary.Keys.ToArray();
    }

    /// <summary>
    /// Place a prefab instance at world coordinates
    /// </summary>
    public SceneNode? PlacePrefabAtWorldPosition(string prefabName, float worldX, float worldY)
    {
        try
        {
            // Convert world coordinates to tile coordinates
            int tileX = (int)Math.Floor(worldX / _engine.TileSize);
            int tileY = (int)Math.Floor(worldY / _engine.TileSize);

            return InstantiatePrefab(prefabName, tileX, tileY);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] Error placing prefab at world position: {ex}");
            return null;
        }
    }

    #endregion

    #region Helper Methods

    private void MarkDirty()
    {
        HasUnsavedChanges = true;
    }

    private SceneNode? FindGameObjectByEntityId(int entityId)
    {
        foreach (var root in _rootGameObjects)
        {
            if (root.Entity.Id == entityId) return root;

            foreach (var descendant in root.GetDescendants())
            {
                if (descendant.Entity.Id == entityId) return descendant;
            }
        }
        return null;
    }

    private Dictionary<string, object> ExtractComponentsFromEntity(Entity entity)
    {
        var components = new Dictionary<string, object>();

        var knownTypes = new[] {
            "TagComponent", "TransformComponent", "SpriteComponent", "GridPositionComponent",
            "SpriteAnimationComponent", "AnimationClipsComponent", "PlayerTagComponent",
            "ObstacleComponent", "FacingComponent", "AnimationStateComponent", "StatsComponent",
            "FactionComponent", "AIParams", "ScriptsComponent"
        };

        foreach (var componentType in knownTypes)
        {
            if (HasComponent(_engine.Context, new EntityId { id = (uint)entity.Id }, componentType) != 0)
            {
                try
                {
                    var json = entity.GetComponent(componentType);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var element = JsonSerializer.Deserialize<JsonElement>(json);
                        components[componentType] = element;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[GameObject] Failed to extract {componentType}: {ex.Message}");
                }
            }
        }

        return components;
    }

    private void AddRequiredComponents(PrefabDefinition prefab)
    {
        prefab.Components["PrefabIdComponent"] = new { prefabId = prefab.PrefabId, prefabName = prefab.Name };
        prefab.Components["TagComponent"] = new { tag = prefab.Name };
        prefab.Components["GridPositionComponent"] = new { tile = new[] { 0, 0 } };
        prefab.Components["TransformComponent"] = JsonSerializer.Deserialize<JsonElement>(GetDefaultTransformJson());
    }

    private string GetDefaultTransformJson() =>
        "{\"localPosition\":[0.0,0.0],\"localRotation\":0.0,\"localScale\":[1.0,1.0],\"worldPosition\":[0.0,0.0],\"worldRotation\":0.0,\"worldScale\":[1.0,1.0],\"isDirty\":true,\"freezeTransform\":false,\"pivot\":[0.5,0.5],\"lockX\":false,\"lockY\":false,\"lockRotation\":false,\"lockScaleX\":false,\"lockScaleY\":false}";

    // Event handlers for prefab file changes
    private void OnPrefabFileChanged(object? sender, string prefabName)
    {
        try
        {
            var prefabsPath = Path.Combine(ContentPaths.Root, "prefabs");
            var filePath = Path.Combine(prefabsPath, $"{prefabName}.json");

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var prefabData = JsonSerializer.Deserialize<JsonElement>(json);
                var prefab = PrefabDefinition.FromJsonElement(prefabData);

                if (prefab != null)
                {
                    _prefabLibrary[prefab.Name] = prefab;
                    PrefabLibraryChanged?.Invoke(this, EventArgs.Empty);
                    Console.WriteLine($"[GameObject] Reloaded prefab: {prefabName}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] OnPrefabFileChanged error: {ex}");
        }
    }

    private void OnPrefabFileAdded(object? sender, string prefabName)
    {
        OnPrefabFileChanged(sender, prefabName);
    }

    private void OnPrefabFileRemoved(object? sender, string prefabName)
    {
        _prefabLibrary.Remove(prefabName);
        PrefabLibraryChanged?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"[GameObject] Removed prefab: {prefabName}");
    }

    #endregion
}