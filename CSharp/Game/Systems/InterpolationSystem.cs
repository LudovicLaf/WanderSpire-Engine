// Game/Systems/InterpolationSystem.cs
using Game.Events;
using ScriptHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using WanderSpire.Components;
using WanderSpire.Core.Events;
using WanderSpire.Scripting;
using WanderSpire.Scripting.Utils;

namespace Game.Systems
{
    /// <summary>
    /// Frame-based interpolation system for buttery smooth movement like RuneScape.
    /// 
    /// Key design:
    /// - Logical position updates on tick system (0.6s intervals)
    /// - Visual interpolation runs every frame (60fps) completely independent
    /// - Uses high-precision Stopwatch for smooth timing
    /// - Smooth continuous interpolation across entire path (no per-tile easing resets)
    /// </summary>
    public sealed class InterpolationSystem : ITickReceiver, IDisposable
    {
        public static InterpolationSystem Instance { get; private set; } = null!;

        // Path-based, multi-segment states
        private class PathState
        {
            public uint EntityId;
            public List<Segment> Segments = new();
            public Stopwatch Timer = null!;
            public float TotalDuration;
            public bool IsComplete;
        }
        private struct Segment
        {
            public float FromX, FromY;
            public float ToX, ToY;
        }
        private readonly Dictionary<uint, PathState> _pathStates = new();

        // Shared lookup for current visual positions
        private readonly Dictionary<uint, (float x, float y)> _currentVisual = new();

        private readonly Action<FrameRenderEvent> _onFrameRender;

        public InterpolationSystem()
        {
            if (Instance != null) throw new InvalidOperationException("Only one InterpolationSystem allowed");
            Instance = this;

            _onFrameRender = OnFrameRender;
            EventBus.FrameRender += _onFrameRender;

            Console.WriteLine("[InterpolationSystem] Initialized with continuous path-based interpolation");
        }

        public void Dispose()
        {
            EventBus.FrameRender -= _onFrameRender;
            _pathStates.Clear();
            _currentVisual.Clear();
            Instance = null!;
            Console.WriteLine("[InterpolationSystem] Disposed");
        }

        public void OnTick(float dt)
        {
            // Intentionally empty; interpolation is entirely frame-driven
        }

        /// <summary>
        /// Every-frame update: advances path-based interpolations continuously.
        /// </summary>
        private void OnFrameRender(FrameRenderEvent _)
        {
            if (_pathStates.Count == 0) return;

            var finished = new List<uint>();
            foreach (var kv in _pathStates)
            {
                var ps = kv.Value;
                if (ps.IsComplete) continue;

                float elapsed = (float)ps.Timer.Elapsed.TotalSeconds;
                if (elapsed >= ps.TotalDuration)
                {
                    // Snap to final endpoint
                    var last = ps.Segments[^1];
                    PatchTransform(ps.EntityId, last.ToX, last.ToY);
                    _currentVisual[ps.EntityId] = (last.ToX, last.ToY);

                    // **Notify that the entire interpolation is done**
                    GameEventBus.Event<InterpolationCompleteEvent>
                        .Publish(new InterpolationCompleteEvent(ps.EntityId));

                    ps.IsComplete = true;
                    finished.Add(ps.EntityId);
                    continue;
                }

                // Continuous linear interpolation across the multi‐segment path
                float globalT = elapsed / ps.TotalDuration;
                int segCount = ps.Segments.Count;
                float f = globalT * segCount;
                int idx = Math.Min((int)Math.Floor(f), segCount - 1);
                float localT = f - idx;

                var seg = ps.Segments[idx];
                float x = seg.FromX + (seg.ToX - seg.FromX) * localT;
                float y = seg.FromY + (seg.ToY - seg.FromY) * localT;

                _currentVisual[ps.EntityId] = (x, y);
                PatchTransform(ps.EntityId, x, y);
            }

            // Remove finished entries
            foreach (var id in finished)
                _pathStates.Remove(id);
        }

        /// <summary>
        /// Kick off a continuous interpolation over an entire path.
        /// </summary>
        /// <param name="entityId">ID of the entity to interpolate</param>
        /// <param name="path">List of grid tiles (x,y) including start and end</param>
        /// <param name="totalDurationSec">Total time in seconds for entire path</param>
        public void StartPathInterpolation(uint entityId, List<(int x, int y)> path, float totalDurationSec)
        {
            if (path == null || path.Count < 2) return;
            float tile = Engine.Instance.TileSize;

            var ps = new PathState
            {
                EntityId = entityId,
                Timer = Stopwatch.StartNew(),
                TotalDuration = Math.Max(0.01f, totalDurationSec),
                IsComplete = false
            };

            // Build raw segments without per-segment duration
            for (int i = 1; i < path.Count; i++)
            {
                var (sx, sy) = path[i - 1];
                var (tx, ty) = path[i];
                ps.Segments.Add(new Segment
                {
                    FromX = sx * tile + tile * 0.5f,
                    FromY = sy * tile + tile * 0.5f,
                    ToX = tx * tile + tile * 0.5f,
                    ToY = ty * tile + tile * 0.5f
                });
            }

            _pathStates[entityId] = ps;
            // Initialize at start point
            var first = ps.Segments[0];
            _currentVisual[entityId] = (first.FromX, first.FromY);
            PatchTransform(entityId, first.FromX, first.FromY);
        }

        /// <summary>
        /// Stop any interpolation and snap to its endpoint.
        /// </summary>
        public void Stop(uint entityId)
        {
            if (_pathStates.TryGetValue(entityId, out var ps))
            {
                // Snap to end
                var last = ps.Segments[^1];
                PatchTransform(entityId, last.ToX, last.ToY);
                _currentVisual[entityId] = (last.ToX, last.ToY);
                _pathStates.Remove(entityId);
            }
        }

        /// <summary>
        /// True if a path interpolation is in progress.
        /// </summary>
        public bool IsPathActive(uint entityId)
            => _pathStates.ContainsKey(entityId);

        /// <summary>
        /// Allows external callers (e.g. CameraController) to grab the latest
        /// frame-interpolated position for an entity.
        /// </summary>
        public static bool TryGetCurrentVisualPosition(uint entityId, out float x, out float y)
        {
            if (Instance != null && Instance._currentVisual.TryGetValue(entityId, out var p))
            {
                x = p.x; y = p.y; return true;
            }
            x = y = 0f; return false;
        }

        // ── INTERNAL HELPERS ──────────────────────────────────────────────
        private static void PatchTransform(uint entityId, float worldX, float worldY)
        {
            try
            {
                var dto = new TransformComponent
                {
                    LocalPosition = new[] { worldX, worldY },
                    LocalRotation = 0f,
                    LocalScale = new[] { 1f, 1f }
                };
                ComponentWriter.Patch(entityId, nameof(TransformComponent), dto);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[InterpolationSystem] PatchTransform failed: {ex.Message}");
            }
        }
    }
}
