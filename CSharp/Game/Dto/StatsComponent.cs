// Game / Components / StatsComponent.cs
using System;

namespace Game.Dto
{
    /// <summary>
    /// Pure-managed mirror of the old native <c>StatsComponent</c>.
    /// It can be deserialized either from a regular component blob
    /// or from script-data (key “stats”) when the native side no longer
    /// owns this type.
    /// </summary>
    [Serializable]
    public class StatsComponent
    {
        public int MaxHitpoints { get; set; }
        public int CurrentHitpoints { get; set; }

        public int MaxMana { get; set; }
        public int CurrentMana { get; set; }

        public int Accuracy { get; set; }
        public int Strength { get; set; }
        public int AttackType { get; set; }

        public int DefenseStab { get; set; }
        public int DefenseSlash { get; set; }
        public int DefenseCrush { get; set; }
        public int DefenseMagic { get; set; }
        public int DefenseRanged { get; set; }

        public float AttackRange { get; set; }
        public int AttackSpeed { get; set; }
    }
}
