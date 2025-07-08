namespace Game.Prefabs
{
    /// <summary>
    /// A factory that knows how to create and initialize one kind of entity.
    /// </summary>
    public interface IPrefabFactory
    {
        /// <summary>
        /// The key (e.g. the JSON “name” or code name) that identifies this prefab.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Instantiate the prefab at grid‐tile (x,y).
        /// </summary>
        WanderSpire.Scripting.Entity SpawnAtTile(int x, int y);
    }
}
