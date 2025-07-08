// ScriptHost/Entity.cs
using System;
using System.Text;
using System.Text.Json;
using WanderSpire.Components;
using WanderSpire.Scripting.Utils;

namespace WanderSpire.Scripting
{
    public sealed partial class Entity : IEquatable<Entity>
    {
        public IntPtr Context { get; }
        /// <summary>Raw EnTT handle (0-based index in the native registry).</summary>
        public int Id { get; }

        internal Entity(IntPtr ctx, int id) => (Context, Id) = (ctx, id);

        public static Entity FromRaw(IntPtr ctx, int id) => new(ctx, id);

        /// <summary>EnTT uses 0 as a valid entity index, so only negatives are invalid.</summary>
        public bool IsValid => Id >= 0;

        // ─────────────────────────────────────────────────────────────────────
        // UUID helper
        // ─────────────────────────────────────────────────────────────────────
        public ulong Uuid
        {
            get
            {
                try
                {
                    var dto = GetComponent<IDComponent>("IDComponent");
                    return dto?.Uuid ?? 0UL;
                }
                catch { return 0UL; }
            }
        }

        public override string ToString() =>
            Uuid != 0UL ? $"Entity({Uuid:X16})" : $"Entity({Id})";

        public override bool Equals(object? obj) => Equals(obj as Entity);

        public bool Equals(Entity? other) =>
            other is not null && Uuid != 0UL && other.Uuid == Uuid;

        public override int GetHashCode() =>
            Uuid != 0UL ? Uuid.GetHashCode() : Id.GetHashCode();

        public static bool operator ==(Entity? a, Entity? b) =>
            ReferenceEquals(a, b) || (a is not null && b is not null && a.Equals(b));

        public static bool operator !=(Entity? a, Entity? b) => !(a == b);

        // ─────────────────────────────────────────────────────────────────────
        //  Component helpers
        // ─────────────────────────────────────────────────────────────────────
        public bool HasComponent(string compName)
        {
            if (Engine.Instance is null)
                throw new InvalidOperationException("Engine not initialized.");
            return Engine.Instance.HasComponent(this, compName);
        }

        public T GetField<T>(string compName, string fieldName)
        {
            if (Engine.Instance is null)
                throw new InvalidOperationException("Engine not initialized.");

            // fetch component JSON
            byte[] buffer = new byte[4096];
            int len = EngineInterop.GetComponentJson(
                Engine.Instance.Context,
                new EntityId { id = (uint)Id },
                compName, buffer, buffer.Length);

            if (len <= 0)
                throw new InvalidOperationException($"Component '{compName}' not found or has no data.");

            string json = Encoding.UTF8.GetString(buffer, 0, len);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty(fieldName, out var elem))
                throw new InvalidOperationException($"Field '{fieldName}' not found in '{compName}'.");

            // fast primitives first
            if (typeof(T) == typeof(int) && elem.ValueKind == JsonValueKind.Number && elem.TryGetInt32(out var iv))
                return (T)(object)iv;
            if (typeof(T) == typeof(float) && elem.ValueKind == JsonValueKind.Number && elem.TryGetSingle(out var fv))
                return (T)(object)fv;
            if (typeof(T) == typeof(bool) && (elem.ValueKind == JsonValueKind.True || elem.ValueKind == JsonValueKind.False))
                return (T)(object)elem.GetBoolean();
            if (typeof(T) == typeof(string) && elem.ValueKind == JsonValueKind.String)
                return (T)(object)elem.GetString()!;

            return JsonHelper.Deserialize<T>(elem.GetRawText())!;
        }

        public void SetField(string compName, string fieldName, int value) =>
            Engine.Instance?.Set(this, compName, fieldName, value);

        public void SetField(string compName, string fieldName, float value) =>
            Engine.Instance?.Set(this, compName, fieldName, value);

        public T? GetComponent<T>(string compName) where T : class
        {
            if (Engine.Instance is null)
                throw new InvalidOperationException("Engine not initialized.");

            byte[] buffer = new byte[4096];
            int len = EngineInterop.GetComponentJson(
                Engine.Instance.Context,
                new EntityId { id = (uint)Id },
                compName, buffer, buffer.Length);

            if (len <= 0) return null;

            string json = Encoding.UTF8.GetString(buffer, 0, len);
            return JsonHelper.Deserialize<T>(json, includeFields: true);
        }

        /// <summary>
        /// Sets (adds or replaces) the full component on this entity using a DTO.
        /// The DTO object must match the component’s structure.
        /// </summary>
        /// <typeparam name="T">DTO type (should match component)</typeparam>
        /// <param name="compName">Component name as registered in native</param>
        /// <param name="dto">Object with the new component data</param>
        /// <returns>True if operation succeeded, false otherwise</returns>
        public bool SetComponent<T>(string compName, T dto) where T : class
        {
            if (Engine.Instance is null)
                throw new InvalidOperationException("Engine not initialized.");
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            // Serialize the DTO (respect C# → C++ field casing)
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // or null for PascalCase, depends on native expectations
                IncludeFields = true // if you want fields too
            });

            int result = EngineInterop.SetComponentJson(
                Engine.Instance.Context,
                new EntityId { id = (uint)Id },
                compName,
                json);

            return result == 0;
        }

        public string? GetComponent(string compName)
        {
            if (Engine.Instance is null)
                throw new InvalidOperationException("Engine not initialized.");
            byte[] buffer = new byte[4096];
            int len = EngineInterop.GetComponentJson(
                Engine.Instance.Context,
                new EntityId { id = (uint)Id },
                compName, buffer, buffer.Length);
            if (len <= 0) return null;
            return Encoding.UTF8.GetString(buffer, 0, len);
        }


        public bool TryGetComponent<T>(string compName, out T? value) where T : class
        {
            value = GetComponent<T>(compName);
            return value != null;
        }

        /// <summary>
        /// **Safe** component removal.  
        /// If the entity doesn’t currently own the component we quietly return
        /// instead of letting the native registry assert.
        /// </summary>
        public bool RemoveComponent(string compName)
        {
            if (Engine.Instance is null)
                throw new InvalidOperationException("Engine not initialized.");

            // Bail early if the entity never had this component
            if (!HasComponent(compName))
                return false;

            int result = EngineInterop.RemoveComponent(
                Engine.Instance.Context,
                new EntityId { id = (uint)Id },
                compName);

            return result == 0;
        }

        // ==========================================================================
        //  Script-data black-board helpers
        // ==========================================================================
        public T? GetScriptData<T>(string key)
        {
            var buf = new byte[2048];
            int len = EngineInterop.GetScriptDataValue(
                Context,
                new EntityId { id = (uint)Id },
                key, buf, buf.Length);
            if (len <= 0) return default;

            string json = Encoding.UTF8.GetString(buf, 0, len);
            try
            {
                return JsonHelper.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Entity({Uuid:X16})] GetScriptData<'{typeof(T).Name}'>('{key}') failed: {ex}");
                return default;
            }
        }

        public void SetScriptData<T>(string key, T value)
        {
            string json = JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            int result = EngineInterop.SetScriptDataValue(Context, new EntityId { id = (uint)Id }, key, json);
            if (result != 0)
                Console.Error.WriteLine($"[Entity({Uuid:X16})] SetScriptData('{key}') failed (err {result})");
        }

        public void RemoveScriptData(string key)
        {
            int result = EngineInterop.RemoveScriptDataValue(Context, new EntityId { id = (uint)Id }, key);
            if (result != 0 && result != -3)
                Console.Error.WriteLine($"[Entity({Uuid:X16})] RemoveScriptData('{key}') failed (err {result})");
        }
    }
}
