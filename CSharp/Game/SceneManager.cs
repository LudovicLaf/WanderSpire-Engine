using Game.Camera;
using ScriptHost;
using System;
using System.Collections.Generic;
using System.IO;
using WanderSpire.Components;
using WanderSpire.Scripting;
using WanderSpire.Scripting.Utils;
using static WanderSpire.Scripting.EngineInterop;

namespace Game
{
    /// <summary>
    /// Managed‐side façade around the native SceneManager.
    /// Handles full tear‐down / reload and makes sure all
    /// C# behaviours are re‐bound to the *new* EnTT registry
    /// so no dangling handles survive a hot‐reload.
    /// </summary>
    public static class SceneManager
    {
        private static bool _animationSafetyInitialized = false;

        public static void Load(string filepath)
        {
            if (!File.Exists(filepath))
                throw new FileNotFoundException($"Scene file not found: {filepath}");

            // A) Tear‐down previous managed behaviours
            ScriptEngine.Current?.PurgeBehaviours();

            // B) Native load – wipes and recreates entities
            var engine = Engine.Instance
                         ?? throw new InvalidOperationException("Engine not initialized");
            var ctx = engine.Context;

            EventBus.Initialize(ctx);

            uint playerId, mainTilemapId;
            float camX, camY;

            // Use new P/Invoke signature: returns bool for success
            bool success = SceneManager_LoadScene(
                ctx,
                filepath,
                out playerId,
                out camX,
                out camY,
                out mainTilemapId
            );
            if (!success)
                throw new Exception($"Failed to load scene from {filepath}");

            // C) ONE‐TIME legacy safety for AnimationStateComponent
            if (!_animationSafetyInitialized)
            {
                InitializeAnimationSafety(ctx);
                _animationSafetyInitialized = true;
            }

            // D) Attach managed behaviours
            ScriptEngine.Current?.BindEntityScripts();

            // E) Camera & player hookup (managed now handles camera)
            Engine_SetCameraPosition(ctx, camX, camY);

            var player = FindPlayer();
            if (player != null && player.IsValid)
            {
                Engine_SetPlayerEntity(ctx, new EntityId { id = (uint)player.Id });

                // Start following the player now that the scene has loaded
                CameraController.Follow(player);
            }

            // Optionally, use mainTilemapId if you want to cache, check, or operate on the tilemap entity.
        }

        private static void InitializeAnimationSafety(IntPtr ctx)
        {
            var missing = new List<int>();
            World.ForEachEntity(ent =>
            {
                if (!ent.HasComponent(nameof(AnimationStateComponent)))
                    missing.Add(ent.Id);
            });

            foreach (var id in missing)
            {
                ComponentWriter.Patch(
                    (uint)id,
                    nameof(AnimationStateComponent),
                    new AnimationStateComponent { state = "0" }
                );
            }
        }

        private static Entity? FindPlayer()
        {
            Entity? player = null;
            World.ForEachEntity(ent =>
            {
                if (player == null && ent.HasComponent(nameof(PlayerTagComponent)))
                    player = ent;
            });
            return player;
        }
    }
}
