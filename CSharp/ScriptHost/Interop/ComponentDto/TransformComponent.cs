using System;

namespace WanderSpire.Components
{
    /// <summary>
    /// Managed mirror of the native <c>TransformComponent</c>.
    /// </summary>
    [Serializable]
    public class TransformComponent
    {
        // Local transform (relative to parent)
        public float[] LocalPosition { get; set; } = new float[2]; // [x, y]
        public float LocalRotation { get; set; }                   // radians
        public float[] LocalScale { get; set; } = new float[] { 1f, 1f };

        // World transform cache
        public float[] WorldPosition { get; set; } = new float[2]; // [x, y]
        public float WorldRotation { get; set; }
        public float[] WorldScale { get; set; } = new float[] { 1f, 1f };

        // Transform state
        public bool IsDirty { get; set; } = true;
        public bool FreezeTransform { get; set; } = false;

        // Pivot support (normalized, e.g., [0.5, 0.5] for center)
        public float[] Pivot { get; set; } = new float[] { 0.5f, 0.5f };

        // Transform constraints
        public bool LockX { get; set; } = false;
        public bool LockY { get; set; } = false;
        public bool LockRotation { get; set; } = false;
        public bool LockScaleX { get; set; } = false;
        public bool LockScaleY { get; set; } = false;
    }
}
