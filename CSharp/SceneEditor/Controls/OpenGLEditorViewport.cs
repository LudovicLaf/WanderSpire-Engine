// SceneEditor/Controls/OpenGLEditorViewport.cs - Fixed for proper context binding and rendering
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using SceneEditor.Services;
using SceneEditor.ViewModels;
using System;
using WanderSpire.Scripting;

namespace SceneEditor.Controls
{
    public class OpenGLEditorViewport : OpenGlControlBase
    {
        private static EngineInterop.GetProcAddressDelegate? _getProc;
        private readonly EditorEngine _engine;
        private readonly GameObjectService _sceneService;
        private readonly DispatcherTimer _updateTimer;
        private ViewportViewModel? _viewModel;

        private static readonly EngineInterop.RunInCtx_t s_runCtx = RunInRenderContext;
        private static readonly Dispatcher _uiDisp = Dispatcher.UIThread;

        private bool _isInitialized = false;
        private bool _contextReady = false;
        private int _viewportWidth = 0;
        private int _viewportHeight = 0;

        private DateTime _lastFrameTime = DateTime.Now;
        private int _frameCount = 0;
        private GlInterface? _gl;
        private bool _engineContextBound = false;

        // OpenGL constants
        private const int GL_BLEND = 0x0BE2;
        private const int GL_COLOR_BUFFER_BIT = 0x00004000;

        public static readonly StyledProperty<ViewportViewModel?> ViewModelProperty =
            AvaloniaProperty.Register<OpenGLEditorViewport, ViewportViewModel?>(nameof(ViewModel));

        public ViewportViewModel? ViewModel
        {
            get => GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        static OpenGLEditorViewport()
        {
            ViewModelProperty.Changed.Subscribe(OnViewModelChanged);
        }

        public OpenGLEditorViewport()
        {
            _engine = App.GetService<EditorEngine>();
            _sceneService = App.GetService<GameObjectService>();
            _updateTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Background, OnUpdateTick);
            this.Unloaded += OnUnloaded;
            this.SizeChanged += OnSizeChanged;
        }

        private static void RunInRenderContext(EngineInterop.RawAction fn, IntPtr user)
        {
            _uiDisp.Post(() =>
            {
                try { fn(user); } catch (Exception ex) { Console.Error.WriteLine($"[RenderCtx] Error: {ex}"); }
            }, DispatcherPriority.Render);
        }

        private static void OnViewModelChanged(AvaloniaPropertyChangedEventArgs<ViewportViewModel?> args)
        {
            if (args.Sender is OpenGLEditorViewport viewport)
            {
                viewport._viewModel = args.NewValue.GetValueOrDefault();
                viewport.RequestNextFrameRendering();
            }
        }

        protected override void OnOpenGlInit(GlInterface gl)
        {
            try
            {
                Console.WriteLine("[OpenGLEditorViewport] Initializing OpenGL context...");

                _gl = gl;
                _getProc = proc => _gl.GetProcAddress(proc);

                Console.WriteLine("OpenGL Version: " + gl.GetString(0x1F02));
                Console.WriteLine("Renderer: " + gl.GetString(0x1F01));
                Console.WriteLine("Vendor: " + gl.GetString(0x1F00));

                // Set basic OpenGL state using available methods
                gl.Enable(GL_BLEND);
                gl.ClearColor(0.15f, 0.18f, 0.22f, 1.0f);

                // Bind engine context FIRST before any rendering setup
                BindEngineContext();

                // Only proceed if context binding succeeded
                if (_engineContextBound)
                {
                    _isInitialized = _contextReady = true;

                    // Initialize engine rendering subsystem
                    InitializeEngineRendering();

                    _updateTimer.Start();
                    Console.WriteLine("[OpenGLEditorViewport] OpenGL initialization complete");
                }
                else
                {
                    Console.Error.WriteLine("[OpenGLEditorViewport] Failed to bind engine context");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OpenGL Init] Error: {ex}");
                _isInitialized = _contextReady = false;
            }
        }

        private void BindEngineContext()
        {
            if (_engineContextBound || _getProc == null) return;

            try
            {
                Console.WriteLine("[OpenGLEditorViewport] Binding engine OpenGL context...");

                // Bind the OpenGL context to the engine
                EngineInterop.Engine_BindOpenGLContext(_getProc);
                EngineInterop.Engine_RegisterRunInContext(s_runCtx);

                _engineContextBound = true;
                Console.WriteLine("[OpenGLEditorViewport] Engine context bound successfully");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Context Bind] Error: {ex}");
                _engineContextBound = false;
            }
        }

        private void InitializeEngineRendering()
        {
            if (!_engineContextBound || !_engine.IsInitialized)
            {
                Console.WriteLine("[OpenGLEditorViewport] Skipping rendering init - engine not ready");
                return;
            }

            try
            {
                Console.WriteLine("[OpenGLEditorViewport] Initializing engine rendering subsystem...");

                // Initialize rendering with current viewport size
                int width = Math.Max(_viewportWidth, 800);
                int height = Math.Max(_viewportHeight, 600);

                var result = EngineInterop.EngineInitRendering(_engine.Context, width, height);
                if (result == 0)
                {
                    Console.WriteLine("[OpenGLEditorViewport] Engine rendering initialized successfully");

                    // Notify engine that OpenGL context is ready
                    _engine.OnOpenGLContextReady();
                }
                else
                {
                    Console.Error.WriteLine($"[OpenGLEditorViewport] Engine rendering initialization failed: {result}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OpenGLEditorViewport] Engine rendering init error: {ex}");
            }
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            _updateTimer.Stop();
            _isInitialized = _contextReady = _engineContextBound = false;
            Console.WriteLine("[OpenGLEditorViewport] OpenGL context deinitialized");
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (!_isInitialized || !_contextReady || _viewportWidth <= 0 || _viewportHeight <= 0)
            {
                // Clear to dark gray while not ready
                gl.Clear(GL_COLOR_BUFFER_BIT);
                return;
            }

            UpdatePerformanceStats();

            // Set viewport
            gl.Viewport(0, 0, _viewportWidth, _viewportHeight);

            // Update engine viewport size
            if (_engineContextBound && _engine.IsInitialized)
            {
                try
                {
                    EngineInterop.Engine_SetEditorViewport(_engine.Context, 0, 0, _viewportWidth, _viewportHeight);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[OpenGLEditorViewport] Viewport update error: {ex}");
                }
            }

            // Clear the framebuffer
            gl.Clear(GL_COLOR_BUFFER_BIT);

            // Render with engine if ready
            if (_engineContextBound && _engine.IsInitialized && _engine.CanRenderSafely)
            {
                RenderWithEngine();
            }
            else
            {
                // Render a simple test pattern while engine isn't ready
                RenderTestPattern(gl);
            }
        }

        private void RenderWithEngine()
        {
            try
            {
                if (_viewModel != null)
                {
                    // Update camera before rendering
                    EngineInterop.EngineSetEditorCamera(_engine.Context,
                        _viewModel.CameraX, _viewModel.CameraY, _viewModel.ZoomLevel,
                        _viewportWidth, _viewportHeight);
                }

                // Render the frame
                EngineInterop.EngineRenderFrame(_engine.Context);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Engine Render] Error: {ex.Message}");

                // Fall back to test pattern on error
                if (_gl != null)
                {
                    RenderTestPattern(_gl);
                }
            }
        }

        private void RenderTestPattern(GlInterface gl)
        {
            // Simple test pattern - just animate the clear color to show the viewport is working
            float time = (float)(DateTime.Now - _lastFrameTime).TotalSeconds;
            float r = 0.2f + 0.1f * MathF.Sin(time * 2.0f);
            float g = 0.3f + 0.1f * MathF.Sin(time * 3.0f);
            float b = 0.4f + 0.1f * MathF.Sin(time * 4.0f);

            gl.ClearColor(r, g, b, 1.0f);
            gl.Clear(GL_COLOR_BUFFER_BIT);
        }

        private void UpdatePerformanceStats()
        {
            _frameCount++;
            var now = DateTime.Now;
            var deltaTime = (now - _lastFrameTime).TotalSeconds;
            if (deltaTime >= 1.0)
            {
                _frameCount = 0;
                _lastFrameTime = now;
            }
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            var width = (int)e.NewSize.Width;
            var height = (int)e.NewSize.Height;
            if (width > 0 && height > 0)
            {
                _viewportWidth = width;
                _viewportHeight = height;
                Console.WriteLine($"[OpenGLEditorViewport] Viewport resized to {width}x{height}");
                RequestNextFrameRendering();
            }
        }

        private void OnUpdateTick(object? sender, EventArgs e)
        {
            if (_isInitialized && _engine.IsInitialized)
            {
                try
                {
                    _engine.Update();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Engine Update] {ex}");
                }
                RequestNextFrameRendering();
            }
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();
            _isInitialized = _contextReady = _engineContextBound = false;
            Console.WriteLine("[OpenGLEditorViewport] Unloaded");
        }
    }
}