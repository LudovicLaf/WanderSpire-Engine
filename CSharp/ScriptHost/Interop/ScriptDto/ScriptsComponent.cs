using System;

namespace WanderSpire.Scripting
{
    /// <summary>
    /// Black-board entry that stores which managed <see cref="Behaviour"/>
    /// class names must be attached to an entity after it spawns or reloads.
    ///
    /// Keeping it inside <c>ScriptHost</c> avoids a Game→ScriptHost→Game
    /// reference cycle.
    /// </summary>
    [Serializable]
    public sealed class ScriptsComponent
    {
        /// <summary>Fully-qualified or short Behaviour type names.</summary>
        public string[] Scripts { get; set; } = Array.Empty<string>();
    }
}
