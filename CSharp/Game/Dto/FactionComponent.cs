// Game / Components / FactionComponent.cs
using System;

namespace Game.Dto
{
    /// <summary>
    /// Managed version of the previous native <c>FactionComponent</c>.
    /// It describes basic alignment and hostility flags for AI logic.
    /// </summary>
    [Serializable]
    public class FactionComponent
    {
        public string Alignment { get; set; } = "neutral";  // good / neutral / bad
        public string Faction { get; set; } = string.Empty;

        public bool HostileToPlayer { get; set; }
        public bool HostileToGood { get; set; }
        public bool HostileToNeutral { get; set; }
        public bool HostileToBad { get; set; }

        /// <summary>
        /// Comma-separated faction names that this entity is hostile to.
        /// </summary>
        public string HostileToFactions { get; set; } = string.Empty;
    }
}
