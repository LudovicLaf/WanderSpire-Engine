namespace Game.Events
{
    /// <summary>
    /// A request for an entity to move to (TargetX,TargetY).
    /// Run==true means “running” (only player systems will respect it).
    /// </summary>
    public struct MovementIntentEvent
    {
        public uint EntityId;
        public int TargetX;
        public int TargetY;
        public bool Run;

        public MovementIntentEvent(uint entityId, int tx, int ty, bool run)
        {
            EntityId = entityId;
            TargetX = tx;
            TargetY = ty;
            Run = run;
        }
    }
}
