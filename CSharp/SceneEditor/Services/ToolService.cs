using ReactiveUI;
using SceneEditor.Models;
using SceneEditor.Tools;
using SceneEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SceneEditor.Services;

/// <summary>
/// Manages editor tools and tool switching - updated with prefab support
/// </summary>
public class ToolService : ReactiveObject
{
    private readonly Dictionary<string, IEditorTool> _tools = new();
    private IEditorTool? _currentTool;
    private readonly EditorEngine _engine;
    private readonly GameObjectService _sceneService;
    private readonly CommandService _commandService;

    public IEditorTool? CurrentTool
    {
        get => _currentTool;
        private set => this.RaiseAndSetIfChanged(ref _currentTool, value);
    }

    public IReadOnlyList<IEditorTool> AvailableTools => _tools.Values.ToList();

    public event EventHandler<IEditorTool>? ToolChanged;

    public ToolService(EditorEngine engine, GameObjectService sceneService, CommandService commandService)
    {
        _engine = engine;
        _sceneService = sceneService;
        _commandService = commandService;

        InitializeTools();
    }

    private void InitializeTools()
    {
        // Register built-in tools
        RegisterTool(new SelectTool(_engine, _sceneService, _commandService));
        RegisterTool(new MoveTool(_engine, _sceneService, _commandService));
        RegisterTool(new RotateTool(_engine, _sceneService, _commandService));
        RegisterTool(new ScaleTool(_engine, _sceneService, _commandService));
        RegisterTool(new TilePaintTool(_engine, _sceneService, _commandService));
        RegisterTool(new TileEraseTool(_engine, _sceneService, _commandService));

        // Register prefab placement tool
        RegisterTool(new PrefabPlacementTool(_engine, _sceneService, _commandService));

        // Set default tool
        SetActiveTool("Select");
    }

    public void RegisterTool(IEditorTool tool)
    {
        _tools[tool.Name] = tool;
    }

    public void SetActiveTool(string toolName)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            Console.Error.WriteLine($"[ToolService] Tool '{toolName}' not found");
            return;
        }

        // Deactivate current tool
        _currentTool?.OnDeactivate();

        // Activate new tool
        CurrentTool = tool;
        tool.OnActivate();

        ToolChanged?.Invoke(this, tool);
        Console.WriteLine($"[ToolService] Switched to tool: {toolName}");
    }

    public T? GetTool<T>() where T : class, IEditorTool
    {
        return _tools.Values.OfType<T>().FirstOrDefault();
    }

    public IEditorTool? GetTool(string name)
    {
        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }
}

/// <summary>
/// Interface for editor tools
/// </summary>
public interface IEditorTool
{
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }
    string Icon { get; }
    bool IsActive { get; }

    void OnActivate();
    void OnDeactivate();
    void OnMouseDown(float worldX, float worldY, ViewportInputModifiers modifiers);
    void OnMouseUp(float worldX, float worldY, ViewportInputModifiers modifiers);
    void OnDrag(float worldX, float worldY, ViewportInputModifiers modifiers);
    void OnRightClick(float worldX, float worldY, ViewportInputModifiers modifiers);
    void OnKeyDown(string key, ViewportInputModifiers modifiers);
    void OnKeyUp(string key, ViewportInputModifiers modifiers);
}

/// <summary>
/// Base class for editor tools
/// </summary>
public abstract class EditorToolBase : ReactiveObject, IEditorTool
{
    protected readonly EditorEngine _engine;
    protected readonly GameObjectService _sceneService;
    protected readonly CommandService _commandService;
    private bool _isActive;

    public abstract string Name { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public abstract string Icon { get; }

    public bool IsActive
    {
        get => _isActive;
        protected set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }

    protected EditorToolBase(EditorEngine engine, GameObjectService sceneService, CommandService commandService)
    {
        _engine = engine;
        _sceneService = sceneService;
        _commandService = commandService;
    }

    public virtual void OnActivate()
    {
        IsActive = true;
    }

    public virtual void OnDeactivate()
    {
        IsActive = false;
    }

    public virtual void OnMouseDown(float worldX, float worldY, ViewportInputModifiers modifiers) { }
    public virtual void OnMouseUp(float worldX, float worldY, ViewportInputModifiers modifiers) { }
    public virtual void OnDrag(float worldX, float worldY, ViewportInputModifiers modifiers) { }
    public virtual void OnRightClick(float worldX, float worldY, ViewportInputModifiers modifiers) { }
    public virtual void OnKeyDown(string key, ViewportInputModifiers modifiers) { }
    public virtual void OnKeyUp(string key, ViewportInputModifiers modifiers) { }

    /// <summary>
    /// Helper to find entity at world position
    /// </summary>
    protected SceneNode? FindEntityAtPosition(float worldX, float worldY)
    {
        // TODO: Implement proper entity picking
        // This would use spatial queries or collision detection
        // For now, return null as placeholder
        return null;
    }

    /// <summary>
    /// Helper to get currently selected entities
    /// </summary>
    protected List<SceneNode> GetSelectedEntities()
    {
        var selected = new List<SceneNode>();
        // TODO: Implement selection tracking
        // For now, get the current hierarchy selection
        return selected;
    }
}