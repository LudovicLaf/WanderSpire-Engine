using Game.Camera;
using Game.Map;
using Game.Prefabs;
using Game.Systems;
using System;
using System.IO;
using System.Runtime.InteropServices;
using WanderSpire.Scripting;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.EngineInterop;
using static WanderSpire.Scripting.TilemapInterop;

internal static class Program
{
    private const int SDL_APP_CONTINUE = 0;
    private static uint _playerId;
    private static DebugUISystem? _debugUISystem;

    private static void Main(string[] args)
    {
        Console.Title = "WanderSpire – Player";
        Console.WriteLine("==== WanderSpire Player ====");

        // ── native context ──────────────────────────────────────────────
        IntPtr ctx = CreateEngineContext();
        if (EngineInit(ctx, args.Length, args) != SDL_APP_CONTINUE)
        {
            Console.Error.WriteLine("[Fatal] EngineInit failed.");
            DestroyEngineContext(ctx);
            return;
        }

        // ── bring window to front ───────────────────────────────────────
        var win = Engine_GetWindow(ctx);
        SDL_ShowWindow(win);
        SDL_RaiseWindow(win);

        // ── content paths & script engine ───────────────────────────────
        ContentPaths.Initialize(Path.Combine(AppContext.BaseDirectory, "Assets"));



        WanderSpire.Scripting.Engine.Initialize(ctx);
        var scriptEngine = new ScriptEngine(
            Path.Combine(ContentPaths.Root, "ScriptsComponent"), ctx)
        {
            BehaviourFactory = Game.BehaviourFactory.Resolve
        };

        // ── event bus / input bootstrap ─────────────────────────────────
        EventBus.Initialize(ctx);
        Input.BeginFrame();

        // Initialize tile definitions BEFORE generating terrain !!
        InitializeTileSystem();

        GenerateTerrain(ctx);

        // ── spawn initial player prefab ─────────────────────────────────
        var player = PrefabRegistry.SpawnAtTile("player", 0, 0);
        _playerId = (uint)player.Id;
        Engine_SetPlayerEntity(ctx, new EntityId { id = _playerId });

        // ── teleport camera to the player's starting world-position ──────
        Engine_GetEntityWorldPosition(
            ctx,
            new EntityId { id = _playerId },
            out float startWX,
            out float startWY);
        CameraController.MoveTo(startWX, startWY);

        // ── then let the native engine smoothly follow the player's TransformComponent ─
        CameraController.Follow(player);

        // ── load default scene if present ───────────────────────────────
        string sceneFile = Path.Combine(ContentPaths.Root, "..", "scenes", "default.json");
        Console.WriteLine(File.Exists(sceneFile)
            ? $"Scene: {sceneFile} found"
            : $"Scene not found at {sceneFile}");

        // ── notify quests they've started ───────────────────────────────
        foreach (var q in scriptEngine.Quests)
            q.OnStart();

        // ── main loop ───────────────────────────────────────────────────
        IntPtr evBuf = Marshal.AllocHGlobal(1024);
        bool running = true;

        bool orcHeld = false;
        bool guardHeld = false;
        bool bushHeld = false;

        Console.WriteLine("=== Controls ===");
        Console.WriteLine("F5: Save Scene");
        Console.WriteLine("F6: Debug World Dump");
        Console.WriteLine("F9: Hot Reload Scripts & Scene");
        Console.WriteLine("F12: Toggle Debug UI");
        Console.WriteLine("I/O/B: Spawn Orc/Guard/Bush at mouse cursor");
        Console.WriteLine("R: Toggle Run Mode");

        while (running)
        {
            // SDL event pump - ALWAYS process all events normally
            while (SDL_PollEvent(evBuf))
            {
                // Let ImGui process events for its internal state
                if (ImGuiManager.Instance != null)
                    ImGuiManager.Instance.ProcessEvent(evBuf);

                // MINIMAL FIX: Don't process ANY events when ImGui wants mouse (includes wheel)
                if (ImGuiManager.Instance?.WantCaptureMouse == true)
                {
                    // Skip event processing when ImGui is using the mouse
                    continue;
                }

                // Process engine events normally
                if (EngineEvent(ctx, evBuf) != SDL_APP_CONTINUE)
                {
                    running = false;
                    break;
                }
            }
            if (!running) break;

            // Input snapshot
            Input.BeginFrame();

            // Simple check: does ImGui want to capture input RIGHT NOW?
            bool imguiWantsInput = _debugUISystem?.WantsInput() == true;

            if (!imguiWantsInput)
            {
                // ── hot-reload all managed scripts + scene (F9) ─────────────────
                if (Input.GetKeyDown(KeyCode.F9))
                {
                    try
                    {
                        scriptEngine.Dispose();

                        // !! NEW: Reinitialize tile system on hot reload !!
                        InitializeTileSystem();

                        scriptEngine = new ScriptEngine(
                            Path.Combine(ContentPaths.Root, "ScriptsComponent"), ctx)
                        {
                            BehaviourFactory = Game.BehaviourFactory.Resolve
                        };

                        foreach (var q in scriptEngine.Quests)
                            q.OnStart();

                        Console.WriteLine("[HotReload] F9 pressed – reloading scene");
                        if (File.Exists(sceneFile))
                        {
                            Game.SceneManager.Load(sceneFile);
                            Console.WriteLine("[Debug] Forcing tilemap render rebuild...");
                            Console.WriteLine("[Debug] Tilemap should now be visible");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[HotReload] {ex}");
                    }
                }

                // ── save scene (F5) ───────────────────────────────────────────
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    try
                    {
                        SceneManager_SaveScene(ctx, sceneFile);
                        Console.WriteLine($"[Scene] Saved to {sceneFile}");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Scene] Save failed: {ex.Message}");
                    }
                }

                // ── mouse-cursor → grid-tile (uses camera & zoom aware native helper)
                Engine_GetMouseTile(ctx, out int tileX, out int tileY);

                // ── place prefabs with I / O / B keys ─────────────────────────-
                if (Input.GetKey(KeyCode.I))
                {
                    if (!orcHeld)
                    {
                        PrefabRegistry.SpawnAtTile("orc", tileX, tileY);
                        Console.WriteLine($"[Spawn] orc @ ({tileX},{tileY})");
                        orcHeld = true;
                    }
                }
                else orcHeld = false;

                if (Input.GetKey(KeyCode.O))
                {
                    if (!guardHeld)
                    {
                        PrefabRegistry.SpawnAtTile("guard", tileX, tileY);
                        Console.WriteLine($"[Spawn] guard @ ({tileX},{tileY})");
                        guardHeld = true;
                    }
                }
                else guardHeld = false;

                if (Input.GetKey(KeyCode.B))
                {
                    if (!bushHeld)
                    {
                        PrefabRegistry.SpawnAtTile("bush", tileX, tileY);
                        Console.WriteLine($"[Spawn] bush @ ({tileX},{tileY})");
                        bushHeld = true;
                    }
                }
                else bushHeld = false;
            }
            else
            {
                // Reset held states when ImGui is capturing input
                orcHeld = false;
                guardHeld = false;
                bushHeld = false;
            }

            // ── tick the native engine ────────────────────────────────────
            if (EngineIterate(ctx) != SDL_APP_CONTINUE)
                break;
        }

        // ── shutdown ────────────────────────────────────────────────────
        Marshal.FreeHGlobal(evBuf);

        // Dispose debug UI system first
        _debugUISystem?.Dispose();
        _debugUISystem = null;

        scriptEngine.Dispose();
        EngineQuit(ctx);
        DestroyEngineContext(ctx);
    }

    /// <summary>
    /// NEW: Initialize the tile definition system to replace hardcoded texture mappings
    /// </summary>
    private static void InitializeTileSystem()
    {
        try
        {
            Console.WriteLine("[TileSystem] Initializing tile definitions...");

            // Use the example setup or create your own
            TileSetupExample.InitializeTileDefinitions();

            // Alternative: Register tiles manually for specific control
            // TileDefinitionManager.RegisterTile(1, "terrain", "grass", walkable: true);
            // TileDefinitionManager.RegisterTile(2, "terrain", "sand", walkable: true);
            // TileDefinitionManager.RegisterTile(3, "terrain", "road", walkable: true);
            // TileDefinitionManager.SetDefault("terrain", "grass");

            Console.WriteLine($"[TileSystem] Registered {TileDefinitionManager.GetCount()} tile definitions");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[TileSystem] Failed to initialize: {ex.Message}");
            // Fall back to registering basic tiles
            TileDefinitionManager.RegisterTile(1, "terrain", "grass");
            TileDefinitionManager.RegisterTile(2, "terrain", "sand");
            TileDefinitionManager.SetDefault("terrain", "grass");
        }
    }

    // put this anywhere inside Program (below Main is fine)
    private static void GenerateTerrain(IntPtr ctx)
    {
        const int mapW = 100, mapH = 100;
        const int grassTileId = 1;
        const int stoneTileId = 2;

        // 1) create tilemap + a "Ground" layer
        // 1) create tilemap + a "Ground" layer
        var tilemapEntity = TilemapInterop.Tilemap_Create(ctx, "MainTilemap");
        var groundLayerEntity = TilemapInterop.Tilemap_CreateLayer(ctx, tilemapEntity, "Ground");

        // !! NEW: Set up the layer to use a tile palette (optional) !!
        // This connects the layer to the tile definition system
        // TileLayer_SetPalette(ctx, groundLayerEntity, 1); // Use palette ID 1

        // 2) blanket-fill grass
        for (int y = 0; y < mapH; ++y)
            for (int x = 0; x < mapW; ++x)
                Tilemap_SetTile(ctx, groundLayerEntity, x, y, grassTileId);

        // 3) sprinkle ten random stone patches
        var rng = new Random();
        for (int i = 0; i < 10; ++i)
        {
            int cx = rng.Next(mapW);
            int cy = rng.Next(mapH);
            int radius = 2 + rng.Next(3);                // 2–4

            for (int dy = -radius; dy <= radius; ++dy)
                for (int dx = -radius; dx <= radius; ++dx)
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        int tx = cx + dx, ty = cy + dy;
                        if (tx >= 0 && tx < mapW && ty >= 0 && ty < mapH)
                            Tilemap_SetTile(ctx, groundLayerEntity, tx, ty, stoneTileId);
                    }
        }

        Console.WriteLine($"[Program] Generated {mapW}×{mapH} terrain in managed layer");
        Console.WriteLine("[Program] Tile definitions will be used for rendering (no more hardcoded textures!)");
    }
}