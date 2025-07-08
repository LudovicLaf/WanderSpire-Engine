using ReactiveUI;

namespace SceneEditor.Models
{
    /// <summary>
    /// Represents a tile definition in a palette
    /// </summary>
    public class TileDefinition : ReactiveObject
    {
        public int TileId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AtlasX { get; set; }
        public int AtlasY { get; set; }
        public bool IsWalkable { get; set; } = true;
        public int CollisionType { get; set; } = 0;
        public string AtlasName { get; set; } = "default";
        public string FrameName { get; set; } = string.Empty;
    }
}
