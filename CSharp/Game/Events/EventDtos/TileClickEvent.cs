namespace Game.Events
{
    /// <summary>
    /// Raised when the player clicks on a tile (x, y) in world‐space.
    /// </summary>
    public struct TileClickEvent
    {
        public int X;
        public int Y;

        public TileClickEvent(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
