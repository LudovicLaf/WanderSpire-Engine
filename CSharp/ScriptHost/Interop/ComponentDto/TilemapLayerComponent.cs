using System;

namespace WanderSpire.Components
{
    /// <summary>
    /// Managed DTO mirroring WanderSpire.TilemapLayerComponent.
    /// </summary>
    [Serializable]
    public class TilemapLayerComponent
    {
        // Layer index/order in the map
        public int LayerIndex { get; set; } = 0;

        // Name for the layer (used in UI)
        public string LayerName { get; set; } = "Layer_0";

        // Layer visual properties
        public float Opacity { get; set; } = 1.0f;
        public bool Visible { get; set; } = true;
        public bool Locked { get; set; } = false;

        // Collision/physics
        public bool HasCollision { get; set; } = false;
        public int PhysicsLayer { get; set; } = 0;

        // Rendering order/material
        public int SortingOrder { get; set; } = 0;
        public string MaterialName { get; set; } = string.Empty;

        // Tile palette integration
        public int PaletteId { get; set; } = 0;
        public bool AutoRefreshDefinitions { get; set; } = true;
    }
}
