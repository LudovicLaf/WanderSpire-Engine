using System;

namespace WanderSpire.Scripting
{
    /// <summary>
    /// Single source of truth for all managed ticks.  Listens to the native
    /// LogicTickEvent (which carries the tick index) and fans out to C#.
    /// </summary>
    public sealed class TickManager
    {
        public static TickManager Instance { get; } = new TickManager();

        private bool _hooked = false;

        public event Action<float, ulong>? Tick;

        public void Initialize(IntPtr ctx)
        {
            if (_hooked) return;
            _hooked = true;
            EventBus.Initialize(ctx);
            EventBus.LogicTick += ev =>
            {
                Engine.Instance!.TickCount = (int)ev.index;
                float dt = Engine.Instance.TickInterval;
                //Console.WriteLine($"[TickManager] Tick #{ev.index} (dt={dt:0.0000}s)");
                Tick?.Invoke(dt, ev.index);
            };
        }
    }

}
