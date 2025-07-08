using System;

namespace WanderSpire.Components
{
    [Serializable]
    public class PrefabIdComponent
    {
        public uint PrefabId { get; set; }    // stable numeric ID per prefab type
        public string PrefabName { get; set; }  // the original prefab key
    }
}
