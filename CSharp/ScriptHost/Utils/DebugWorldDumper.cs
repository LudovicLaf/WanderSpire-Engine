// ScriptHost/DebugWorldDumper.cs

using System;
using WanderSpire.Scripting;

namespace DebugTools
{
    public static class DebugWorldDumper
    {
        public static void DumpAllEntitiesAndComponents(string header = null)
        {
            if (header != null) Console.WriteLine(header);
            ScriptHost.World.ForEachEntity(DumpEntity);
        }

        public static void DumpEntity(Entity ent)
        {
            Console.WriteLine($"[Entity {ent.Uuid:X16}]");
            foreach (var comp in ComponentNames)
                TryDumpComponent(ent, comp);

            // For full reflection, if you can enumerate component names from engine, do it here!
        }

        private static readonly string[] ComponentNames = new[]
        {
            "GridPositionComponent", "StatsComponent", "FactionComponent",
            "MovementComponent", "MovementRequest", "FacingComponent", "AnimationStateComponent",
            "NameComponent", "PlayerTagComponent", "PathFollowingComponent", "ObstacleComponent",
            "TagComponent", "TargetGridPositionComponent",  "SpriteComponent",
            "PrefabIdComponent", "IDComponent", "SpriteAnimationComponent", "AnimationClipsComponent",
            // add more as needed...
        };

        private static void TryDumpComponent(Entity ent, string compName)
        {
            var buf = new byte[4096];
            int len = WanderSpire.Scripting.EngineInterop.GetComponentJson(
                ent.Context, new EntityId { id = (uint)ent.Id }, compName, buf, buf.Length);
            if (len <= 0) return;
            var json = System.Text.Encoding.UTF8.GetString(buf, 0, len);
            Console.WriteLine($"  [{compName}] {json}");
        }
    }
}
