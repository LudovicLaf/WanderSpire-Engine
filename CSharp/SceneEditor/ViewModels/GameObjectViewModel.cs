using ReactiveUI;
using SceneEditor.Models;
using SceneEditor.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using WanderSpire.Scripting;
using ICommand = System.Windows.Input.ICommand;

namespace SceneEditor.ViewModels;

/// <summary>
/// Unified GameObject view model that combines scene hierarchy and prefab management
/// Works like Unity's GameObject system - entities in scene can become prefabs
/// </summary>
public class GameObjectViewModel : ReactiveObject
{
    private readonly GameObjectService _gameObjectService;
    private readonly CommandService _commandService;
    private readonly AssetService _assetService;
    private readonly EditorEngine _engine;

    private SceneNode? _selectedGameObject;
    private string _searchText = string.Empty;
    private PanelMode _currentMode = PanelMode.Scene;
    private string _selectedComponentType = string.Empty;
    private PrefabDefinition? _selectedPrefab;

    // Scene Hierarchy Data
    public ObservableCollection<SceneNode> RootGameObjects { get; } = new();

    // Prefab Library Data  
    public ObservableCollection<PrefabDefinition> PrefabLibrary { get; } = new();
    public ObservableCollection<string> ComponentTypes { get; } = new();

    public SceneNode? SelectedGameObject
    {
        get => _selectedGameObject;
        set => this.RaiseAndSetIfChanged(ref _selectedGameObject, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public PanelMode CurrentMode
    {
        get => _currentMode;
        set => this.RaiseAndSetIfChanged(ref _currentMode, value);
    }

    public string SelectedComponentType
    {
        get => _selectedComponentType;
        set => this.RaiseAndSetIfChanged(ref _selectedComponentType, value);
    }

    public PrefabDefinition? SelectedPrefab
    {
        get => _selectedPrefab;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPrefab, value);
            // Refresh component items when prefab is selected
            if (value != null)
            {
                value.RefreshComponentItems();
            }
        }
    }

    // Mode Properties
    public bool IsSceneMode => CurrentMode == PanelMode.Scene;
    public bool IsPrefabMode => CurrentMode == PanelMode.Prefab;
    public bool HasSelectedGameObject => SelectedGameObject != null;
    public bool HasSelectedPrefab => SelectedPrefab != null;
    public bool CanCreatePrefab => HasSelectedGameObject && IsSceneMode;
    public bool CanAddComponent => (HasSelectedGameObject && IsSceneMode) || (SelectedPrefab != null && IsPrefabMode);

    // Commands - Scene Operations
    public ICommand CreateGameObjectCommand { get; private set; }
    public ICommand DuplicateGameObjectCommand { get; private set; }
    public ICommand DeleteGameObjectCommand { get; private set; }
    public ICommand CreatePrefabFromGameObjectCommand { get; private set; }
    public ICommand InstantiatePrefabCommand { get; private set; }
    public ICommand FocusGameObjectCommand { get; private set; }
    public ICommand RenameGameObjectCommand { get; private set; }

    // Commands - Prefab Operations  
    public ICommand CreateNewPrefabCommand { get; private set; }
    public ICommand SavePrefabCommand { get; private set; }
    public ICommand DeletePrefabCommand { get; private set; }
    public ICommand DuplicatePrefabCommand { get; private set; }

    // Commands - Component Operations
    public ICommand AddComponentCommand { get; private set; }
    public ICommand RemoveComponentCommand { get; private set; }

    // Commands - Mode Switching
    public ICommand SwitchToSceneModeCommand { get; private set; }
    public ICommand SwitchToPrefabModeCommand { get; private set; }
    public ICommand RefreshCommand { get; private set; }


    public GameObjectViewModel(GameObjectService gameObjectService, CommandService commandService,
        AssetService assetService, EditorEngine engine)
    {
        _gameObjectService = gameObjectService;
        _commandService = commandService;
        _assetService = assetService;
        _engine = engine;

        InitializeCommands();
        LoadComponentTypes();
        SetupEventSubscriptions();
        SetupPropertySubscriptions();
    }

    private void InitializeCommands()
    {
        // Scene Operations
        CreateGameObjectCommand = ReactiveCommand.Create<string?>(CreateGameObject);
        DuplicateGameObjectCommand = ReactiveCommand.Create<SceneNode?>(DuplicateGameObject);
        DeleteGameObjectCommand = ReactiveCommand.Create<SceneNode?>(DeleteGameObject);
        CreatePrefabFromGameObjectCommand = ReactiveCommand.Create(CreatePrefabFromGameObject,
            this.WhenAnyValue(x => x.CanCreatePrefab));
        InstantiatePrefabCommand = ReactiveCommand.Create<string?>(InstantiatePrefab);
        FocusGameObjectCommand = ReactiveCommand.Create<SceneNode?>(FocusGameObject);
        RenameGameObjectCommand = ReactiveCommand.Create<SceneNode?>(RenameGameObject);

        // Prefab Operations
        CreateNewPrefabCommand = ReactiveCommand.Create<string?>(CreateNewPrefab);
        SavePrefabCommand = ReactiveCommand.Create(SaveSelectedPrefab);
        DeletePrefabCommand = ReactiveCommand.Create<PrefabDefinition?>(DeletePrefab);
        DuplicatePrefabCommand = ReactiveCommand.Create<PrefabDefinition?>(DuplicatePrefab);

        // Component Operations
        AddComponentCommand = ReactiveCommand.Create(AddComponent,
            this.WhenAnyValue(x => x.CanAddComponent, x => x.SelectedComponentType,
                (canAdd, componentType) => canAdd && !string.IsNullOrEmpty(componentType)));
        RemoveComponentCommand = ReactiveCommand.Create<string?>(RemoveComponent);

        // Mode Switching
        SwitchToSceneModeCommand = ReactiveCommand.Create(() => CurrentMode = PanelMode.Scene);
        SwitchToPrefabModeCommand = ReactiveCommand.Create(() => CurrentMode = PanelMode.Prefab);
        RefreshCommand = ReactiveCommand.Create(RefreshAll);
    }

    private void SetupEventSubscriptions()
    {
        _gameObjectService.HierarchyChanged += OnHierarchyChanged;
        _gameObjectService.GameObjectSelected += OnGameObjectSelected;
        _gameObjectService.PrefabLibraryChanged += OnPrefabLibraryChanged;
        _assetService.AssetsRefreshed += OnAssetsRefreshed;
    }

    private void SetupPropertySubscriptions()
    {
        // Update property dependencies
        this.WhenAnyValue(x => x.SelectedGameObject)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(HasSelectedGameObject));
                this.RaisePropertyChanged(nameof(CanCreatePrefab));
                this.RaisePropertyChanged(nameof(CanAddComponent));
            });

        this.WhenAnyValue(x => x.CurrentMode)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(IsSceneMode));
                this.RaisePropertyChanged(nameof(IsPrefabMode));
                this.RaisePropertyChanged(nameof(CanCreatePrefab));
                this.RaisePropertyChanged(nameof(CanAddComponent));
            });

        this.WhenAnyValue(x => x.SelectedPrefab)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(CanAddComponent));
                this.RaisePropertyChanged(nameof(HasSelectedPrefab));
            });

        // Handle search
        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(OnSearchTextChanged);
    }

    public void Initialize()
    {
        RefreshHierarchy();
        RefreshPrefabLibrary();

        // Set initial component type
        if (ComponentTypes.Any())
        {
            SelectedComponentType = ComponentTypes.First();
        }
    }

    #region Scene Operations

    private void CreateGameObject(string? prefabName)
    {
        try
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                // Create empty GameObject
                var node = _gameObjectService.CreateEmptyGameObject("GameObject");
                if (node != null)
                {
                    SelectedGameObject = node;
                    Console.WriteLine("[GameObject] Created empty GameObject");
                }
            }
            else
            {
                // Instantiate prefab
                var node = _gameObjectService.InstantiatePrefab(prefabName, 0, 0);
                if (node != null)
                {
                    SelectedGameObject = node;
                    Console.WriteLine($"[GameObject] Instantiated prefab: {prefabName}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] CreateGameObject error: {ex.Message}");
        }
    }

    private void DuplicateGameObject(SceneNode? node)
    {
        node ??= SelectedGameObject;
        if (node == null) return;

        try
        {
            var duplicatedNode = _gameObjectService.DuplicateGameObject(node);
            if (duplicatedNode != null)
            {
                SelectedGameObject = duplicatedNode;
                Console.WriteLine($"[GameObject] Duplicated: {node.Name}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] DuplicateGameObject error: {ex.Message}");
        }
    }

    private void DeleteGameObject(SceneNode? node)
    {
        node ??= SelectedGameObject;
        if (node == null) return;

        try
        {
            _gameObjectService.DeleteGameObject(node);
            if (SelectedGameObject == node)
            {
                SelectedGameObject = null;
            }
            Console.WriteLine($"[GameObject] Deleted: {node.Name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] DeleteGameObject error: {ex.Message}");
        }
    }

    private void CreatePrefabFromGameObject()
    {
        if (SelectedGameObject?.Entity?.IsValid != true) return;

        try
        {
            var prefab = _gameObjectService.CreatePrefabFromGameObject(SelectedGameObject, SelectedGameObject.Name);
            if (prefab != null)
            {
                // Don't manually add to collection - let the event handler do it
                SelectedPrefab = prefab;
                CurrentMode = PanelMode.Prefab;
                Console.WriteLine($"[GameObject] Created prefab from GameObject: {prefab.Name}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] CreatePrefabFromGameObject error: {ex.Message}");
        }
    }

    private void InstantiatePrefab(string? prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return;

        try
        {
            var node = _gameObjectService.InstantiatePrefab(prefabName, 0, 0);
            if (node != null)
            {
                SelectedGameObject = node;
                Console.WriteLine($"[GameObject] Instantiated prefab: {prefabName}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] InstantiatePrefab error: {ex.Message}");
        }
    }

    private void FocusGameObject(SceneNode? node)
    {
        node ??= SelectedGameObject;
        if (node?.Entity?.IsValid != true) return;

        try
        {
            // TODO: Focus camera on this entity in the viewport
            Console.WriteLine($"[GameObject] Focus on: {node.Name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] FocusGameObject error: {ex.Message}");
        }
    }

    private void RenameGameObject(SceneNode? node)
    {
        node ??= SelectedGameObject;
        if (node == null) return;

        try
        {
            // TODO: Implement inline renaming or show rename dialog
            Console.WriteLine($"[GameObject] Rename: {node.Name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] RenameGameObject error: {ex.Message}");
        }
    }

    #endregion

    #region Prefab Operations

    private void CreateNewPrefab(string? name)
    {
        try
        {
            var prefabName = name ?? $"NewPrefab_{DateTime.Now:HHmmss}";
            var prefab = _gameObjectService.CreateNewPrefab(prefabName);

            // Don't manually add to PrefabLibrary - the event handler will do this
            // This prevents duplication
            SelectedPrefab = prefab;
            CurrentMode = PanelMode.Prefab;

            Console.WriteLine($"[GameObject] Created new prefab: {prefabName}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] CreateNewPrefab error: {ex.Message}");
        }
    }

    private void SaveSelectedPrefab()
    {
        if (SelectedPrefab == null) return;

        try
        {
            _gameObjectService.SavePrefab(SelectedPrefab);
            Console.WriteLine($"[GameObject] Saved prefab: {SelectedPrefab.Name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] SaveSelectedPrefab error: {ex.Message}");
        }
    }

    private void DeletePrefab(PrefabDefinition? prefab)
    {
        prefab ??= SelectedPrefab;
        if (prefab == null) return;

        try
        {
            _gameObjectService.DeletePrefab(prefab.Name);
            // Don't manually remove from collection - the event handler will do this
            if (SelectedPrefab == prefab)
            {
                SelectedPrefab = null;
            }

            Console.WriteLine($"[GameObject] Deleted prefab: {prefab.Name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] DeletePrefab error: {ex.Message}");
        }
    }

    private void DuplicatePrefab(PrefabDefinition? prefab)
    {
        prefab ??= SelectedPrefab;
        if (prefab == null) return;

        try
        {
            var duplicate = prefab.Clone($"{prefab.Name}_Copy");
            var savedPrefab = _gameObjectService.CreateNewPrefab(duplicate.Name);

            // Copy components from original
            savedPrefab.Components = duplicate.Components;
            _gameObjectService.SavePrefab(savedPrefab);

            // Don't manually add to collection - the event handler will do this
            SelectedPrefab = savedPrefab;

            Console.WriteLine($"[GameObject] Duplicated prefab: {prefab.Name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] DuplicatePrefab error: {ex.Message}");
        }
    }

    #endregion

    #region Component Operations

    private void AddComponent()
    {
        try
        {
            if (IsSceneMode && SelectedGameObject?.Entity?.IsValid == true)
            {
                AddComponentToEntity(SelectedGameObject.Entity, SelectedComponentType);
            }
            else if (IsPrefabMode && SelectedPrefab != null)
            {
                AddComponentToPrefab(SelectedPrefab, SelectedComponentType);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] AddComponent error: {ex.Message}");
        }
    }

    private void RemoveComponent(string? componentType)
    {
        if (string.IsNullOrEmpty(componentType)) return;

        try
        {
            if (IsSceneMode && SelectedGameObject?.Entity?.IsValid == true)
            {
                RemoveComponentFromEntity(SelectedGameObject.Entity, componentType);
            }
            else if (IsPrefabMode && SelectedPrefab != null)
            {
                SelectedPrefab.RemoveComponent(componentType);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[GameObject] RemoveComponent error: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    private void LoadComponentTypes()
    {
        ComponentTypes.Clear();
        var types = new[]
        {
            "SpriteComponent",
            "SpriteAnimationComponent",
            "AnimationClipsComponent",
            "ObstacleComponent",
            "MovementComponent",
            "PathFollowingComponent",
            "FacingComponent",
            "AnimationStateComponent",
            "StatsComponent",
            "FactionComponent",
            "AIParams",
            "ScriptsComponent",
            "PlayerTagComponent"
        };

        foreach (var type in types)
        {
            ComponentTypes.Add(type);
        }
    }

    private void RefreshHierarchy()
    {
        RootGameObjects.Clear();
        foreach (var node in _gameObjectService.RootGameObjects)
        {
            RootGameObjects.Add(node);
        }
    }

    private void RefreshPrefabLibrary()
    {
        PrefabLibrary.Clear();
        foreach (var prefab in _gameObjectService.PrefabLibrary)
        {
            PrefabLibrary.Add(prefab);
        }
    }

    private void RefreshAll()
    {
        RefreshHierarchy();
        RefreshPrefabLibrary();
        _gameObjectService.RefreshHierarchy();
        _gameObjectService.LoadPrefabLibrary();
    }

    // Event Handlers
    private void OnHierarchyChanged(object? sender, EventArgs e) => RefreshHierarchy();
    private void OnGameObjectSelected(object? sender, SceneNode node) => SelectedGameObject = node;
    private void OnPrefabLibraryChanged(object? sender, EventArgs e) => RefreshPrefabLibrary();
    private void OnAssetsRefreshed(object? sender, EventArgs e) => RefreshPrefabLibrary();
    private void OnSearchTextChanged(string searchText)
    {
        // TODO: Implement search filtering
    }

    private void AddComponentToEntity(Entity entity, string componentType)
    {
        var defaultData = GetDefaultComponentData(componentType);
        entity.SetComponent(componentType, defaultData);
        Console.WriteLine($"[GameObject] Added {componentType} to entity {entity.Id}");
    }

    private void AddComponentToPrefab(PrefabDefinition prefab, string componentType)
    {
        if (prefab.Components.ContainsKey(componentType)) return;

        var defaultComponent = CreateDefaultComponent(componentType);
        prefab.AddComponent(componentType, defaultComponent);
        Console.WriteLine($"[GameObject] Added {componentType} to prefab {prefab.Name}");
    }

    private void RemoveComponentFromEntity(Entity entity, string componentType)
    {
        if (IsRequiredComponent(componentType)) return;

        EngineInterop.RemoveComponent(_engine.Context, new EntityId { id = (uint)entity.Id }, componentType);
        Console.WriteLine($"[GameObject] Removed {componentType} from entity {entity.Id}");
    }

    private bool IsRequiredComponent(string componentType) =>
        componentType is "PrefabIdComponent" or "TagComponent" or "GridPositionComponent" or "TransformComponent";

    private string GetDefaultComponentData(string componentType) => componentType switch
    {
        "SpriteComponent" => "{\"atlasName\":\"default\",\"frameName\":\"default\"}",
        "ObstacleComponent" => "{\"blocksMovement\":false,\"blocksVision\":false,\"zOrder\":0}",
        "StatsComponent" => "{\"maxHitpoints\":100,\"currentHitpoints\":100,\"maxMana\":50,\"currentMana\":50,\"accuracy\":10,\"strength\":10,\"attackType\":0,\"defenseStab\":5,\"defenseSlash\":5,\"defenseCrush\":5,\"defenseMagic\":5,\"defenseRanged\":5,\"attackRange\":1.0,\"attackSpeed\":3}",
        "ScriptsComponent" => "{\"scripts\":[]}",
        _ => "{}"
    };

    private object CreateDefaultComponent(string componentType) => componentType switch
    {
        "SpriteComponent" => new { atlasName = "default", frameName = "default" },
        "ObstacleComponent" => new { blocksMovement = false, blocksVision = false, zOrder = 0 },
        "MovementComponent" => new { },
        "FacingComponent" => new { facing = 0 },
        "AnimationStateComponent" => new { state = 0 },
        "StatsComponent" => new { maxHitpoints = 100, currentHitpoints = 100, maxMana = 50, currentMana = 50, accuracy = 10, strength = 10, attackType = 0, defenseStab = 5, defenseSlash = 5, defenseCrush = 5, defenseMagic = 5, defenseRanged = 5, attackRange = 1.0f, attackSpeed = 3 },
        "ScriptsComponent" => new { scripts = new string[0] },
        _ => new { }
    };

    public string[] GetAvailablePrefabs() => _gameObjectService.GetAvailablePrefabNames();

    #endregion
}

public enum PanelMode
{
    Scene,
    Prefab
}