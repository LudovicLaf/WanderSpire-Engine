// ScriptHost/UI/ImGuiManager.cs - Updated for command-based rendering
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using WanderSpire.Core.Events;
using static WanderSpire.Scripting.EngineInterop;

namespace WanderSpire.Scripting.UI
{
    /// <summary>
    /// Enhanced high-level manager for ImGui integration with command-based rendering support.
    /// Now properly integrates with the new render pipeline for correct layering.
    /// </summary>
    public sealed class ImGuiManager : IDisposable
    {
        public static ImGuiManager? Instance { get; private set; }

        private readonly IntPtr _context;
        private readonly List<IImGuiWindow> _windows = new();
        private readonly object _windowsLock = new();
        private readonly Dictionary<string, WindowState> _windowStates = new();

        private bool _initialized = false;
        private bool _disposed = false;
        private bool _dockingEnabled = true;
        private int _iconFontStackDepth = 0;
        private bool _iconFontMissingLogged = false;
        private ImGuiTheme _currentTheme = ImGuiTheme.Dark;

        // Font management
        private IntPtr _fontAwesome = IntPtr.Zero;
        private GCHandle _iconRangesHandle;
        private GCHandle _faFileHandle;

        // Event delegates to hold references for unsubscription
        private Action<FrameRenderEvent>? _onFrameRender;

        // Performance tracking
        private DateTime _lastFrameTime = DateTime.UtcNow;
        private float _averageFrameTime = 0f;
        private int _frameCount = 0;

        // Render command callback delegate - kept alive to prevent GC
        private RenderCallback? _renderCallback;
        private GCHandle _callbackHandle;

        private ImGuiManager(IntPtr context)
        {
            _context = context;
        }

        /// <summary>
        /// Initialize ImGui with the given engine context.
        /// Should be called once during engine startup.
        /// </summary>
        public static ImGuiManager Initialize(IntPtr context)
        {
            if (Instance != null)
                throw new InvalidOperationException("ImGuiManager already initialized");

            var manager = new ImGuiManager(context);
            var result = ImGuiInterop.ImGui_Initialize(context);

            if (result != 0)
                throw new InvalidOperationException($"Failed to initialize ImGui (error code: {result})");

            manager._initialized = true;
            Instance = manager;

            // Load fonts (retrieve from native side)
            manager.LoadFonts();

            // Subscribe to frame render events to submit render commands
            manager._onFrameRender = ev => manager.OnFrameRender();
            EventBus.FrameRender += manager._onFrameRender;

            // Set initial display size and theme
            manager.UpdateDisplaySize();
            manager.ApplyTheme(ImGuiTheme.Dark);

            if (manager._dockingEnabled)
                ImGui.SetDockingEnabled(true);

            // Create the render callback and keep it alive
            manager._renderCallback = manager.RenderImGuiCallback;
            manager._callbackHandle = GCHandle.Alloc(manager._renderCallback);

            Console.WriteLine("[ImGuiManager] Successfully initialized with command-based rendering");
            return manager;
        }

        /// <summary>
        /// Load FontAwesome font 
        /// </summary>
        private void LoadFonts()
        {
            if (_fontAwesome != IntPtr.Zero)
                return;

            // Get the FontAwesome font that was loaded on the native side
            _fontAwesome = ImGuiInterop.ImGui_GetFontAwesome();

            if (_fontAwesome != IntPtr.Zero)
            {
                Console.WriteLine("[ImGuiManager] FontAwesome font retrieved from native side");
            }
            else
            {
                Console.WriteLine("[ImGuiManager] FontAwesome font not available from native side");
            }
        }

        /// <summary>
        /// Push FontAwesome font for icon rendering.
        /// </summary>
        public void PushIconFont()
        {
            if (_fontAwesome != IntPtr.Zero)
            {
                ImGui.PushFont(_fontAwesome);
                _iconFontStackDepth++;
            }
            else if (!_iconFontMissingLogged)
            {
                Console.Error.WriteLine("[ImGuiManager] FontAwesome font has not been loaded");
                _iconFontMissingLogged = true;
            }
        }

        /// <summary>
        /// Pop FontAwesome font after icon rendering.
        /// </summary>
        public void PopIconFont()
        {
            if (_iconFontStackDepth > 0)
            {
                ImGui.PopFont();
                _iconFontStackDepth--;
            }
        }

        /// <summary>
        /// Update display size based on current window size.
        /// Called automatically during initialization and should be called on window resize.
        /// </summary>
        public void UpdateDisplaySize()
        {
            if (!_initialized || _disposed) return;

            ImGuiInterop.Engine_GetWindowSize(_context, out int width, out int height);
            if (width > 0 && height > 0)
            {
                ImGuiInterop.ImGui_SetDisplaySize(_context, width, height);
            }
        }

        /// <summary>
        /// Register a UI window to be rendered each frame.
        /// </summary>
        public void RegisterWindow(IImGuiWindow window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            lock (_windowsLock)
            {
                if (!_windows.Contains(window))
                {
                    _windows.Add(window);

                    // Load window state if it exists
                    string windowId = GetWindowId(window);
                    if (_windowStates.TryGetValue(windowId, out var state))
                    {
                        window.IsVisible = state.IsVisible;
                    }

                    Console.WriteLine($"[ImGuiManager] Registered window: {window.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// Unregister a UI window.
        /// </summary>
        public void UnregisterWindow(IImGuiWindow window)
        {
            if (window == null) return;

            lock (_windowsLock)
            {
                if (_windows.Remove(window))
                {
                    // Save window state
                    string windowId = GetWindowId(window);
                    _windowStates[windowId] = new WindowState
                    {
                        IsVisible = window.IsVisible,
                        LastAccessed = DateTime.UtcNow
                    };

                    Console.WriteLine($"[ImGuiManager] Unregistered window: {window.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// Process SDL events for ImGui. Call this before your own event handling.
        /// Returns true if ImGui consumed the event.
        /// </summary>
        public bool ProcessEvent(IntPtr sdlEvent)
        {
            if (!_initialized || _disposed)
                return false;

            return ImGuiInterop.ImGui_ProcessEvent(_context, sdlEvent) != 0;
        }

        /// <summary>
        /// Check if ImGui wants to capture mouse input.
        /// Use this to avoid processing mouse events in your game when ImGui is using them.
        /// </summary>
        public bool WantCaptureMouse =>
            _initialized && !_disposed && ImGuiInterop.ImGui_WantCaptureMouse(_context) != 0;

        /// <summary>
        /// Check if ImGui wants to capture keyboard input.
        /// Use this to avoid processing keyboard events in your game when ImGui is using them.
        /// </summary>
        public bool WantCaptureKeyboard =>
            _initialized && !_disposed && ImGuiInterop.ImGui_WantCaptureKeyboard(_context) != 0;

        /// <summary>
        /// Set display size (call on window resize).
        /// </summary>
        public void SetDisplaySize(float width, float height)
        {
            if (!_initialized || _disposed) return;
            ImGuiInterop.ImGui_SetDisplaySize(_context, width, height);
        }

        /// <summary>
        /// Apply a visual theme to ImGui.
        /// </summary>
        public void ApplyTheme(ImGuiTheme theme)
        {
            if (!_initialized || _disposed) return;

            _currentTheme = theme;
            Console.WriteLine($"[ImGuiManager] Applied theme: {theme}");
        }

        /// <summary>
        /// Get statistics about the ImGui manager.
        /// </summary>
        public ImGuiStats GetStats()
        {
            lock (_windowsLock)
            {
                return new ImGuiStats
                {
                    TotalWindows = _windows.Count,
                    VisibleWindows = _windows.Count(w => w.IsVisible),
                    AverageFrameTime = _averageFrameTime,
                    Theme = _currentTheme,
                    WantCaptureMouse = WantCaptureMouse,
                    WantCaptureKeyboard = WantCaptureKeyboard
                };
            }
        }

        /// <summary>
        /// Get a list of all registered windows.
        /// </summary>
        public IReadOnlyList<IImGuiWindow> GetRegisteredWindows()
        {
            lock (_windowsLock)
            {
                return _windows.ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Find a window by its type.
        /// </summary>
        public T? FindWindow<T>() where T : class, IImGuiWindow
        {
            lock (_windowsLock)
            {
                return _windows.OfType<T>().FirstOrDefault();
            }
        }

        /// <summary>
        /// Show or hide all windows of a specific type.
        /// </summary>
        public void SetWindowVisibility<T>(bool visible) where T : class, IImGuiWindow
        {
            lock (_windowsLock)
            {
                foreach (var window in _windows.OfType<T>())
                {
                    window.IsVisible = visible;
                }
            }
        }

        /// <summary>
        /// Toggle visibility of all windows of a specific type.
        /// </summary>
        public void ToggleWindowVisibility<T>() where T : class, IImGuiWindow
        {
            lock (_windowsLock)
            {
                foreach (var window in _windows.OfType<T>())
                {
                    window.IsVisible = !window.IsVisible;
                }
            }
        }

        /// <summary>
        /// Submit ImGui rendering commands to the render pipeline.
        /// This ensures ImGui renders at the correct layer (UI) above entities.
        /// </summary>
        private void OnFrameRender()
        {
            if (!_initialized || _disposed) return;

            // Update performance metrics
            var now = DateTime.UtcNow;
            var frameTime = (float)(now - _lastFrameTime).TotalMilliseconds;
            _lastFrameTime = now;

            _frameCount++;
            _averageFrameTime = (_averageFrameTime * (_frameCount - 1) + frameTime) / _frameCount;

            // Submit ImGui rendering at UI layer with high priority
            // This ensures it renders AFTER terrain and entities but BEFORE debug overlays
            Render_SubmitCustom(
                _context,
                _renderCallback!,
                IntPtr.Zero,
                (int)RenderLayer.UI,
                0 // Base UI order
            );
        }

        /// <summary>
        /// The actual ImGui rendering callback executed by the render pipeline.
        /// This runs during the UI render layer pass.
        /// </summary>
        private void RenderImGuiCallback(IntPtr userData)
        {
            try
            {
                // Start new ImGui frame
                ImGuiInterop.ImGui_NewFrame(_context);

                if (_dockingEnabled)
                {
                    // Full-viewport dockspace (passthrough background so the
                    // game still renders under our transparent root window).
                    ImGui.DockSpaceOverViewport(ImGuiDockNodeFlags.PassthruCentralNode);
                }

                // Render all registered windows
                lock (_windowsLock)
                {
                    foreach (var window in _windows.ToList()) // Create copy to avoid modification during iteration
                    {
                        try
                        {
                            if (window.IsVisible)
                            {
                                window.Render();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[ImGuiManager] Error rendering window {window.GetType().Name}: {ex}");
                            // Optionally disable the problematic window
                            window.IsVisible = false;
                        }
                    }
                }

                // Render performance overlay if enabled
                RenderPerformanceOverlay();

                // Render ImGui - this submits the draw data to OpenGL
                ImGuiInterop.ImGui_Render(_context);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGuiManager] Critical error in render callback: {ex}");
            }
        }

        private void RenderPerformanceOverlay()
        {
            // This would render a small performance overlay in the corner
            // Currently disabled as it would require additional ImGui window functions
        }

        private string GetWindowId(IImGuiWindow window)
        {
            return $"{window.GetType().Name}_{window.Title}";
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Unsubscribe from events
            if (_onFrameRender != null)
                EventBus.FrameRender -= _onFrameRender;

            // Clean up callback handle
            if (_callbackHandle.IsAllocated)
                _callbackHandle.Free();

            // Clean up windows
            lock (_windowsLock)
            {
                foreach (var window in _windows)
                {
                    if (window is IDisposable disposable)
                        disposable.Dispose();
                }
                _windows.Clear();
            }

            // Clean up font resources
            _fontAwesome = IntPtr.Zero;
            if (_iconRangesHandle.IsAllocated)
                _iconRangesHandle.Free();

            // Shutdown ImGui
            if (_initialized)
            {
                ImGuiInterop.ImGui_Shutdown(_context);
                Console.WriteLine("[ImGuiManager] ImGui shutdown complete");
            }

            _disposed = true;
            Instance = null;
        }
    }

    /// <summary>
    /// ImGui theme enumeration.
    /// </summary>
    public enum ImGuiTheme
    {
        Dark,
        Light,
        Classic,
        Custom
    }

    /// <summary>
    /// Statistics about the ImGui manager.
    /// </summary>
    public struct ImGuiStats
    {
        public int TotalWindows;
        public int VisibleWindows;
        public float AverageFrameTime;
        public ImGuiTheme Theme;
        public bool WantCaptureMouse;
        public bool WantCaptureKeyboard;
    }

    /// <summary>
    /// Internal window state for persistence.
    /// </summary>
    internal struct WindowState
    {
        public bool IsVisible;
        public DateTime LastAccessed;
    }
}