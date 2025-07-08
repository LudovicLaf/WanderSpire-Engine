// Game/Systems/DebugUISystem.cs - Fixed initialization and input handling
using Game.Systems.UI;
using ScriptHost;
using System;
using WanderSpire.Core.Events;
using WanderSpire.Scripting;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.EngineInterop;

namespace Game.Systems
{
    /// <summary>
    /// Enhanced debug UI system with proper initialization and input handling.
    /// Toggles all debug windows via tilde key and ensures proper render layering.
    /// Debug overlays render at the Debug layer (above UI) for maximum visibility.
    /// </summary>
    public sealed class DebugUISystem : ITickReceiver, IDisposable
    {
        public static DebugUISystem? Instance { get; private set; }

        private ImGuiManager? _imguiManager;
        private bool _debugUIEnabled = false;
        private bool _initialized = false;
        private bool _showRenderStats = false;
        private bool _lastTildePressed = false;
        private bool _lastF12Pressed = false;

        // All debug windows:
        private EngineDebugWindow? _engineDebugWindow;
        private EntityInspectorWindow? _entityInspectorWindow;
        private PerformanceWindow? _performanceWindow;
        private WorldViewerWindow? _worldViewerWindow;
        private ComponentRegistryWindow? _componentRegistryWindow;
        private EventLogWindow? _eventLogWindow;
        private ScriptDebugWindow? _scriptDebugWindow;
        private CameraDebugWindow? _cameraDebugWindow;
        private SystemsDebugWindow? _systemsDebugWindow;
        private EntitySearchWindow? _entitySearchWindow;
        private AssetDebugWindow? _assetDebugWindow;
        private AIDebugWindow? _aiDebugWindow;
        private CombatDebugWindow? _combatDebugWindow;
        private MemoryDebugWindow? _memoryDebugWindow;
        private PrefabDebugWindow? _prefabDebugWindow;
        private InputDebugWindow? _inputDebugWindow;
        private InputCaptureDebugWindow? _inputCaptureDebugWindow;

        // Enhanced debug windows:
        private DebugTerminalWindow? _debugTerminalWindow;
        private ImGuiDemoWindow? _imguiDemoWindow;

        private Action<FrameRenderEvent>? _onFrameRender;

        public DebugUISystem()
        {
            Instance = this;
            _onFrameRender = OnFrame;
            EventBus.FrameRender += _onFrameRender;
            Input.Initialize();

            Console.WriteLine("[DebugUISystem] Initialized - will attempt to create ImGuiManager");
        }

        public void OnTick(float dt)
        {
            if (!_initialized)
            {
                Initialize();
                _initialized = true;
            }


        }

        /// <summary>
        /// Does ImGui want to capture input right now?
        /// </summary>
        public bool WantsInput()
            => _debugUIEnabled
            && _imguiManager != null
            && (_imguiManager.WantCaptureMouse || _imguiManager.WantCaptureKeyboard);

        private void CheckInput()
        {
            try
            {
                // Check for tilde key press (with edge detection)
                bool tildePressed = Input.GetKey(KeyCode.Grave);
                if (tildePressed && !_lastTildePressed)
                {
                    Console.WriteLine("[DebugUISystem] Tilde key detected!");
                    ToggleDebugUI();
                }
                _lastTildePressed = tildePressed;

                // Check for F12 key press (with edge detection)
                bool f12Pressed = Input.GetKey(KeyCode.F12);
                if (f12Pressed && !_lastF12Pressed)
                {
                    Console.WriteLine("[DebugUISystem] F12 key detected!");
                    ToggleRenderStats();
                }
                _lastF12Pressed = f12Pressed;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DebugUISystem] Error checking input: {ex.Message}");
            }
        }

        private void OnFrame(FrameRenderEvent _)
        {
            // Submit debug overlays to render pipeline if enabled
            if (_debugUIEnabled && _showRenderStats)
            {
                SubmitDebugOverlays();
            }

            // Check for input every tick
            CheckInput();
        }

        private void Initialize()
        {
            try
            {
                var engine = Engine.Instance;
                if (engine == null)
                {
                    Console.Error.WriteLine("[DebugUISystem] Engine.Instance is null - cannot initialize");
                    return;
                }

                Console.WriteLine("[DebugUISystem] Engine found, attempting to initialize ImGuiManager...");

                // Try to get existing ImGuiManager or create a new one
                _imguiManager = ImGuiManager.Instance;
                if (_imguiManager == null)
                {
                    Console.WriteLine("[DebugUISystem] ImGuiManager not found, creating new instance...");
                    try
                    {
                        _imguiManager = ImGuiManager.Initialize(engine.Context);
                        Console.WriteLine("[DebugUISystem] ImGuiManager created successfully!");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[DebugUISystem] Failed to create ImGuiManager: {ex.Message}");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("[DebugUISystem] Found existing ImGuiManager instance");
                }

                CreateDebugWindows();

                Console.WriteLine("[DebugUISystem] Initialized successfully!");
                Console.WriteLine("  Controls:");
                Console.WriteLine("    ` (tilde/grave) - Toggle Debug UI");
                Console.WriteLine("    F12 - Toggle Render Stats Overlay");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DebugUISystem] Initialization failed: {ex.Message}");
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void CreateDebugWindows()
        {
            try
            {
                Console.WriteLine("[DebugUISystem] Creating debug windows...");

                // Instantiate each window (all start hidden)
                _engineDebugWindow = new EngineDebugWindow { IsVisible = false };
                _entityInspectorWindow = new EntityInspectorWindow { IsVisible = false };
                _performanceWindow = new PerformanceWindow { IsVisible = false };
                _worldViewerWindow = new WorldViewerWindow { IsVisible = false };
                _componentRegistryWindow = new ComponentRegistryWindow { IsVisible = false };
                _eventLogWindow = new EventLogWindow { IsVisible = false };
                _scriptDebugWindow = new ScriptDebugWindow { IsVisible = false };
                _cameraDebugWindow = new CameraDebugWindow { IsVisible = false };
                _systemsDebugWindow = new SystemsDebugWindow { IsVisible = false };
                _entitySearchWindow = new EntitySearchWindow { IsVisible = false };
                _assetDebugWindow = new AssetDebugWindow { IsVisible = false };
                _aiDebugWindow = new AIDebugWindow { IsVisible = false };
                _combatDebugWindow = new CombatDebugWindow { IsVisible = false };
                _memoryDebugWindow = new MemoryDebugWindow { IsVisible = false };
                _prefabDebugWindow = new PrefabDebugWindow { IsVisible = false };
                _inputDebugWindow = new InputDebugWindow { IsVisible = false };
                _inputCaptureDebugWindow = new InputCaptureDebugWindow { IsVisible = false };

                // Enhanced debug windows
                _debugTerminalWindow = new DebugTerminalWindow { IsVisible = false };
                _imguiDemoWindow = new ImGuiDemoWindow { IsVisible = false };

                Console.WriteLine("[DebugUISystem] All debug windows created successfully");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DebugUISystem] Error creating debug windows: {ex.Message}");
            }
        }

        private void ToggleDebugUI()
        {
            try
            {
                if (_imguiManager == null)
                {
                    Console.Error.WriteLine("[DebugUISystem] Cannot toggle UI - ImGuiManager is null");
                    return;
                }

                _debugUIEnabled = !_debugUIEnabled;

                if (_debugUIEnabled)
                {
                    Console.WriteLine("[DebugUISystem] ENABLING Debug UI...");
                    RegisterAllWindows();

                    // Show main windows
                    if (_engineDebugWindow != null)
                    {
                        _engineDebugWindow.IsVisible = true;
                        Console.WriteLine("[DebugUISystem] Engine debug window enabled");
                    }

                    if (_debugTerminalWindow != null)
                    {
                        _debugTerminalWindow.IsVisible = true;
                        Console.WriteLine("[DebugUISystem] Debug terminal enabled");
                    }

                    Console.WriteLine("[DebugUISystem] Debug UI ENABLED - All windows available through docking");
                }
                else
                {
                    Console.WriteLine("[DebugUISystem] DISABLING Debug UI...");
                    UnregisterAllWindows();
                    Console.WriteLine("[DebugUISystem] Debug UI DISABLED");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DebugUISystem] Error toggling debug UI: {ex.Message}");
            }
        }

        private void ToggleRenderStats()
        {
            _showRenderStats = !_showRenderStats;
            Console.WriteLine($"[DebugUISystem] Render stats overlay: {(_showRenderStats ? "ENABLED" : "DISABLED")}");
        }

        /// <summary>
        /// Submit debug overlays to the render command pipeline.
        /// These render at the Debug layer (2000) above all UI elements.
        /// </summary>
        private void SubmitDebugOverlays()
        {
            if (Engine.Instance?.Context == null) return;

            try
            {
                // Submit render statistics overlay at debug layer with high priority
                Render_SubmitCustom(
                    Engine.Instance.Context,
                    RenderStatsOverlay,
                    IntPtr.Zero,
                    (int)RenderLayer.Debug,
                    1000 // High priority within debug layer
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DebugUISystem] Error submitting debug overlays: {ex.Message}");
            }
        }

        /// <summary>
        /// Render performance and render command statistics overlay.
        /// </summary>
        private static void RenderStatsOverlay(IntPtr userData)
        {
            try
            {
                var engine = Engine.Instance;
                if (engine?.Context == null) return;

                // Get render command statistics
                int commandCount = Render_GetCommandCount(engine.Context);

                // Get camera bounds for positioning
                Render_GetCameraBounds(engine.Context,
                    out float minX, out float minY,
                    out float maxX, out float maxY);

                // Position overlay in top-right corner of screen
                var windowSize = new System.Numerics.Vector2(300, 200);
                var padding = 10f;

                // Calculate screen position (this is a simplified approach)
                var screenPos = new System.Numerics.Vector2(
                    maxX - windowSize.X - padding,
                    minY + padding
                );

                ImGui.SetNextWindowPos(screenPos);
                ImGui.SetNextWindowSize(windowSize);
                ImGui.SetNextWindowBgAlpha(0.8f);

                if (ImGui.Begin("Render Stats",
                    ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoCollapse))
                {
                    ImGui.Text("=== RENDER PIPELINE ===");
                    ImGui.Text($"Queued Commands: {commandCount}");
                    ImGui.Text($"Camera Bounds: ({minX:F0},{minY:F0}) to ({maxX:F0},{maxY:F0})");

                    ImGui.Separator();
                    ImGui.Text("=== CONTROLS ===");
                    ImGui.Text("` - Toggle Debug UI");
                    ImGui.Text("F12 - Toggle Render Stats");
                }
                ImGui.End();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DebugUISystem] Error in render stats overlay: {ex}");
            }
        }

        private void RegisterAllWindows()
        {
            if (_imguiManager == null) return;

            try
            {
                _imguiManager.RegisterWindow(_engineDebugWindow!);
                _imguiManager.RegisterWindow(_entityInspectorWindow!);
                _imguiManager.RegisterWindow(_performanceWindow!);
                _imguiManager.RegisterWindow(_worldViewerWindow!);
                _imguiManager.RegisterWindow(_componentRegistryWindow!);
                _imguiManager.RegisterWindow(_eventLogWindow!);
                _imguiManager.RegisterWindow(_scriptDebugWindow!);
                _imguiManager.RegisterWindow(_cameraDebugWindow!);
                _imguiManager.RegisterWindow(_systemsDebugWindow!);
                _imguiManager.RegisterWindow(_entitySearchWindow!);
                _imguiManager.RegisterWindow(_assetDebugWindow!);
                _imguiManager.RegisterWindow(_aiDebugWindow!);
                _imguiManager.RegisterWindow(_combatDebugWindow!);
                _imguiManager.RegisterWindow(_memoryDebugWindow!);
                _imguiManager.RegisterWindow(_prefabDebugWindow!);
                _imguiManager.RegisterWindow(_inputDebugWindow!);
                _imguiManager.RegisterWindow(_inputCaptureDebugWindow!);

                // Register enhanced windows
                _imguiManager.RegisterWindow(_debugTerminalWindow!);
                _imguiManager.RegisterWindow(_imguiDemoWindow!);

                Console.WriteLine("[DebugUISystem] All windows registered with ImGuiManager");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DebugUISystem] Error registering windows: {ex.Message}");
            }
        }

        private void UnregisterAllWindows()
        {
            if (_imguiManager == null) return;

            try
            {
                _imguiManager.UnregisterWindow(_engineDebugWindow!);
                _imguiManager.UnregisterWindow(_entityInspectorWindow!);
                _imguiManager.UnregisterWindow(_performanceWindow!);
                _imguiManager.UnregisterWindow(_worldViewerWindow!);
                _imguiManager.UnregisterWindow(_componentRegistryWindow!);
                _imguiManager.UnregisterWindow(_eventLogWindow!);
                _imguiManager.UnregisterWindow(_scriptDebugWindow!);
                _imguiManager.UnregisterWindow(_cameraDebugWindow!);
                _imguiManager.UnregisterWindow(_systemsDebugWindow!);
                _imguiManager.UnregisterWindow(_entitySearchWindow!);
                _imguiManager.UnregisterWindow(_assetDebugWindow!);
                _imguiManager.UnregisterWindow(_aiDebugWindow!);
                _imguiManager.UnregisterWindow(_combatDebugWindow!);
                _imguiManager.UnregisterWindow(_memoryDebugWindow!);
                _imguiManager.UnregisterWindow(_prefabDebugWindow!);
                _imguiManager.UnregisterWindow(_inputDebugWindow!);
                _imguiManager.UnregisterWindow(_inputCaptureDebugWindow!);

                // Unregister enhanced windows
                _imguiManager.UnregisterWindow(_debugTerminalWindow!);
                _imguiManager.UnregisterWindow(_imguiDemoWindow!);

                SetAllWindowsVisible(false);
                Console.WriteLine("[DebugUISystem] All windows unregistered from ImGuiManager");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DebugUISystem] Error unregistering windows: {ex.Message}");
            }
        }

        private void SetAllWindowsVisible(bool visible)
        {
            _engineDebugWindow!.IsVisible = visible;
            _entityInspectorWindow!.IsVisible = visible;
            _performanceWindow!.IsVisible = visible;
            _worldViewerWindow!.IsVisible = visible;
            _componentRegistryWindow!.IsVisible = visible;
            _eventLogWindow!.IsVisible = visible;
            _scriptDebugWindow!.IsVisible = visible;
            _cameraDebugWindow!.IsVisible = visible;
            _systemsDebugWindow!.IsVisible = visible;
            _entitySearchWindow!.IsVisible = visible;
            _assetDebugWindow!.IsVisible = visible;
            _aiDebugWindow!.IsVisible = visible;
            _combatDebugWindow!.IsVisible = visible;
            _memoryDebugWindow!.IsVisible = visible;
            _prefabDebugWindow!.IsVisible = visible;
            _inputDebugWindow!.IsVisible = visible;
            _inputCaptureDebugWindow!.IsVisible = visible;
            _debugTerminalWindow!.IsVisible = visible;
            _imguiDemoWindow!.IsVisible = visible;
        }

        public void Dispose()
        {
            Instance = null;
            if (_onFrameRender != null)
                EventBus.FrameRender -= _onFrameRender;

            // Dispose all windows
            (_engineDebugWindow as IDisposable)?.Dispose();
            _entityInspectorWindow?.Dispose();
            (_performanceWindow as IDisposable)?.Dispose();
            (_worldViewerWindow as IDisposable)?.Dispose();
            (_componentRegistryWindow as IDisposable)?.Dispose();
            _eventLogWindow?.Dispose();
            (_scriptDebugWindow as IDisposable)?.Dispose();
            (_cameraDebugWindow as IDisposable)?.Dispose();
            (_systemsDebugWindow as IDisposable)?.Dispose();
            _entitySearchWindow?.Dispose();
            (_assetDebugWindow as IDisposable)?.Dispose();
            (_aiDebugWindow as IDisposable)?.Dispose();
            (_combatDebugWindow as IDisposable)?.Dispose();
            (_memoryDebugWindow as IDisposable)?.Dispose();
            (_prefabDebugWindow as IDisposable)?.Dispose();
            (_inputDebugWindow as IDisposable)?.Dispose();
            (_inputCaptureDebugWindow as IDisposable)?.Dispose();
            (_debugTerminalWindow as IDisposable)?.Dispose();
            (_imguiDemoWindow as IDisposable)?.Dispose();

            // Note: Don't dispose ImGuiManager here as it's shared

            Console.WriteLine("[DebugUISystem] Disposed");
        }
    }
}