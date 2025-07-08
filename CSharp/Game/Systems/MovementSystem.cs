// Game/Systems/MovementSystem.cs
using Game.Events;
using ScriptHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using WanderSpire.Components;
using WanderSpire.Core.Events;
using WanderSpire.Scripting;
using WanderSpire.Scripting.Utils;
using static WanderSpire.Scripting.EngineInterop;

namespace Game.Systems
{
    /// <summary>
    /// RuneScape‐style movement:
    ///  - Teleport grid on each logic tick
    ///  - One continuous, multi‐segment visual interpolation for the whole path
    /// </summary>
    public sealed class MovementSystem : ITickReceiver, IDisposable
    {
        public static MovementSystem? Instance { get; private set; }

        class State
        {
            public List<(int x, int y)> Path = new();
            public bool Run;
            public int NextIndex;        // next tile index to teleport into
            public float StepInterval;   // single‐tile duration
            public bool InterpStarted;   // have we kicked off the continuous interpolation?
        }

        readonly Dictionary<uint, State> _moving = new();

        public MovementSystem()
        {
            if (Instance != null) throw new InvalidOperationException("Only one MovementSystem allowed");
            Instance = this;
            GameEventBus.Event<MovementIntentEvent>.Subscribe(OnIntent);
        }

        public void Dispose()
        {
            GameEventBus.Event<MovementIntentEvent>.Unsubscribe(OnIntent);
            _moving.Clear();
            Instance = null;
        }

        // Runs exactly once per logic tick (dt == Engine.TickInterval)
        public void OnTick(float dt)
        {
            if (_moving.Count == 0 || InterpolationSystem.Instance == null)
                return;

            foreach (var kv in _moving.ToList())
            {
                uint id = kv.Key;
                var st = kv.Value;

                // 1) On the very first tile of this move, kick off the full‐path interpolation
                if (!st.InterpStarted)
                {
                    float totalDuration = st.StepInterval * (st.Path.Count - 1);
                    InterpolationSystem.Instance.StartPathInterpolation(
                        id,
                        st.Path,
                        totalDuration
                    );
                    st.InterpStarted = true;
                }

                // 2) If there are still tiles left to teleport into this tick, do one or two
                int steps = st.Run ? 2 : 1;
                for (int i = 0; i < steps && st.NextIndex < st.Path.Count; i++)
                {
                    var from = st.Path[st.NextIndex - 1];
                    var to = st.Path[st.NextIndex];

                    // Fire MoveStarted only on the first real step
                    if (st.NextIndex == 1)
                    {
                        GameEventBus.Event<MoveStartedEvent>.Publish(new MoveStartedEvent
                        {
                            entity = id,
                            fromTile = new[] { from.x, from.y },
                            toTile = new[] { to.x, to.y }
                        });
                    }

                    // Teleport the logical position
                    var dto = new GridPositionComponent
                    {
                        Tile = new[] { to.x, to.y },
                        TileObj = new GridPositionComponent.Vec2 { X = to.x, Y = to.y }
                    };
                    ComponentWriter.Patch(id, nameof(GridPositionComponent), dto);

                    st.NextIndex++;

                    // If that was the last tile, finish up
                    if (st.NextIndex == st.Path.Count)
                    {
                        GameEventBus.Event<MoveCompletedEvent>.Publish(new MoveCompletedEvent
                        {
                            entity = id,
                            tile = new[] { to.x, to.y }
                        });
                        _moving.Remove(id);
                        break;
                    }
                }
            }
        }

        private void OnIntent(MovementIntentEvent ev)
        {
            // 1) Read current grid position
            var ent = Entity.FromRaw(Engine.Instance.Context, (int)ev.EntityId);
            var grid = ent.GetComponent<GridPositionComponent>(nameof(GridPositionComponent))
                    ?? ent.GetScriptData<GridPositionComponent>(nameof(GridPositionComponent));
            var (sx, sy) = grid?.AsTuple() ?? (0, 0);

            // 2) Ask native for the full path
            IntPtr raw = Engine_FindPath(
                Engine.Instance.Context,
                sx, sy,
                ev.TargetX, ev.TargetY,
                ev.Run ? 1000 : 64);
            if (raw == IntPtr.Zero) return;
            string json = Marshal.PtrToStringAnsi(raw) ?? "[]";
            Engine_FreeString(raw);

            var arr = JsonHelper.Deserialize<List<int[]>>(json);
            if (arr == null || arr.Count < 2) return;
            var path = arr.Select(a => (x: a[0], y: a[1])).ToList();

            // 3) Queue up for the next tick
            _moving[ev.EntityId] = new State
            {
                Path = path,
                Run = ev.Run,
                NextIndex = 1,  // index 0 is spot-in-place
                StepInterval = Engine.Instance.TickInterval * (ev.Run ? 0.5f : 1f),
                InterpStarted = false
            };
        }
    }
}
