// ScriptHost/Interfaces.cs

using WanderSpire.Scripting;

namespace ScriptHost
{
    public interface ITickReceiver
    {
        void OnTick(float dt);
    }
}

namespace WanderSpire.ScriptHost
{
    public abstract class Behaviour : global::ScriptHost.ITickReceiver
    {
        private bool _started;

        /// <summary>The engine entity this script is driving.</summary>
        public Entity Entity { get; private set; } = default!;

        /// <summary>Called by PrefabManager when the behaviour is created.</summary>
        public void _Attach(Entity entity)
            => Entity = entity;

        /// <summary>Override for one-time initialization.</summary>
        protected virtual void Start() { }

        /// <summary>Your per-frame logic.</summary>
        public abstract void Update(float dt);

        void global::ScriptHost.ITickReceiver.OnTick(float dt)
        {
            if (!_started)
            {
                Start();
                _started = true;
            }
            Update(dt);
        }
    }
}
