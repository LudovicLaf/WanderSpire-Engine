using System;

namespace WanderSpire.Components
{
    [Serializable]
    public class GridPositionComponent
    {
        public int[] Tile { get; set; } // [x, y] if prefab or JSON
        public Vec2 TileObj { get; set; } // { "x": ..., "y": ... } if C++ engine

        [Serializable]
        public class Vec2
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        // Unified getter for use everywhere in AI code
        public (int X, int Y) AsTuple()
        {
            if (Tile != null && Tile.Length == 2)
                return (Tile[0], Tile[1]);
            if (TileObj != null)
                return (TileObj.X, TileObj.Y);
            // fallback for old engine or broken
            return (0, 0);
        }
    }
}
