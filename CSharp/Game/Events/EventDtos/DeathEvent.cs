namespace Game.Events
{
    /// <summary>
    /// Raised when an entity dies.
    /// </summary>
    public struct DeathEvent
    {
        public uint EntityId;

        public DeathEvent(uint entityId)
        {
            EntityId = entityId;
        }
    }
}
