// SceneEditor/ViewModels/ViewportViewModel.cs - Updated for Tile Painting
using ReactiveUI;
using SceneEditor.Models;
using SceneEditor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using WanderSpire.Scripting;
using ICommand = System.Windows.Input.ICommand;

namespace SceneEditor.ViewModels;

/// <summary>
/// Enhanced viewport view model with tile painting support
/// </summary>
public class ViewportViewModel : ReactiveObject
{
    private readonly EditorEngine _engine;
    private readonly GameObjectService _sceneService;
    private readonly ToolService _toolService;
    private readonly CommandService _commandService;
    private readonly TilemapService _tilemapService;
    private readonly TilePaletteService _tilePaletteService;
    private readonly TilePaintingService _tilePaintingService;

    // Camera properties
    private float _cameraX = 0f;
    private float _cameraY = 0f;
    private float _zoomLevel = 1f;
    private readonly float _minZoom = 0.1f;
    private readonly float _maxZoom = 10f;

    // Display options
    private bool _isGridVisible = true;
    private bool _isGizmosVisible = true;
    private bool _isHelpHidden = false;
    private ViewportMode _viewMode = ViewportMode.Scene;

    // Engine state
    private bool _isEngineReady = false;
    private bool _hasRenderingError = false;
    private string _errorMessage = string.Empty;

    // Selection
    private readonly ObservableCollection<SceneNode> _selectedNodes = new();

    // Grid settings
    private float _gridSize = 32f;
    private int _gridSubdivisions = 4;

    // Camera update throttling
    private bool _cameraUpdatePending = false;

    public float CameraX
    {
        get => _cameraX;
        set => this.RaiseAndSetIfChanged(ref _cameraX, value);
    }

    public float CameraY
    {
        get => _cameraY;
        set => this.RaiseAndSetIfChanged(ref _cameraY, value);
    }

    public float ZoomLevel
    {
        get => _zoomLevel;
        set => this.RaiseAndSetIfChanged(ref _zoomLevel, Math.Clamp(value, _minZoom, _maxZoom));
    }

    public bool IsGridVisible
    {
        get => _isGridVisible;
        set => this.RaiseAndSetIfChanged(ref _isGridVisible, value);
    }

    public bool IsGizmosVisible
    {
        get => _isGizmosVisible;
        set => this.RaiseAndSetIfChanged(ref _isGizmosVisible, value);
    }

    public bool IsHelpHidden
    {
        get => _isHelpHidden;
        set => this.RaiseAndSetIfChanged(ref _isHelpHidden, value);
    }

    public ViewportMode ViewMode
    {
        get => _viewMode;
        set => this.RaiseAndSetIfChanged(ref _viewMode, value);
    }

    public bool IsEngineReady
    {
        get => _isEngineReady;
        set => this.RaiseAndSetIfChanged(ref _isEngineReady, value);
    }

    public bool HasRenderingError
    {
        get => _hasRenderingError;
        set => this.RaiseAndSetIfChanged(ref _hasRenderingError, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    public float GridSize
    {
        get => _gridSize;
        set => this.RaiseAndSetIfChanged(ref _gridSize, Math.Max(1f, value));
    }

    public int GridSubdivisions
    {
        get => _gridSubdivisions;
        set => this.RaiseAndSetIfChanged(ref _gridSubdivisions, Math.Clamp(value, 1, 10));
    }

    public IReadOnlyList<SceneNode> SelectedNodes => _selectedNodes;
    public bool HasSelection => _selectedNodes.Count > 0;

    // Tile painting properties
    public TilemapService TilemapService => _tilemapService;
    public TilePaletteService TilePaletteService => _tilePaletteService;
    public TilePaintingService TilePaintingService => _tilePaintingService;

    // Commands
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ZoomToFitCommand { get; }
    public ICommand ZoomToSelectionCommand { get; }
    public ICommand ResetCameraCommand { get; }
    public ICommand ToggleGridCommand { get; }
    public ICommand ToggleGizmosCommand { get; }
    public ICommand FocusSelectedCommand { get; }
    public ICommand FrameAllCommand { get; }
    public ICommand ToggleHelpCommand { get; }
    public ICommand RetryInitializationCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public ViewportViewModel(EditorEngine engine, GameObjectService sceneService, ToolService toolService,
        CommandService commandService, TilemapService tilemapService, TilePaletteService tilePaletteService,
        TilePaintingService tilePaintingService)
    {
        _engine = engine;
        _sceneService = sceneService;
        _toolService = toolService;
        _commandService = commandService;
        _tilemapService = tilemapService;
        _tilePaletteService = tilePaletteService;
        _tilePaintingService = tilePaintingService;

        // Initialize commands
        ZoomInCommand = ReactiveCommand.Create(ZoomIn);
        ZoomOutCommand = ReactiveCommand.Create(ZoomOut);
        ZoomToFitCommand = ReactiveCommand.Create(ZoomToFit);
        ZoomToSelectionCommand = ReactiveCommand.Create(ZoomToSelection, this.WhenAnyValue(x => x.HasSelection));
        ResetCameraCommand = ReactiveCommand.Create(ResetCamera);
        ToggleGridCommand = ReactiveCommand.Create(ToggleGrid);
        ToggleGizmosCommand = ReactiveCommand.Create(ToggleGizmos);
        FocusSelectedCommand = ReactiveCommand.Create(FocusSelected, this.WhenAnyValue(x => x.HasSelection));
        FrameAllCommand = ReactiveCommand.Create(FrameAll);
        ToggleHelpCommand = ReactiveCommand.Create(ToggleHelp);
        RetryInitializationCommand = ReactiveCommand.Create(RetryInitialization);
        ClearErrorCommand = ReactiveCommand.Create(ClearError);

        // Subscribe to engine events
        _engine.EngineInitialized += OnEngineInitialized;
        _engine.EngineShutdown += OnEngineShutdown;

        // Subscribe to scene events
        _sceneService.GameObjectSelected += OnNodeSelected;
        _sceneService.HierarchyChanged += OnHierarchyChanged;

        // Monitor property changes with throttling for camera updates
        this.WhenAnyValue(x => x.CameraX, x => x.CameraY)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(_ => ScheduleCameraUpdate());

        this.WhenAnyValue(x => x.ZoomLevel)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(_ => ScheduleCameraUpdate());

        this.WhenAnyValue(x => x.IsGridVisible)
            .Subscribe(visible => SafeUpdateGridVisibility());

        // Update selection binding
        this.WhenAnyValue(x => x._selectedNodes.Count)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(HasSelection)));
    }

    public void Initialize()
    {
        try
        {
            ResetCamera();
            IsEngineReady = _engine.IsInitialized;
            ClearError();

            // Initialize grid settings
            SafeUpdateGridVisibility();
            SafeUpdateGridProperties();
        }
        catch (Exception ex)
        {
            SetError($"Initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Schedule a camera update to happen outside of render context
    /// </summary>
    private void ScheduleCameraUpdate()
    {
        if (_cameraUpdatePending) return;

        _cameraUpdatePending = true;

        // Use dispatcher to update camera state outside of render context
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _cameraUpdatePending = false;
            SafeUpdateEngineCamera();
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// Handle mouse input in the viewport with tool support
    /// </summary>
    public void HandleMouseInput(ViewportMouseEventArgs args)
    {
        try
        {
            var worldPos = ScreenToWorld(args.X, args.Y);
            args.WorldX = worldPos.x;
            args.WorldY = worldPos.y;

            var currentTool = _toolService.CurrentTool;
            if (currentTool != null)
            {
                // Let the current tool handle the input
                switch (args.Type)
                {
                    case ViewportMouseEventType.LeftClick:
                        currentTool.OnMouseDown(worldPos.x, worldPos.y, args.Modifiers);
                        break;

                    case ViewportMouseEventType.RightClick:
                        currentTool.OnRightClick(worldPos.x, worldPos.y, args.Modifiers);
                        break;

                    case ViewportMouseEventType.Drag:
                        currentTool.OnDrag(worldPos.x, worldPos.y, args.Modifiers);
                        break;

                    case ViewportMouseEventType.MouseUp:
                        currentTool.OnMouseUp(worldPos.x, worldPos.y, args.Modifiers);
                        break;
                }
            }
            else
            {
                // Default behavior when no tool is selected
                switch (args.Type)
                {
                    case ViewportMouseEventType.LeftClick:
                        HandleDefaultLeftClick(worldPos.x, worldPos.y, args.Modifiers);
                        break;

                    case ViewportMouseEventType.RightClick:
                        HandleDefaultRightClick(worldPos.x, worldPos.y, args.Modifiers);
                        break;

                    case ViewportMouseEventType.Wheel:
                        HandleMouseWheel(args.Delta, args.X, args.Y);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] Mouse input error: {ex.Message}");
        }
    }

    /// <summary>
    /// Default left click behavior (selection)
    /// </summary>
    private void HandleDefaultLeftClick(float worldX, float worldY, ViewportInputModifiers modifiers)
    {
        try
        {
            PerformSelection(worldX, worldY, modifiers.HasFlag(ViewportInputModifiers.Control));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] Default left click error: {ex.Message}");
        }
    }

    /// <summary>
    /// Default right click behavior (context menu)
    /// </summary>
    private void HandleDefaultRightClick(float worldX, float worldY, ViewportInputModifiers modifiers)
    {
        try
        {
            // Show context menu or other right-click behavior
            Console.WriteLine($"Right click at world position: ({worldX:F2}, {worldY:F2})");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] Default right click error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle mouse wheel for zooming
    /// </summary>
    private void HandleMouseWheel(float delta, int screenX, int screenY)
    {
        try
        {
            var worldPos = ScreenToWorld(screenX, screenY);

            float zoomFactor = delta > 0 ? 1.1f : 0.9f;
            float newZoom = ZoomLevel * zoomFactor;

            float deltaX = (worldPos.x - CameraX) * (1f - zoomFactor);
            float deltaY = (worldPos.y - CameraY) * (1f - zoomFactor);

            ZoomLevel = newZoom;
            CameraX += deltaX;
            CameraY += deltaY;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] Mouse wheel error: {ex.Message}");
        }
    }

    /// <summary>
    /// Convert screen coordinates to world coordinates with fallback
    /// </summary>
    public (float x, float y) ScreenToWorld(int screenX, int screenY)
    {
        try
        {
            if (_engine?.IsInitialized == true)
            {
                EngineInterop.Engine_ScreenToWorld(_engine.Context, screenX, screenY, out float worldX, out float worldY);
                return (worldX, worldY);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] ScreenToWorld engine error: {ex.Message}");
        }

        // Fallback calculation
        try
        {
            float worldX = (screenX - 400) / ZoomLevel + CameraX; // Assuming 800x600 viewport
            float worldY = (screenY - 300) / ZoomLevel + CameraY;
            return (worldX, worldY);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] ScreenToWorld fallback error: {ex.Message}");
            return (screenX, screenY);
        }
    }

    /// <summary>
    /// Convert world coordinates to screen coordinates with fallback
    /// </summary>
    public (int x, int y) WorldToScreen(float worldX, float worldY)
    {
        try
        {
            if (_engine?.IsInitialized == true)
            {
                EngineInterop.Engine_WorldToScreen(_engine.Context, worldX, worldY, out int screenX, out int screenY);
                return (screenX, screenY);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] WorldToScreen engine error: {ex.Message}");
        }

        // Fallback calculation
        try
        {
            int screenX = (int)((worldX - CameraX) * ZoomLevel + 400);
            int screenY = (int)((worldY - CameraY) * ZoomLevel + 300);
            return (screenX, screenY);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] WorldToScreen fallback error: {ex.Message}");
            return ((int)worldX, (int)worldY);
        }
    }

    /// <summary>
    /// Get the current viewport bounds in world space with fallback
    /// </summary>
    public (float minX, float minY, float maxX, float maxY) GetViewportBounds()
    {
        try
        {
            if (_engine?.IsInitialized == true)
            {
                EngineInterop.Render_GetCameraBounds(_engine.Context, out float minX, out float minY, out float maxX, out float maxY);
                return (minX, minY, maxX, maxY);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] GetViewportBounds engine error: {ex.Message}");
        }

        // Fallback calculation
        try
        {
            float halfWidth = 400 / ZoomLevel;
            float halfHeight = 300 / ZoomLevel;
            return (CameraX - halfWidth, CameraY - halfHeight, CameraX + halfWidth, CameraY + halfHeight);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] GetViewportBounds fallback error: {ex.Message}");
            return (-100, -100, 100, 100);
        }
    }

    /// <summary>
    /// Snap a world position to the grid
    /// </summary>
    public (float x, float y) SnapToGrid(float worldX, float worldY)
    {
        try
        {
            if (_engine?.IsInitialized == true)
            {
                EngineInterop.Grid_SnapPosition(_engine.Context, worldX, worldY, out float snappedX, out float snappedY);
                return (snappedX, snappedY);
            }
            else
            {
                float snappedX = MathF.Round(worldX / GridSize) * GridSize;
                float snappedY = MathF.Round(worldY / GridSize) * GridSize;
                return (snappedX, snappedY);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] SnapToGrid error: {ex.Message}");
            return (worldX, worldY);
        }
    }

    // Safe wrapper methods for engine interaction
    private void SafeUpdateEngineCamera()
    {
        try
        {
            if (_engine?.IsInitialized == true)
            {
                EngineInterop.Engine_SetCameraPosition(_engine.Context, CameraX, CameraY);
                EngineInterop.Engine_SetCameraZoom(_engine.Context, ZoomLevel);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] Camera update error: {ex.Message}");
        }
    }

    private void SafeUpdateGridVisibility()
    {
        try
        {
            if (_engine?.IsInitialized == true)
            {
                EngineInterop.Engine_SetGridVisible(_engine.Context, IsGridVisible ? 1 : 0);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] Grid visibility error: {ex.Message}");
        }
    }

    private void SafeUpdateGridProperties()
    {
        try
        {
            if (_engine?.IsInitialized == true)
            {
                EngineInterop.Engine_SetGridProperties(_engine.Context, GridSize, GridSubdivisions,
                    0.4f, 0.4f, 0.4f, 0.8f); // Gray grid color
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] Grid properties error: {ex.Message}");
        }
    }

    private void PerformSelection(float worldX, float worldY, bool addToSelection)
    {
        try
        {
            if (_engine?.IsInitialized == true)
            {
                var screenPos = WorldToScreen(worldX, worldY);
                var pickedEntity = EngineInterop.Engine_PickEntity(_engine.Context, screenPos.x, screenPos.y);

                if (pickedEntity.IsValid)
                {
                    if (addToSelection)
                    {
                        EngineInterop.Selection_AddToSelection(_engine.Context, pickedEntity);
                    }
                    else
                    {
                        EngineInterop.Selection_SelectEntity(_engine.Context, pickedEntity);
                    }
                }
                else if (!addToSelection)
                {
                    EngineInterop.Selection_DeselectAll(_engine.Context);
                }
            }

            Console.WriteLine($"Selection at world position: ({worldX:F2}, {worldY:F2})");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] Selection error: {ex.Message}");
        }
    }

    private void OnEngineInitialized(object? sender, EventArgs e)
    {
        IsEngineReady = true;
        ClearError();

        // Initialize engine settings
        SafeUpdateEngineCamera();
        SafeUpdateGridVisibility();
        SafeUpdateGridProperties();
    }

    private void OnEngineShutdown(object? sender, EventArgs e)
    {
        IsEngineReady = false;
    }

    private void OnNodeSelected(object? sender, SceneNode node)
    {
        try
        {
            if (!_selectedNodes.Contains(node))
            {
                _selectedNodes.Clear();
                _selectedNodes.Add(node);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] Node selection error: {ex.Message}");
        }
    }

    private void OnHierarchyChanged(object? sender, EventArgs e)
    {
        try
        {
            for (int i = _selectedNodes.Count - 1; i >= 0; i--)
            {
                if (_selectedNodes[i].Entity?.IsValid != true)
                {
                    _selectedNodes.RemoveAt(i);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportViewModel] Hierarchy change error: {ex.Message}");
        }
    }

    // Command implementations
    private void ZoomIn() => ZoomLevel *= 1.2f;
    private void ZoomOut() => ZoomLevel /= 1.2f;
    private void ResetCamera()
    {
        CameraX = 0f;
        CameraY = 0f;
        ZoomLevel = 1f;
    }
    private void ToggleGrid() => IsGridVisible = !IsGridVisible;
    private void ToggleGizmos() => IsGizmosVisible = !IsGizmosVisible;
    private void FocusSelected() => ZoomToSelection();
    private void FrameAll() => ZoomToFit();
    private void ToggleHelp() => IsHelpHidden = !IsHelpHidden;

    private void ZoomToFit()
    {
        // Implementation for zooming to fit all content
        ResetCamera(); // Simple fallback
    }

    private void ZoomToSelection()
    {
        // Implementation for zooming to selected entities
        if (_selectedNodes.Count > 0)
        {
            // Focus on first selected node
            var node = _selectedNodes[0];
            if (node.Entity?.IsValid == true)
            {
                try
                {
                    EngineInterop.Engine_GetEntityWorldPosition(_engine.Context,
                        new EntityId { id = (uint)node.Entity.Id }, out float x, out float y);
                    CameraX = x;
                    CameraY = y;
                    ZoomLevel = 1.0f;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ViewportViewModel] ZoomToSelection error: {ex.Message}");
                }
            }
        }
    }

    private void RetryInitialization()
    {
        try
        {
            ClearError();
            var success = _engine?.Initialize() ?? false;
            if (!success)
            {
                SetError("Engine initialization failed. Check console for details.");
            }
        }
        catch (Exception ex)
        {
            SetError($"Retry failed: {ex.Message}");
        }
    }

    private void ClearError()
    {
        HasRenderingError = false;
        ErrorMessage = string.Empty;
    }

    private void SetError(string message)
    {
        HasRenderingError = true;
        ErrorMessage = message;
        Console.Error.WriteLine($"[ViewportViewModel] Error: {message}");
    }
}