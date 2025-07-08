// ScriptHost/WanderSpireEngine.cs
using System;
using System.Text;
using System.Text.Json;

namespace WanderSpire.Scripting
{
    /// <summary>C# façade over the native context + reflection helpers.</summary>
    public sealed class Engine
    {
        public static Engine? Instance { get; private set; }
        private readonly IntPtr _ctx;

        private Engine(IntPtr ctx) => _ctx = ctx;

        public static void Initialize(IntPtr ctx)
        {
            if (Instance != null)
                throw new InvalidOperationException("Engine already initialized.");
            Instance = new Engine(ctx);
        }

        public IntPtr Context => _ctx;

        public float TickInterval => EngineInterop.Engine_GetTickInterval(_ctx);
        public int TickCount { get; internal set; }

        public float TileSize => EngineInterop.Engine_GetTileSize(_ctx);

        /* ───────────────────────── entity helpers ───────────────────────── */
        public Entity CreateEntity()
        {
            var entityId = EngineInterop.CreateEntity(_ctx);
            return Entity.FromRaw(_ctx, (int)entityId.id);
        }

        public void DestroyEntity(Entity e)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            EngineInterop.DestroyEntity(_ctx, entityId);
        }

        public Entity SpawnPrefabAtTile(string prefab, int x, int y)
        {
            var entityId = EngineInterop.Prefab_InstantiateAtTile(_ctx, prefab, x, y);
            return Entity.FromRaw(_ctx, (int)entityId.id);
        }

        public Entity Spawn(string prefab, float x, float y)
        {
            var entityId = EngineInterop.InstantiatePrefab(_ctx, prefab, x, y);
            return Entity.FromRaw(_ctx, (int)entityId.id);
        }

        public bool HasComponent(Entity e, string comp)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            return EngineInterop.HasComponent(_ctx, entityId, comp) != 0;
        }

        /* ───────────────────────── field setters (FIXED) ─────────────────── */
        public void Set(Entity e, string comp, string field, int v)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            bool success = EngineInterop.SetComponentField(_ctx, entityId, comp, field, v);

            if (!success)
                Console.Error.WriteLine(
                    $"[Engine.Set] ({comp}.{field}) failed on {e.Id}");
        }

        public void Set(Entity e, string comp, string field, float v)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            bool success = EngineInterop.SetComponentField(_ctx, entityId, comp, field, v);

            if (!success)
                Console.Error.WriteLine(
                    $"[Engine.Set] ({comp}.{field}) failed on {e.Id}");
        }

        public void Set(Entity e, string comp, string field, bool v)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            bool success = EngineInterop.SetComponentField(_ctx, entityId, comp, field, v ? 1 : 0);

            if (!success)
                Console.Error.WriteLine(
                    $"[Engine.Set] ({comp}.{field}) failed on {e.Id}");
        }

        /* ───────────────────────── field getters (FIXED) ─────────────────── */
        public T Get<T>(Entity e, string comp, string field) where T : struct
        {
            var entityId = new EntityId { id = (uint)e.Id };
            return EngineInterop.GetComponentField<T>(_ctx, entityId, comp, field);
        }

        public int GetInt(Entity e, string comp, string field)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            byte[] buf = new byte[4096];
            int len = EngineInterop.GetComponentJson(_ctx, entityId, comp, buf, buf.Length);
            if (len <= 0)
                throw new InvalidOperationException(
                    $"Component '{comp}' not found on entity {e.Id}");

            string json = Encoding.UTF8.GetString(buf, 0, len);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(field, out var elem)
                && elem.ValueKind == JsonValueKind.Number
                && elem.TryGetInt32(out int value))
            {
                return value;
            }

            throw new InvalidOperationException(
                $"Field '{field}' not found or not an integer in '{comp}'");
        }

        public float GetFloat(Entity e, string comp, string field)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            byte[] buf = new byte[4096];
            int len = EngineInterop.GetComponentJson(_ctx, entityId, comp, buf, buf.Length);
            if (len <= 0)
                throw new InvalidOperationException(
                    $"Component '{comp}' not found on entity {e.Id}");

            string json = Encoding.UTF8.GetString(buf, 0, len);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(field, out var elem)
                && elem.ValueKind == JsonValueKind.Number)
            {
                if (elem.TryGetSingle(out float f)) return f;
                if (elem.TryGetDouble(out double d)) return (float)d;
            }

            throw new InvalidOperationException(
                $"Field '{field}' not found or not a float in '{comp}'");
        }

        public bool GetBool(Entity e, string comp, string field)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            byte[] buf = new byte[4096];
            int len = EngineInterop.GetComponentJson(_ctx, entityId, comp, buf, buf.Length);
            if (len <= 0)
                throw new InvalidOperationException(
                    $"Component '{comp}' not found on entity {e.Id}");

            string json = Encoding.UTF8.GetString(buf, 0, len);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(field, out var elem))
            {
                if (elem.ValueKind == JsonValueKind.True) return true;
                if (elem.ValueKind == JsonValueKind.False) return false;
                if (elem.ValueKind == JsonValueKind.Number && elem.TryGetInt32(out int value))
                    return value != 0;
            }

            throw new InvalidOperationException(
                $"Field '{field}' not found or not a boolean in '{comp}'");
        }

        /* ───────────────────────── component JSON helpers ─────────────────── */
        public void SetComponent(Entity e, string comp, string jsonData)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            int result = EngineInterop.SetComponentJson(_ctx, entityId, comp, jsonData);
            if (result != 0)
                Console.Error.WriteLine($"[Engine.SetComponent] Failed to set {comp} on entity {e.Id}");
        }

        public string GetComponent(Entity e, string comp)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            byte[] buf = new byte[4096];
            int len = EngineInterop.GetComponentJson(_ctx, entityId, comp, buf, buf.Length);
            if (len <= 0)
                return string.Empty;

            return Encoding.UTF8.GetString(buf, 0, len);
        }

        public void RemoveComponent(Entity e, string comp)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            EngineInterop.RemoveComponent(_ctx, entityId, comp);
        }

        /* ───────────────────────── script data helpers ─────────────────── */
        public void SetScriptData(Entity e, string key, object value)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            string jsonValue = JsonSerializer.Serialize(value);
            int result = EngineInterop.SetScriptDataValue(_ctx, entityId, key, jsonValue);
            if (result != 0)
                Console.Error.WriteLine($"[Engine.SetScriptData] Failed to set {key} on entity {e.Id}");
        }

        public T GetScriptData<T>(Entity e, string key)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            byte[] buf = new byte[4096];
            int len = EngineInterop.GetScriptDataValue(_ctx, entityId, key, buf, buf.Length);
            if (len <= 0)
                return default(T);

            string json = Encoding.UTF8.GetString(buf, 0, len);
            return JsonSerializer.Deserialize<T>(json);
        }

        public void RemoveScriptData(Entity e, string key)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            EngineInterop.RemoveScriptDataValue(_ctx, entityId, key);
        }

        /* ───────────────────────── camera helpers ─────────────────────── */
        public void SetPlayer(Entity player)
        {
            var entityId = new EntityId { id = (uint)player.Id };
            EngineInterop.Engine_SetPlayerEntity(_ctx, entityId);
        }

        public void SetCameraTarget(Entity target)
        {
            var entityId = new EntityId { id = (uint)target.Id };
            EngineInterop.Engine_SetCameraTarget(_ctx, entityId);
        }

        public void ClearCameraTarget()
        {
            EngineInterop.Engine_ClearCameraTarget(_ctx);
        }

        public void SetCameraPosition(float x, float y)
        {
            EngineInterop.Engine_SetCameraPosition(_ctx, x, y);
        }

        /* ───────────────────────── input helpers ─────────────────────── */
        public (int x, int y) GetMouseTile()
        {
            EngineInterop.Engine_GetMouseTile(_ctx, out int x, out int y);
            return (x, y);
        }

        public (float x, float y) GetEntityWorldPosition(Entity e)
        {
            var entityId = new EntityId { id = (uint)e.Id };
            EngineInterop.Engine_GetEntityWorldPosition(_ctx, entityId, out float x, out float y);
            return (x, y);
        }

        /* ───────────────────────── pathfinding helpers ─────────────────── */
        public string FindPath(int startX, int startY, int targetX, int targetY, int maxRange = 100)
        {
            IntPtr pathPtr = EngineInterop.Engine_FindPath(_ctx, startX, startY, targetX, targetY, maxRange);
            return EngineInterop.GetPathfindingResult(pathPtr);
        }

        /* ───────────────────────── scene management ─────────────────── */
        public void SaveScene(string path)
        {
            EngineInterop.SceneManager_SaveScene(_ctx, path);
        }

        public bool LoadScene(string path, out uint playerId, out float playerX, out float playerY, out uint tilemapId)
        {
            return EngineInterop.SceneManager_LoadScene(_ctx, path, out playerId, out playerX, out playerY, out tilemapId);
        }
    }
}