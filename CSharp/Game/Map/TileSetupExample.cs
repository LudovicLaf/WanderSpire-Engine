using ScriptHost;
using System;
using WanderSpire.Components;
using WanderSpire.Scripting;
using static WanderSpire.Scripting.TilemapInterop;

namespace Game.Map
{
    /// <summary>
    /// Example showing how to set up tile definitions and palettes.
    /// </summary>
    public static class TileSetupExample
    {
        public static void InitializeTileDefinitions()
        {
            Console.WriteLine("[TileSetup] Initializing tile definitions...");

            // Clear any existing definitions
            TileDefinitionManager.Clear();

            // Register tiles individually (for fine control)
            RegisterIndividualTiles();

            // Optionally, load from palette (data-driven approach)
            SetupTilePalette();

            Console.WriteLine($"[TileSetup] Registered {TileDefinitionManager.GetCount()} tile definitions");
        }

        private static void RegisterIndividualTiles()
        {
            TileDefinitionManager.RegisterTile(1, "terrain", "grass", walkable: true, collisionType: 0);
            TileDefinitionManager.RegisterTile(2, "terrain", "sand", walkable: true, collisionType: 0);
            TileDefinitionManager.RegisterTile(3, "terrain", "road", walkable: true, collisionType: 0);
            TileDefinitionManager.RegisterTile(4, "terrain", "blank", walkable: true, collisionType: 0);

            TileDefinitionManager.RegisterTile(10, "environment", "rock", walkable: false, collisionType: 1);

            TileDefinitionManager.RegisterTile(20, "buildings", "wall", walkable: false, collisionType: 1);
            TileDefinitionManager.RegisterTile(21, "buildings", "door", walkable: true, collisionType: 0);
            TileDefinitionManager.RegisterTile(22, "buildings", "floor", walkable: true, collisionType: 0);

            TileDefinitionManager.SetDefault("terrain", "grass");
        }

        private static void SetupTilePalette()
        {
            var engine = Engine.Instance;
            if (engine == null) throw new InvalidOperationException("Engine not initialized");

            int paletteId = TilePalette_Create(engine.Context, "TerrainBasic", "terrain", 64, 64);

            if (paletteId > 0)
            {
                TilePalette_AddTile(engine.Context, paletteId, 1, "grass", "grass.png", 0, 0, 1, 0);
                TilePalette_AddTile(engine.Context, paletteId, 2, "sand", "sand.png", 64, 0, 1, 0);
                TilePalette_AddTile(engine.Context, paletteId, 3, "road", "road.png", 0, 64, 1, 0);
                TilePalette_AddTile(engine.Context, paletteId, 4, "blank", "blank.png", 64, 64, 1, 0);

                Console.WriteLine($"[TileSetup] Created terrain palette with ID {paletteId}");

                // Load tile definitions from the palette
                TileDefinitionManager.LoadFromPalette(paletteId);

                // Optionally, assign the palette to all tilemap layers in the world
                SetupTilemapWithPalette(paletteId);
            }
        }

        private static void SetupTilemapWithPalette(int paletteId)
        {
            // This assumes a World utility for iterating entities, adjust as needed.
            World.ForEachEntity(entity =>
            {
                var layerComponent = entity.GetComponent<TilemapLayerComponent>("TilemapLayerComponent");
                if (layerComponent != null)
                {
                    TileLayerHelper.SetPalette(entity, paletteId);
                    Console.WriteLine($"[TileSetup] Assigned palette {paletteId} to layer '{layerComponent.LayerName}'");
                }
            });
        }

        public static void ExampleTileQuery()
        {
            int tileId = 1;
            var tileDef = TileDefinitionManager.GetTileDefinition(tileId);

            if (tileDef != null)
            {
                Console.WriteLine($"Tile {tileId}: Atlas='{tileDef.AtlasName}', Frame='{tileDef.FrameName}', Walkable={tileDef.Walkable}");
            }
            else
            {
                Console.WriteLine($"Tile {tileId}: No definition found");
            }
        }

        public static void ExampleRuntimeUpdate()
        {
            // Register/update a tile at runtime
            TileDefinitionManager.RegisterTile(99, "special", "magical_grass", walkable: true);

            // Refresh definitions for all layers using a specific palette
            World.ForEachEntity(entity =>
            {
                var layerComponent = entity.GetComponent<TilemapLayerComponent>("TilemapLayerComponent");
                if (layerComponent != null)
                {
                    TileLayerHelper.RefreshDefinitions(entity);
                }
            });
        }
    }
}