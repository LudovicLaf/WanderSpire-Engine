using System;

namespace WanderSpire.Components
{
    [Serializable]
    public class SpriteAnimationComponent
    {
        public int CurrentFrame { get; set; }
        public float ElapsedTime { get; set; }
        public bool Finished { get; set; }
        public int StartFrame { get; set; }
        public int FrameCount { get; set; }
        public float FrameDuration { get; set; }
        public bool Loop { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public int Columns { get; set; }
        public int Rows { get; set; }
        public float WorldWidth { get; set; }
        public float WorldHeight { get; set; }
    }
}
