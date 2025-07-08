// ScriptHost/ContentPaths.cs
using System;

namespace WanderSpire.Scripting
{
    /// <summary>
    ///  “Assets” root (csx, JSON, textures, prefabs…).
    /// Call Initialize() once in your Player before using any Content loader.
    /// </summary>
    public static class ContentPaths
    {
        /// <summary>Absolute path to the root Assets directory next to your exe.</summary>
        public static string Root { get; private set; } = string.Empty;

        public static void Initialize(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
                throw new ArgumentException("Assets root must be a valid, non-empty path.", nameof(root));
            Root = root;
        }
    }
}
