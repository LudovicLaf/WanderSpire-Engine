namespace WanderSpire.Scripting
{
    public static class PrefabManager
    {
        public static Entity SpawnAtTile(string prefabName, int x, int y)
        {
            var ctx = Engine.Instance!.Context;
            var entityId = EngineInterop.Prefab_InstantiateAtTile(ctx, prefabName, x, y);
            var ent = Entity.FromRaw(ctx, (int)entityId.id);
            // Immediately attach any ScriptsComponent behaviours
            ScriptEngine.Current?.BindEntityScripts();
            return ent;
        }

        public static Entity Spawn(string prefabName, float worldX, float worldY)
        {
            var ctx = Engine.Instance!.Context;
            // FIXED: Extract .id from EntityId struct
            var entityId = EngineInterop.InstantiatePrefab(ctx, prefabName, worldX, worldY);
            var ent = Entity.FromRaw(ctx, (int)entityId.id);
            // Also bind here for code‐spawned prefabs
            ScriptEngine.Current?.BindEntityScripts();
            return ent;
        }
    }
}