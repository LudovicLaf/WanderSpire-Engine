using System;
using System.Collections.Generic;
using System.Linq;
using WanderSpire.ScriptHost;

namespace Game
{
    /// <summary>
    /// Runtime resolver handed to ScriptHost so that it can instantiate
    /// concrete Behaviour classes without referencing Game at compile-time.
    /// </summary>
    public static class BehaviourFactory
    {
        // Cache mapping both full and short names to Type
        private static readonly Dictionary<string, Type> _behaviourMap;

        static BehaviourFactory()
        {
            // 1) Grab all non-abstract behaviours from the registry
            // 2) For each, emit (FullName, Type) and (Name, Type), if FullName != null
            // 3) Build a single dictionary (case-insensitive) in one pass
            _behaviourMap = ScriptRegistry.AllBehaviours
                .Where(t => !t.IsAbstract)
                .SelectMany(t =>
                {
                    if (t.FullName is null)
                        return new[] { new KeyValuePair<string, Type>(t.Name, t) };

                    return new[]
                    {
                        new KeyValuePair<string, Type>(t.FullName, t),
                        new KeyValuePair<string, Type>(t.Name,     t)
                    };
                })
                .ToDictionary(pair => pair.Key!, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Attempt to create a new instance of the named Behaviour. Returns null if not found.
        /// </summary>
        public static Behaviour? Resolve(string behaviourName)
        {
            if (_behaviourMap.TryGetValue(behaviourName, out var type))
                return (Behaviour?)Activator.CreateInstance(type);

            return null;
        }
    }
}
