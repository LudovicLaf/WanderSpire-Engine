using System.Text.Json;

namespace WanderSpire.Scripting.Utils
{
    /// <summary>
    /// Centralizes all JsonSerializerOptions in one place, and exposes
    /// a simple Deserialize<T> wrapper.
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Default options: case‐insensitive property names.
        /// </summary>
        public static readonly JsonSerializerOptions Default = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Includes fields (in addition to properties) and case‐insensitive names.
        /// </summary>
        public static readonly JsonSerializerOptions IncludeFields = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };

        /// <summary>
        /// Deserialize JSON text into T. If includeFields==true, we also read public fields.
        /// </summary>
        public static T? Deserialize<T>(string json, bool includeFields = false)
        {
            var opts = includeFields ? IncludeFields : Default;
            return JsonSerializer.Deserialize<T>(json, opts);
        }
    }
}
