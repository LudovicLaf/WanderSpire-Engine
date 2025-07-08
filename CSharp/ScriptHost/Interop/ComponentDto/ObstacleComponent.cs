using System;

namespace WanderSpire.Components
{
    [Serializable]
    public class ObstacleComponent
    {
        public bool BlocksMovement { get; set; }
        public bool BlocksVision { get; set; }
        public int ZOrder { get; set; }
    }
}
