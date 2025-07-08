using System;

namespace Game.Dto
{
    /// <summary>
    /// Represents a tile's visual and physical properties for rendering and gameplay.
    /// </summary>
    /// 
    [Serializable]
    public class TileDefinition
    {
        public string AtlasName { get; set; }
        public string FrameName { get; set; }
        public bool Walkable { get; set; }
        public int CollisionType { get; set; }
    }
}
