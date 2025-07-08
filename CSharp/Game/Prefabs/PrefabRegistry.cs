using System;
using System.Collections.Generic;
using System.IO;
using WanderSpire.Components;
using WanderSpire.Scripting;
using WanderSpire.Scripting.Utils;

namespace Game.Prefabs
{
    public static class PrefabRegistry
    {
        // our factory map
        private static readonly Dictionary<string, IPrefabFactory> _factories =
            new(StringComparer.OrdinalIgnoreCase);

        // guard so we only scan once
        private static bool _initialized = false;

        /// <summary>
        /// Spawns the given prefab at tile (x,y), using whichever factory was registered.
        /// If this is the first call, we auto-register all JSON files in Assets/prefabs.
        /// Ensures the TransformComponent is initialized so the sprite is visible immediately.
        /// </summary>
        public static Entity SpawnAtTile(string key, int x, int y)
        {
            EnsureInitialized();

            Entity entity;
            if (_factories.TryGetValue(key, out var factory))
                entity = factory.SpawnAtTile(x, y);
            else
                entity = WanderSpire.Scripting.PrefabManager.SpawnAtTile(key, x, y);

            // Initialize TransformComponent so the sprite appears at the spawn location
            float tileSize = Engine.Instance!.TileSize;
            float worldX = x * tileSize + tileSize * 0.5f;
            float worldY = y * tileSize + tileSize * 0.5f;
            ComponentWriter.Patch(
                (uint)entity.Id,
                nameof(TransformComponent),
                new TransformComponent
                {
                    LocalPosition = new[] { worldX, worldY },
                    LocalRotation = 0f,
                    LocalScale = new[] { 1f, 1f }
                }
            );

            return entity;
        }

        /// <summary>
        /// Registers a custom factory. You can still call this at startup if you have code-only prefabs.
        /// </summary>
        public static void Register(IPrefabFactory factory)
            => _factories[factory.Key] = factory;

        //------------------------------------------------------------------------
        //  Internals
        //------------------------------------------------------------------------
        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            var folder = Path.Combine(ContentPaths.Root, "prefabs");
            if (!Directory.Exists(folder)) return;

            foreach (var jsonPath in Directory.EnumerateFiles(folder, "*.json"))
            {
                var key = Path.GetFileNameWithoutExtension(jsonPath);
                // if someone already registered this key manually, skip
                if (_factories.ContainsKey(key)) continue;

                _factories[key] = new JsonPrefabFactory(key, jsonPath);
            }
        }
    }
}
