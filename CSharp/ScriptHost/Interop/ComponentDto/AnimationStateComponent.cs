using System;

namespace WanderSpire.Components
{
    /// <summary>
    /// Generic, string-based state. 
    /// No gameplay logic here—just a free-form key ("Idle", "Walk", "Attack", etc).
    /// </summary>
    [Serializable]
    public class AnimationStateComponent
    {
        public string state { get; set; }
    }
}
