using WanderSpire.Scripting;
using static WanderSpire.Scripting.TilemapInterop;

namespace Game.Map
{
    /// <summary>
    /// Helper class for working with tilemap layers and palettes
    /// </summary>
    public static class TileLayerHelper
    {
        /// <summary>
        /// Set which palette a tilemap layer uses for rendering
        /// </summary>
        public static void SetPalette(Entity tilemapLayer, int paletteId)
        {
            var engine = Engine.Instance;
            if (engine == null) return;

            var entityId = new EntityId { id = (uint)tilemapLayer.Id };
            TileLayer_SetPalette(engine.Context, entityId, paletteId);
        }

        /// <summary>
        /// Get the palette ID used by a tilemap layer
        /// </summary>
        public static int GetPalette(Entity tilemapLayer)
        {
            var engine = Engine.Instance;
            if (engine == null) return 0;

            var entityId = new EntityId { id = (uint)tilemapLayer.Id };
            return TileLayer_GetPalette(engine.Context, entityId);
        }

        /// <summary>
        /// Refresh tile definitions from the layer's palette
        /// </summary>
        public static void RefreshDefinitions(Entity tilemapLayer)
        {
            var engine = Engine.Instance;
            if (engine == null) return;

            var entityId = new EntityId { id = (uint)tilemapLayer.Id };
            TileLayer_RefreshDefinitions(engine.Context, entityId);
        }
    }
}