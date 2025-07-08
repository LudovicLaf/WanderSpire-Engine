// ScriptHost/ContentInterfaces.cs
namespace WanderSpire.Scripting.Content
{
    /// <summary>
    /// Author a C# (.csx) quest by implementing this interface.
    /// </summary>
    public interface IQuestDefinition
    {
        /// <summary>Unique quest ID (e.g. "orc_hunt").</summary>
        string Id { get; }
        /// <summary>Human-readable quest title.</summary>
        string Title { get; }
        /// <summary>Called once when the quest is first registered.</summary>
        void OnStart();
        /// <summary>Called each frame (dt in seconds) to update quest state.</summary>
        void OnTick(float dt);
    }

    /// <summary>
    /// Author a C# (.csx) encounter by implementing this interface.
    /// </summary>
    public interface IEncounterDefinition
    {
        /// <summary>Unique encounter ID (e.g. "tower_ambush").</summary>
        string Id { get; }
        /// <summary>Called to trigger the encounter from game code (pass in the source entity).</summary>
        void Trigger(Entity source);
    }
}
