using System;
using System.Collections.Generic;

namespace WanderSpire.Components
{
    [Serializable]
    public class AnimationClipsComponent
    {
        public Dictionary<string, Clip> Clips { get; set; }

        [Serializable]
        public class Clip
        {
            public int StartFrame { get; set; }
            public int FrameCount { get; set; }
            public float FrameDuration { get; set; }
            public bool Loop { get; set; }
        }
    }
}
