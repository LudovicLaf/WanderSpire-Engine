namespace Game.Events
{
    /// <summary>
    /// Raised when an entity performs an attack on another.
    /// </summary>
    public struct AttackEvent
    {
        public uint AttackerId;
        public uint VictimId;
        public bool RightHand;

        public AttackEvent(uint attackerId, uint victimId, bool rightHand)
        {
            AttackerId = attackerId;
            VictimId = victimId;
            RightHand = rightHand;
        }
    }
}
