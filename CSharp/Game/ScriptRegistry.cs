using System;
using System.Linq;
using System.Reflection;
using WanderSpire.ScriptHost;

namespace Game
{
    /// <summary>
    /// Finds all compiled Behaviours in this assembly.
    /// </summary>
    public static class ScriptRegistry
    {
        public static readonly Type[] AllBehaviours =
            Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t =>
                        typeof(Behaviour).IsAssignableFrom(t)
                        && !t.IsAbstract
                        && t.Namespace?.StartsWith("Game.Behaviours") == true
                    )
                    .ToArray();
    }
}
