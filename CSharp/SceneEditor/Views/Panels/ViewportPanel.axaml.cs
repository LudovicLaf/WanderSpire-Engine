// File: CSharp/SceneEditor/Views/Panels/ViewportPanel.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using ReactiveUI;
using SceneEditor.Services;
using SceneEditor.ViewModels;
using System;
using System.Linq;

namespace SceneEditor.Views.Panels;

public partial class ViewportPanel : UserControl
{
    private readonly DispatcherTimer _updateTimer;
    private readonly EditorEngine _engine;
    private readonly GameObjectService _sceneService;

    public ViewportPanel()
    {
        InitializeComponent();

        _engine = App.GetService<EditorEngine>();
        _sceneService = App.GetService<GameObjectService>();

        // Setup UI update timer
        _updateTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, OnUpdateTick);

        this.Loaded += ViewportPanel_Loaded;
        this.Unloaded += ViewportPanel_Unloaded;

        // Wire up event handlers for buttons that need code-behind
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        // Find the performance toggle button and wire up the click event
        this.Loaded += (s, e) =>
        {
            // Find all buttons and check for the performance display toggle
            var buttons = this.GetLogicalDescendants().OfType<Button>();
            foreach (var button in buttons)
            {
                var toolTip = ToolTip.GetTip(button)?.ToString();
                if (toolTip?.Contains("Toggle Performance Display") == true)
                {
                    button.Click += TogglePerformanceDisplay_Click;
                    break;
                }
            }
        };
    }

    private void TogglePerformanceDisplay_Click(object? sender, RoutedEventArgs e)
    {
        TogglePerformanceDisplay();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ViewportViewModel viewModel)
        {
            SetupViewModelBindings(viewModel);
        }
    }

    private void SetupViewModelBindings(ViewportViewModel viewModel)
    {
        // Subscribe to property changes for UI updates
        viewModel.WhenAnyValue(
            x => x.CameraX,
            x => x.CameraY,
            x => x.ZoomLevel,
            x => x.IsGridVisible,
            x => x.IsGizmosVisible)
            .Subscribe(_ => UpdateViewportDisplay());

        // Update status display when selection changes
        _sceneService.GameObjectSelected += OnNodeSelected;
    }

    private void ViewportPanel_Loaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            Console.WriteLine("[ViewportPanel] Loading...");

            // Initialize the native viewport
            if (OpenGLViewport != null)
            {
                OpenGLViewport.Focus();
            }

            // Start UI update timer
            _updateTimer.Start();

            // Setup initial state
            UpdateEngineStatus();
            UpdatePerformanceDisplay();

            Console.WriteLine("[ViewportPanel] Loaded successfully");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportPanel] Load error: {ex}");
        }
    }

    private void ViewportPanel_Unloaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            Console.WriteLine("[ViewportPanel] Unloading...");

            _updateTimer.Stop();

            if (_sceneService != null)
            {
                _sceneService.GameObjectSelected -= OnNodeSelected;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportPanel] Unload error: {ex}");
        }
    }

    private void OnUpdateTick(object? sender, EventArgs e)
    {
        try
        {
            UpdateEngineStatus();
            UpdatePerformanceDisplay();
            UpdateSelectionInfo();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportPanel] Update tick error: {ex}");
        }
    }

    private void UpdateViewportDisplay()
    {
        try
        {
            // Update camera position display is handled by bindings
            // Any additional viewport-specific updates go here
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportPanel] UpdateViewportDisplay error: {ex}");
        }
    }

    private void UpdateEngineStatus()
    {
        try
        {
            if (ViewportStatus != null)
            {
                bool engineReady = _engine?.IsInitialized == true;

                if (engineReady)
                {
                    ViewportStatus.Text = "Engine Ready";
                }
                else
                {
                    ViewportStatus.Text = "Engine Initializing...";
                }

                // Update loading overlay visibility
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.IsVisible = !engineReady;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportPanel] UpdateEngineStatus error: {ex}");
        }
    }

    private void UpdatePerformanceDisplay()
    {
        try
        {
            if (PerformanceDisplay?.IsVisible == true)
            {
                var entityCount = GetEntityCount();
                var fps = GetCurrentFPS();
                var renderTime = GetRenderTime();

                if (EntityCount != null)
                    EntityCount.Text = $"{entityCount}";

                if (FpsCounter != null)
                    FpsCounter.Text = $"{fps:F0}";

                if (RenderTime != null)
                    RenderTime.Text = $"{renderTime:F1}ms";
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportPanel] UpdatePerformanceDisplay error: {ex}");
        }
    }

    private void UpdateSelectionInfo()
    {
        try
        {
            if (SelectionInfo != null)
            {
                // Get current selection from scene service
                var selectedCount = GetSelectedEntityCount();

                if (selectedCount == 0)
                {
                    SelectionInfo.Text = "No selection";
                }
                else if (selectedCount == 1)
                {
                    SelectionInfo.Text = "1 entity selected";
                }
                else
                {
                    SelectionInfo.Text = $"{selectedCount} entities selected";
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportPanel] UpdateSelectionInfo error: {ex}");
        }
    }

    private void OnNodeSelected(object? sender, SceneEditor.Models.SceneNode node)
    {
        try
        {
            // Update selection display immediately
            UpdateSelectionInfo();

            // Focus camera on selected entity if requested
            if (DataContext is ViewportViewModel viewModel && node?.Entity?.IsValid == true)
            {
                // Get entity position for potential camera focus
                try
                {
                    var transform = node.Entity.GetComponent<WanderSpire.Components.TransformComponent>("TransformComponent");
                    if (transform?.LocalPosition != null && transform.LocalPosition.Length >= 2)
                    {
                        // Store position for potential focus operation
                        // The actual focus happens when user presses F or clicks focus button
                    }
                }
                catch
                {
                    // Ignore transform access errors
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportPanel] OnNodeSelected error: {ex}");
        }
    }

    // Helper methods for metrics
    private int GetEntityCount()
    {
        try
        {
            if (_engine?.IsInitialized == true)
            {
                var entities = _engine.GetAllEntities();
                return entities.Length;
            }
        }
        catch
        {
            // Ignore errors when getting entity count
        }
        return 0;
    }

    private int GetSelectedEntityCount()
    {
        try
        {
            // This would need to be implemented in SceneService
            // For now return 0 or 1 based on current selection
            return 0; // Placeholder
        }
        catch
        {
            return 0;
        }
    }

    private float GetCurrentFPS()
    {
        // This would need engine integration to get actual FPS
        // For now return estimated FPS
        return 60.0f; // Placeholder
    }

    private float GetRenderTime()
    {
        // This would need engine integration to get actual render time
        // For now return estimated time
        return 16.7f; // ~60 FPS
    }

    /// <summary>
    /// Update mouse position display from native viewport
    /// </summary>
    public void UpdateMousePosition(float worldX, float worldY)
    {
        try
        {
            if (MouseWorldPos != null)
            {
                MouseWorldPos.Text = $"World: ({worldX:F1}, {worldY:F1})";
            }

            if (MouseGridPos != null)
            {
                // Calculate grid position (assuming 32-unit grid)
                var gridX = (int)Math.Floor(worldX / 32f);
                var gridY = (int)Math.Floor(worldY / 32f);
                MouseGridPos.Text = $"Grid: ({gridX}, {gridY})";
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportPanel] UpdateMousePosition error: {ex}");
        }
    }

    /// <summary>
    /// Toggle performance display visibility
    /// </summary>
    public void TogglePerformanceDisplay()
    {
        try
        {
            if (PerformanceDisplay != null)
            {
                PerformanceDisplay.IsVisible = !PerformanceDisplay.IsVisible;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportPanel] TogglePerformanceDisplay error: {ex}");
        }
    }

    /// <summary>
    /// Show a temporary status message
    /// </summary>
    public void ShowStatusMessage(string message, TimeSpan? duration = null)
    {
        try
        {
            if (ViewportStatus != null)
            {
                ViewportStatus.Text = message;

                // Reset to default after duration
                if (duration.HasValue)
                {
                    var timer = new DispatcherTimer(duration.Value, DispatcherPriority.Background, (s, e) =>
                    {
                        if (ViewportStatus != null)
                        {
                            ViewportStatus.Text = "Ready";
                        }
                        ((DispatcherTimer)s!).Stop();
                    });
                    timer.Start();
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ViewportPanel] ShowStatusMessage error: {ex}");
        }
    }
}