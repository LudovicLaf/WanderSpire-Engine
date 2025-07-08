// Game/Prefabs/JsonPrefabFactory.cs
using Game.Dto;
using System;
using System.Linq;
using WanderSpire.Scripting;

namespace Game.Prefabs
{
    public class JsonPrefabFactory : IPrefabFactory
    {
        public string Key { get; }
        private readonly string _jsonPath;

        public JsonPrefabFactory(string key, string jsonPath)
        {
            Key = key;
            _jsonPath = jsonPath;
        }

        public Entity SpawnAtTile(int x, int y)
        {
            // 1) Spawn the native prefab
            var entity = PrefabManager.SpawnAtTile(Key, x, y);

            // 2) Read any “scripts” array from the JSON and just store it;
            //    actual attachment now happens inside ScriptEngine.BindEntityScripts().
            var sc = entity.GetScriptData<ScriptsComponent>("ScriptsComponent");
            string[] scripts = sc?.Scripts
                              ?? entity.GetScriptData<string[]>("scripts")
                              ?? Array.Empty<string>();

            // 3) If this prefab uses AIBehaviour, overwrite its AIParams origin
            if (scripts.Contains("Game.Behaviours.AIBehaviour") ||
                scripts.Contains("AIBehaviour"))
            {
                // Load existing AIParams (from native component or previous script-data)
                var aiParams = entity.GetScriptData<AIParams>("AIParams")
                               ?? entity.GetComponent<AIParams>("AIParams")
                               ?? new AIParams();

                // Overwrite origin and position to current spawn tile
                aiParams.origin = new[] { x, y };
                aiParams.position = new[] { x, y };
                aiParams.state = 0; // reset FSM state if desired

                // Persist into script-data so Save() will pick it up
                entity.SetScriptData("AIParams", aiParams);
            }

            return entity;
        }
    }
}
