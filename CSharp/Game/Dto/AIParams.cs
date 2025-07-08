using System;

namespace Game.Dto
{
    [Serializable]
    public class AIParams
    {
        // Tuning
        public float wanderRadius { get; set; }
        public float wanderChance { get; set; }
        public int awarenessRange { get; set; }
        public int chaseRange { get; set; }

        /// <summary>Current FSM state (matches AIBehaviour.State enum)</summary>
        public int state { get; set; }

        /// <summary>Current tile [x,y] (so we can restore our last known position)</summary>
        public int[] position { get; set; }

        /// <summary>Home/origin tile [x,y], i.e. where we leash back to</summary>
        public int[] origin { get; set; }
    }
}
