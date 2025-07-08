namespace Game.Events
{
    /// <summary>
    /// Raised when an entity takes damage.
    /// </summary>
    public struct HurtEvent
    {
        public uint EntityId;
        public int Damage;

        public HurtEvent(uint entityId, int damage)
        {
            EntityId = entityId;
            Damage = damage;
        }
    }
}
