using Game.Dto;
using System;
using WanderSpire.Scripting;
using static WanderSpire.Scripting.TilemapInterop;


namespace Game.Map
{
    /// <summary>
    /// Manages tile ID to texture/atlas frame mappings for runtime rendering.
    /// Replaces hardcoded texture values in the render system.
    /// </summary>
    public static class TileDefinitionManager
    {
        /// <summary>
        /// Register a tile definition for rendering. This replaces hardcoded mappings.
        /// </summary>
        public static void RegisterTile(int tileId, string atlasName, string frameName,
                                        bool walkable = true, int collisionType = 0)
        {
            var engine = Engine.Instance;
            if (engine == null) throw new InvalidOperationException("Engine not initialized");

            TileDef_RegisterTile(engine.Context, tileId, atlasName, frameName,
                                       walkable ? 1 : 0, collisionType);
        }

        /// <summary>
        /// Get tile definition information for a tile ID
        /// </summary>
        public static TileDefinition? GetTileDefinition(int tileId)
        {
            var engine = Engine.Instance;
            if (engine == null) return null;

            const int bufferSize = 256;
            var atlasNameBuffer = new byte[bufferSize];
            var frameNameBuffer = new byte[bufferSize];

            int result = TileDef_GetTileInfo(engine.Context, tileId,
                atlasNameBuffer, bufferSize,
                frameNameBuffer, bufferSize,
                out int walkable, out int collisionType);

            if (result != 0) return null;

            string atlasName = System.Text.Encoding.UTF8.GetString(atlasNameBuffer).TrimEnd('\0');
            string frameName = System.Text.Encoding.UTF8.GetString(frameNameBuffer).TrimEnd('\0');

            return new TileDefinition
            {
                AtlasName = atlasName,
                FrameName = frameName,
                Walkable = walkable != 0,
                CollisionType = collisionType
            };
        }

        /// <summary>
        /// Load tile definitions from a registered palette
        /// </summary>
        public static void LoadFromPalette(int paletteId)
        {
            var engine = Engine.Instance;
            if (engine == null) return;

            TileDef_LoadFromPalette(engine.Context, paletteId);
        }

        /// <summary>
        /// Set default tile definition for unknown tile IDs
        /// </summary>
        public static void SetDefault(string atlasName, string frameName)
        {
            var engine = Engine.Instance;
            if (engine == null) return;

            TileDef_SetDefault(engine.Context, atlasName, frameName);
        }

        /// <summary>
        /// Clear all registered tile definitions
        /// </summary>
        public static void Clear()
        {
            var engine = Engine.Instance;
            if (engine == null) return;

            TileDef_Clear(engine.Context);
        }

        /// <summary>
        /// Get number of registered tile definitions
        /// </summary>
        public static int GetCount()
        {
            var engine = Engine.Instance;
            if (engine == null) return 0;

            return TileDef_GetCount(engine.Context);
        }

        /// <summary>
        /// Register common terrain tiles. Call this during game initialization.
        /// </summary>
        public static void RegisterCommonTiles()
        {
            RegisterTile(1, "terrain", "grass", walkable: true, collisionType: 0);
            RegisterTile(2, "terrain", "sand", walkable: true, collisionType: 0);
            RegisterTile(3, "terrain", "road", walkable: true, collisionType: 0);
            RegisterTile(4, "terrain", "blank", walkable: true, collisionType: 0);

            SetDefault("terrain", "grass");
        }
    }
}
