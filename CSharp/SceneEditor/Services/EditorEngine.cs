// SceneEditor/Services/EditorEngine.cs - Fixed for proper context management and rendering
using System;
using WanderSpire.Scripting;

namespace SceneEditor.Services;

/// <summary>
/// Enhanced engine integration service with robust initialization and rendering support
/// </summary>
public class EditorEngine : IDisposable
{
    private IntPtr _context;
    private bool _initialized;
    private bool _disposed;
    private bool _renderingInitialized = false;
    private bool _openGLContextReady = false;

    // Engine state
    private float _cameraX = 0f;
    private float _cameraY = 0f;
    private float _cameraZoom = 1f;
    private float _tileSize = 64f;

    public IntPtr Context => _context;
    public bool IsInitialized => _initialized;
    public bool IsRenderingInitialized => _renderingInitialized;
    public bool IsOpenGLContextReady => _openGLContextReady;
    public bool CanRenderSafely => _initialized && _renderingInitialized && _openGLContextReady && ValidateContext();

    // Camera properties
    public float CameraX => _cameraX;
    public float CameraY => _cameraY;
    public float CameraZoom => _cameraZoom;
    public float TileSize => _tileSize;

    public event EventHandler? EngineInitialized;
    public event EventHandler? EngineShutdown;

    public EditorEngine()
    {
    }

    /// <summary>
    /// Initialize the engine with proper editor configuration
    /// </summary>
    public bool Initialize()
    {
        if (_initialized)
            return true;

        try
        {
            Console.WriteLine("[EditorEngine] Starting engine initialization...");

            // Create engine context
            _context = EngineInterop.CreateEngineContext();
            if (_context == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create engine context");
            }

            Console.WriteLine("[EditorEngine] Engine context created successfully");

            // Use editor-specific initialization
            bool success = TryInitializeEditorMode();

            if (success)
            {
                _initialized = true;
                EngineInitialized?.Invoke(this, EventArgs.Empty);
                Console.WriteLine("[EditorEngine] Engine initialization completed successfully");
                return true;
            }
            else
            {
                throw new InvalidOperationException("Editor initialization failed");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorEngine] Initialization failed: {ex}");
            Cleanup();
            return false;
        }
    }

    /// <summary>
    /// Called by OpenGL viewport when context is ready - this is where rendering init happens
    /// </summary>
    public void OnOpenGLContextReady()
    {
        if (!_initialized)
        {
            Console.Error.WriteLine("[EditorEngine] OnOpenGLContextReady called but engine not initialized");
            return;
        }

        if (_openGLContextReady)
        {
            Console.WriteLine("[EditorEngine] OpenGL context already ready");
            return;
        }

        try
        {
            Console.WriteLine("[EditorEngine] OpenGL context is ready, setting up rendering...");
            _openGLContextReady = true;

            // Initialize scripting subsystems now that we have OpenGL context
            InitializeScriptingSubsystems();

            // Set up initial configuration
            SetupEditorConfiguration();

            // Create test scene
            CreateTestScene();

            Console.WriteLine("[EditorEngine] OpenGL context setup completed");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorEngine] OpenGL context setup failed: {ex}");
            _openGLContextReady = false;
        }
    }

    private bool TryInitializeEditorMode()
    {
        try
        {
            // Use the editor-specific initialization that doesn't create SDL window
            string[] editorArgs = {
                "--editor",
                "--no-audio",
                "--headless"
            };

            Console.WriteLine("[EditorEngine] Attempting editor-mode initialization: " + string.Join(" ", editorArgs));
            int result = EngineInterop.EngineInitEditor(_context, editorArgs.Length, editorArgs);

            if (result == 0)
            {
                Console.WriteLine("[EditorEngine] Editor-mode initialization successful");
                return true;
            }
            else
            {
                Console.WriteLine($"[EditorEngine] Editor-mode initialization failed with code: {result}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorEngine] Editor-mode initialization exception: {ex}");
            return false;
        }
    }

    private bool ValidateContext()
    {
        try
        {
            if (_context == IntPtr.Zero)
            {
                return false;
            }

            // Simple validation - just check if the context responds to basic queries
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorEngine] Context validation failed: {ex}");
            return false;
        }
    }

    private void InitializeScriptingSubsystems()
    {
        try
        {
            WanderSpire.Scripting.Engine.Initialize(_context);
            EventBus.Initialize(_context);
            Console.WriteLine("[EditorEngine] Scripting subsystems initialized");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorEngine] Scripting initialization warning: {ex.Message}");
        }
    }

    private void SetupEditorConfiguration()
    {
        try
        {
            // Get initial tile size
            _tileSize = SafeEngineCall(() => EngineInterop.Engine_GetTileSize(_context), 64f);

            Console.WriteLine($"[EditorEngine] Configuration: TileSize={_tileSize}, Rendering={_renderingInitialized}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorEngine] Configuration setup error: {ex}");
        }
    }

    private void CreateTestScene()
    {
        try
        {
            Console.WriteLine("[EditorEngine] Creating test scene...");

            // Create test entities using prefab system
            try
            {
                // Try to create a player entity using the prefab system
                var playerEntityId = EngineInterop.Prefab_InstantiateAtTile(_context, "player", 0, 0);
                if (playerEntityId.IsValid)
                {
                    Console.WriteLine($"[EditorEngine] Created test player entity with ID: {playerEntityId.id}");
                }
                else
                {
                    Console.WriteLine("[EditorEngine] Failed to create player prefab, creating basic entity instead");

                    // Fallback: create a basic entity
                    var basicEntityId = CreateEntity();
                    if (basicEntityId.IsValid)
                    {
                        var entityId = new EntityId { id = (uint)basicEntityId.Id };
                        string gridPosJson = """{"tile": [0, 0]}""";
                        int result = EngineInterop.SetComponentJson(_context, entityId, "GridPositionComponent", gridPosJson);

                        if (result == 0)
                        {
                            Console.WriteLine($"[EditorEngine] Created basic test entity with GridPosition at (0, 0)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EditorEngine] Failed to create player entity: {ex.Message}");
            }

            // Create a test tilemap
            try
            {
                var tilemap = TilemapInterop.Tilemap_Create(_context, "TestTilemap");
                if (tilemap.id != EngineInterop.WS_INVALID_ENTITY)
                {
                    var layer = TilemapInterop.Tilemap_CreateLayer(_context, tilemap, "TestLayer");
                    if (layer.id != EngineInterop.WS_INVALID_ENTITY)
                    {
                        // Place some test tiles
                        int tilesPlaced = 0;
                        for (int x = -5; x <= 5; x++)
                        {
                            for (int y = -5; y <= 5; y++)
                            {
                                int tileId = (Math.Abs(x) + Math.Abs(y)) % 3 + 1;
                                TilemapInterop.Tilemap_SetTile(_context, layer, x, y, tileId);
                                tilesPlaced++;
                            }
                        }
                        Console.WriteLine($"[EditorEngine] Created test tilemap with {tilesPlaced} tiles");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EditorEngine] Failed to create tilemap: {ex.Message}");
            }

            Console.WriteLine("[EditorEngine] Test scene creation completed");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorEngine] Failed to create test scene: {ex}");
        }
    }

    /// <summary>
    /// Safe wrapper for engine calls that returns a default value on failure
    /// </summary>
    private T SafeEngineCall<T>(Func<T> action, T defaultValue)
    {
        try
        {
            if (!_initialized || _context == IntPtr.Zero)
            {
                return defaultValue;
            }

            return action();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorEngine] Safe engine call failed: {ex.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    /// Safe wrapper for engine calls that might fail
    /// </summary>
    private void SafeEngineCall(Action action)
    {
        try
        {
            if (!_initialized || _context == IntPtr.Zero || !ValidateContext())
            {
                return;
            }

            action();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorEngine] Safe engine call failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Update engine systems - safe version
    /// </summary>
    public void Update()
    {
        if (!CanRenderSafely)
            return;

        SafeEngineCall(() =>
        {
            int result = EngineInterop.EngineIterateEditor(_context);
            if (result != 0)
            {
                Console.Error.WriteLine($"[EditorEngine] EngineIterateEditor returned error code: {result}");
            }
        });
    }

    /// <summary>
    /// Render a frame safely
    /// </summary>
    public void RenderFrame()
    {
        if (!CanRenderSafely)
            return;

        SafeEngineCall(() =>
        {
            // Update editor camera before rendering
            EngineInterop.EngineSetEditorCamera(_context, _cameraX, _cameraY, _cameraZoom, 800, 600);

            // Render the frame
            EngineInterop.EngineRenderFrame(_context);
        });
    }

    /// <summary>
    /// Set viewport size for rendering
    /// </summary>
    public void SetViewportSize(int width, int height)
    {
        if (width > 0 && height > 0)
        {
            SafeEngineCall(() => EngineInterop.Engine_SetViewportSize(_context, width, height));
        }
    }

    /// <summary>
    /// Set camera position
    /// </summary>
    public void SetCameraPosition(float x, float y)
    {
        _cameraX = x;
        _cameraY = y;
        SafeEngineCall(() => EngineInterop.Engine_SetCameraPosition(_context, x, y));
    }

    /// <summary>
    /// Set camera zoom level
    /// </summary>
    public void SetCameraZoom(float zoom)
    {
        _cameraZoom = Math.Max(0.1f, zoom);
        SafeEngineCall(() => EngineInterop.Engine_SetCameraZoom(_context, _cameraZoom));
    }

    /// <summary>
    /// Create a new entity
    /// </summary>
    public Entity CreateEntity()
    {
        if (!CanRenderSafely)
            throw new InvalidOperationException("Engine not ready for entity creation");

        try
        {
            var entityId = EngineInterop.CreateEntity(_context);
            if (!entityId.IsValid)
            {
                throw new InvalidOperationException("Failed to create valid entity");
            }

            var entity = Entity.FromRaw(_context, (int)entityId.id);
            return entity;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorEngine] CreateEntity error: {ex}");
            throw;
        }
    }


    /// <summary>
    /// Destroy an entity
    /// </summary>
    public void DestroyEntity(Entity entity)
    {
        if (!_initialized || !entity.IsValid || !ValidateContext())
            return;

        try
        {
            var entityId = new EntityId { id = (uint)entity.Id };
            Console.WriteLine($"[EditorEngine] Destroying entity {entity.Id}");
            EngineInterop.DestroyEntity(_context, entityId);
            Console.WriteLine($"[EditorEngine] Successfully destroyed entity {entity.Id}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorEngine] DestroyEntity error: {ex}");
        }
    }

    /// <summary>
    /// Get all entities in the scene
    /// </summary>
    public Entity[] GetAllEntities()
    {
        if (!CanRenderSafely)
            return Array.Empty<Entity>();

        return SafeEngineCall(() =>
        {
            const int maxEntities = 1000;
            var buffer = new uint[maxEntities];
            int count = EngineInterop.Engine_GetAllEntities(_context, buffer, maxEntities);

            if (count < 0 || count > maxEntities)
            {
                return Array.Empty<Entity>();
            }

            var entities = new Entity[count];
            for (int i = 0; i < count; i++)
            {
                entities[i] = Entity.FromRaw(_context, (int)buffer[i]);
            }

            return entities;
        }, Array.Empty<Entity>());
    }

    /// <summary>
    /// Get currently selected entities
    /// </summary>
    public Entity[] GetSelectedEntities()
    {
        if (!CanRenderSafely)
            return Array.Empty<Entity>();

        return SafeEngineCall(() =>
        {
            const int maxEntities = 100;
            var buffer = new uint[maxEntities];
            int count = EngineInterop.Selection_GetSelectedEntities(_context, buffer, maxEntities);

            if (count < 0 || count > maxEntities)
            {
                return Array.Empty<Entity>();
            }

            var entities = new Entity[count];
            for (int i = 0; i < count; i++)
            {
                entities[i] = Entity.FromRaw(_context, (int)buffer[i]);
            }

            return entities;
        }, Array.Empty<Entity>());
    }

    /// <summary>
    /// Enable/disable grid rendering
    /// </summary>
    public void SetGridVisible(bool visible)
    {
        SafeEngineCall(() => EngineInterop.Engine_SetGridVisible(_context, visible ? 1 : 0));
    }

    private void Cleanup()
    {
        if (_context != IntPtr.Zero)
        {
            try
            {
                Console.WriteLine("[EditorEngine] Cleaning up engine context...");
                EngineInterop.EngineQuit(_context);
                EngineInterop.DestroyEngineContext(_context);
                Console.WriteLine("[EditorEngine] Engine context cleaned up");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EditorEngine] Cleanup error: {ex}");
            }
            finally
            {
                _context = IntPtr.Zero;
                _initialized = false;
                _renderingInitialized = false;
                _openGLContextReady = false;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Console.WriteLine("[EditorEngine] Disposing...");
        EngineShutdown?.Invoke(this, EventArgs.Empty);
        Cleanup();
        _disposed = true;
    }
}