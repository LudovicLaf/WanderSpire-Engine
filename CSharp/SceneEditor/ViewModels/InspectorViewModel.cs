// SceneEditor/ViewModels/InspectorViewModel.cs
using ReactiveUI;
using SceneEditor.Models;
using SceneEditor.Services;
using SceneEditor.ViewModels.ComponentEditors;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using WanderSpire.Scripting;
using ICommand = System.Windows.Input.ICommand;

namespace SceneEditor.ViewModels;

/// <summary>
/// View model for the inspector panel that shows entity properties and components
/// </summary>
public class InspectorViewModel : ReactiveObject
{
    private readonly EditorEngine _engine;
    private readonly GameObjectService _sceneService;
    private readonly CommandService _commandService;

    private SceneNode? _selectedNode;
    private Entity? _selectedEntity;
    private string _entityName = string.Empty;
    private bool _entityEnabled = true;

    public ObservableCollection<ComponentEditorViewModel> ComponentEditors { get; } = new();

    public SceneNode? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    public Entity? SelectedEntity
    {
        get => _selectedEntity;
        private set => this.RaiseAndSetIfChanged(ref _selectedEntity, value);
    }

    public string EntityName
    {
        get => _entityName;
        set => this.RaiseAndSetIfChanged(ref _entityName, value);
    }

    public bool EntityEnabled
    {
        get => _entityEnabled;
        set => this.RaiseAndSetIfChanged(ref _entityEnabled, value);
    }

    public bool HasSelection => SelectedEntity?.IsValid == true;
    public string SelectionInfo => HasSelection ? $"Entity {SelectedEntity!.Id}" : "No selection";

    // Commands
    public ICommand AddComponentCommand { get; }
    public ICommand RemoveComponentCommand { get; }
    public ICommand ResetComponentCommand { get; }
    public ICommand CopyComponentCommand { get; }
    public ICommand PasteComponentCommand { get; }

    public InspectorViewModel(EditorEngine engine, GameObjectService sceneService, CommandService commandService)
    {
        _engine = engine;
        _sceneService = sceneService;
        _commandService = commandService;

        // Initialize commands
        AddComponentCommand = ReactiveCommand.Create<string>(AddComponent);
        RemoveComponentCommand = ReactiveCommand.Create<ComponentEditorViewModel>(RemoveComponent);
        ResetComponentCommand = ReactiveCommand.Create<ComponentEditorViewModel>(ResetComponent);
        CopyComponentCommand = ReactiveCommand.Create<ComponentEditorViewModel>(CopyComponent);
        PasteComponentCommand = ReactiveCommand.Create<string>(PasteComponent);

        // Subscribe to selection changes
        _sceneService.GameObjectSelected += OnNodeSelected;

        // Monitor property changes
        this.WhenAnyValue(x => x.SelectedNode)
            .Subscribe(OnSelectedNodeChanged);

        this.WhenAnyValue(x => x.EntityName)
            .Subscribe(OnEntityNameChanged);

        this.WhenAnyValue(x => x.EntityEnabled)
            .Subscribe(OnEntityEnabledChanged);

        // Update property change notifications
        this.WhenAnyValue(x => x.SelectedEntity)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(HasSelection));
                this.RaisePropertyChanged(nameof(SelectionInfo));
            });
    }

    public void Initialize()
    {
        // Initial setup
        RefreshInspector();
    }

    private void OnNodeSelected(object? sender, SceneNode node)
    {
        SelectedNode = node;
    }

    private void OnSelectedNodeChanged(SceneNode? node)
    {
        try
        {
            if (node?.Entity?.IsValid == true)
            {
                SelectedEntity = node.Entity;
                LoadEntityData();
                RefreshComponentEditors();
            }
            else
            {
                SelectedEntity = null;
                ClearInspector();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] OnSelectedNodeChanged error: {ex}");
        }
    }

    private void LoadEntityData()
    {
        if (SelectedEntity?.IsValid != true)
            return;

        try
        {
            // Load entity name from TagComponent
            var tagComponent = SelectedEntity.GetComponent<WanderSpire.Components.TagComponent>("TagComponent");
            EntityName = tagComponent?.Tag ?? $"Entity_{SelectedEntity.Id}";

            // TODO: Load entity enabled state if available
            EntityEnabled = true;

            Console.WriteLine($"[InspectorViewModel] Loaded entity data for {EntityName}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] LoadEntityData error: {ex}");
            EntityName = $"Entity_{SelectedEntity?.Id ?? -1}";
            EntityEnabled = true;
        }
    }

    private void RefreshComponentEditors()
    {
        ComponentEditors.Clear();

        if (SelectedEntity?.IsValid != true)
            return;

        try
        {
            // Get all component types from the entity
            var componentTypes = GetEntityComponentTypes();

            foreach (var componentType in componentTypes)
            {
                var editor = CreateComponentEditor(componentType);
                if (editor != null)
                {
                    ComponentEditors.Add(editor);
                }
            }

            Console.WriteLine($"[InspectorViewModel] Created {ComponentEditors.Count} component editors");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] RefreshComponentEditors error: {ex}");
        }
    }

    private string[] GetEntityComponentTypes()
    {
        if (SelectedEntity?.IsValid != true)
            return Array.Empty<string>();

        try
        {
            // Common component types to check for
            var knownComponents = new[]
            {
                "TagComponent",
                "TransformComponent",
                "SpriteComponent",
                "GridPositionComponent",
                "SpriteAnimationComponent",
                "PlayerTagComponent",
                "ObstacleComponent",
                "FacingComponent",
                "AnimationStateComponent"
            };

            var existingComponents = knownComponents
                .Where(componentType => EngineInterop.HasComponent(_engine.Context,
                    new EntityId { id = (uint)SelectedEntity.Id }, componentType) != 0)
                .ToArray();

            return existingComponents;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] GetEntityComponentTypes error: {ex}");
            return Array.Empty<string>();
        }
    }

    private ComponentEditorViewModel? CreateComponentEditor(string componentType)
    {
        if (SelectedEntity?.IsValid != true)
            return null;

        try
        {
            return componentType switch
            {
                "TagComponent" => new TagComponentEditor(SelectedEntity),
                "TransformComponent" => new TransformComponentEditor(SelectedEntity),
                "SpriteComponent" => new SpriteComponentEditor(SelectedEntity),
                "GridPositionComponent" => new GridPositionComponentEditor(SelectedEntity),
                "SpriteAnimationComponent" => new SpriteAnimationComponentEditor(SelectedEntity),
                "PlayerTagComponent" => new PlayerTagComponentEditor(SelectedEntity),
                "ObstacleComponent" => new ObstacleComponentEditor(SelectedEntity),
                "FacingComponent" => new FacingComponentEditor(SelectedEntity),
                "AnimationStateComponent" => new AnimationStateComponentEditor(SelectedEntity),
                _ => new GenericComponentEditor(componentType, SelectedEntity)
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] CreateComponentEditor error for {componentType}: {ex}");
            return null;
        }
    }

    private void RefreshInspector()
    {
        try
        {
            if (SelectedEntity?.IsValid == true)
            {
                LoadEntityData();
                RefreshComponentEditors();
            }
            else
            {
                ClearInspector();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] RefreshInspector error: {ex}");
        }
    }

    private void ClearInspector()
    {
        ComponentEditors.Clear();
        EntityName = string.Empty;
        EntityEnabled = true;
    }

    private void OnEntityNameChanged(string newName)
    {
        if (SelectedEntity?.IsValid != true || string.IsNullOrWhiteSpace(newName))
            return;

        try
        {
            // Update the TagComponent
            var tagJson = System.Text.Json.JsonSerializer.Serialize(new { tag = newName });
            SelectedEntity.SetComponent("TagComponent", tagJson);

            // Update the scene node name
            if (SelectedNode != null)
            {
                SelectedNode.Name = newName;
            }

            Console.WriteLine($"[InspectorViewModel] Updated entity name to: {newName}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] OnEntityNameChanged error: {ex}");
        }
    }

    private void OnEntityEnabledChanged(bool enabled)
    {
        if (SelectedEntity?.IsValid != true)
            return;

        try
        {
            // TODO: Implement entity enabled/disabled state if supported by engine
            Console.WriteLine($"[InspectorViewModel] Entity enabled changed to: {enabled}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] OnEntityEnabledChanged error: {ex}");
        }
    }

    // Command implementations
    private void AddComponent(string componentType)
    {
        if (SelectedEntity?.IsValid != true || string.IsNullOrEmpty(componentType))
            return;

        try
        {
            // Check if component already exists
            if (EngineInterop.HasComponent(_engine.Context,
                new EntityId { id = (uint)SelectedEntity.Id }, componentType) != 0)
            {
                Console.WriteLine($"[InspectorViewModel] Component {componentType} already exists");
                return;
            }

            // Add default component data
            var defaultData = GetDefaultComponentData(componentType);
            SelectedEntity.SetComponent(componentType, defaultData);

            // Refresh the inspector
            RefreshComponentEditors();

            Console.WriteLine($"[InspectorViewModel] Added component: {componentType}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] AddComponent error: {ex}");
        }
    }

    private void RemoveComponent(ComponentEditorViewModel? editor)
    {
        if (editor == null || SelectedEntity?.IsValid != true)
            return;

        try
        {
            // Remove the component
            EngineInterop.RemoveComponent(_engine.Context,
                new EntityId { id = (uint)SelectedEntity.Id }, editor.ComponentType);

            // Remove from UI
            ComponentEditors.Remove(editor);

            Console.WriteLine($"[InspectorViewModel] Removed component: {editor.ComponentType}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] RemoveComponent error: {ex}");
        }
    }

    private void ResetComponent(ComponentEditorViewModel? editor)
    {
        if (editor == null || SelectedEntity?.IsValid != true)
            return;

        try
        {
            // Reset to default values
            var defaultData = GetDefaultComponentData(editor.ComponentType);
            SelectedEntity.SetComponent(editor.ComponentType, defaultData);

            // Refresh the editor
            editor.RefreshFromEntity();

            Console.WriteLine($"[InspectorViewModel] Reset component: {editor.ComponentType}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] ResetComponent error: {ex}");
        }
    }

    private void CopyComponent(ComponentEditorViewModel? editor)
    {
        if (editor == null || SelectedEntity?.IsValid != true)
            return;

        try
        {
            // TODO: Implement component clipboard functionality
            Console.WriteLine($"[InspectorViewModel] Copied component: {editor.ComponentType}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] CopyComponent error: {ex}");
        }
    }

    private void PasteComponent(string componentType)
    {
        if (SelectedEntity?.IsValid != true)
            return;

        try
        {
            // TODO: Implement component clipboard functionality
            Console.WriteLine($"[InspectorViewModel] Pasted component: {componentType}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] PasteComponent error: {ex}");
        }
    }

    private string GetDefaultComponentData(string componentType)
    {
        return componentType switch
        {
            "TagComponent" => "{\"tag\":\"New Entity\"}",
            "TransformComponent" => "{\"localPosition\":[0.0,0.0],\"localRotation\":0.0,\"localScale\":[1.0,1.0],\"worldPosition\":[0.0,0.0],\"worldRotation\":0.0,\"worldScale\":[1.0,1.0],\"isDirty\":true,\"freezeTransform\":false,\"pivot\":[0.5,0.5],\"lockX\":false,\"lockY\":false,\"lockRotation\":false,\"lockScaleX\":false,\"lockScaleY\":false}",
            "SpriteComponent" => "{\"atlasName\":\"default\",\"frameName\":\"default\"}",
            "GridPositionComponent" => "{\"tile\":[0,0],\"tileObj\":{\"x\":0,\"y\":0}}",
            "SpriteAnimationComponent" => "{\"currentAnimation\":\"\",\"frameIndex\":0,\"timeAccumulator\":0.0,\"isPlaying\":false}",
            "PlayerTagComponent" => "{}",
            "ObstacleComponent" => "{\"blocking\":true}",
            "FacingComponent" => "{\"direction\":\"down\"}",
            "AnimationStateComponent" => "{\"state\":\"idle\"}",
            _ => "{}"
        };
    }

    /// <summary>
    /// Get available component types that can be added
    /// </summary>
    public string[] GetAvailableComponentTypes()
    {
        if (SelectedEntity?.IsValid != true)
            return Array.Empty<string>();

        var allComponentTypes = new[]
        {
            "TagComponent",
            "TransformComponent",
            "SpriteComponent",
            "GridPositionComponent",
            "SpriteAnimationComponent",
            "PlayerTagComponent",
            "ObstacleComponent",
            "FacingComponent",
            "AnimationStateComponent"
        };

        var existingTypes = GetEntityComponentTypes();
        return allComponentTypes.Except(existingTypes).ToArray();
    }

    /// <summary>
    /// Refresh all component editors from entity data
    /// </summary>
    public void RefreshAllEditors()
    {
        try
        {
            foreach (var editor in ComponentEditors)
            {
                editor.RefreshFromEntity();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] RefreshAllEditors error: {ex}");
        }
    }

    /// <summary>
    /// Save all component editor changes to entity
    /// </summary>
    public void SaveAllEditors()
    {
        try
        {
            foreach (var editor in ComponentEditors)
            {
                editor.SaveToEntity();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InspectorViewModel] SaveAllEditors error: {ex}");
        }
    }
}