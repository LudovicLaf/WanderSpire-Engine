namespace Game.Events
{
    /// <summary>
    /// Raised when an entity finishes one interpolation segment.
    /// </summary>
    public struct InterpolationCompleteEvent
    {
        public uint EntityId;

        public InterpolationCompleteEvent(uint entityId)
        {
            EntityId = entityId;
        }
    }
}
