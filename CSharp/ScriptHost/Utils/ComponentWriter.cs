// ScriptHost/Utils/ComponentWriter.cs
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WanderSpire.Scripting.Utils
{
    /// <summary>
    /// Serialize any DTO (or anonymous object) and patch a component via
    /// the reflective <c>SetComponentJson</c>.  Eliminates brittle
    /// string-interpolation and keeps call-sites concise.
    /// </summary>
    public static class ComponentWriter
    {
        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
#if NET6_0_OR_GREATER
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#else
            IgnoreNullValues           = true,
#endif
            IncludeFields = true,
            WriteIndented = false
        };

        public static void Patch<T>(uint entityId, string componentName, T dto)
        {
            var eng = Engine.Instance ?? throw new InvalidOperationException("Engine not initialised");
            string json = JsonSerializer.Serialize(dto!, _opts);

            EngineInterop.SetComponentJson(
                eng.Context,
                new EntityId { id = entityId },
                componentName,
                json);
        }
    }
}
