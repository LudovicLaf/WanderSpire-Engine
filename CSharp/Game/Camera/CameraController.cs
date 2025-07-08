// Game/Camera/CameraController.cs
using Game.Systems;
using System;
using WanderSpire.Core.Events;
using WanderSpire.Scripting;
using static WanderSpire.Scripting.EngineInterop;

namespace Game.Camera
{
    /// <summary>
    /// Unity-style “camera follows this entity” helper.
    /// Instead of using the native Engine_GetEntityWorldPosition (which is grid-based),
    /// we fetch the interpolated world position out of our InterpolationSystem.  If the
    /// entity is not currently interpolating, we fall back to grid-center via Engine_GetEntityWorldPosition.
    /// </summary>
    public static class CameraController
    {
        // The entity we are currently tracking
        private static Entity? _followTarget;

        // Cached delegate so we can unsubscribe cleanly
        private static Action<FrameRenderEvent>? _onFrameRender;

        /// <summary>
        /// Instantly teleport the camera to world position (x,y).
        /// </summary>
        public static void MoveTo(float x, float y)
        {
            var ctx = Engine.Instance.Context;
            Engine_SetCameraPosition(ctx, x, y);
        }

        /// <summary>
        /// Begin “following” the given Entity.  
        /// Each render‐frame, the camera is moved to that Entity’s current world position—
        /// first checking InterpolationSystem.TryGetCurrentPosition; if there is no active
        /// interpolation, we then call the grid‐based Engine_GetEntityWorldPosition.
        /// If you call Follow again, any previous follow is cleared first.
        /// </summary>
        public static void Follow(Entity target)
        {
            if (target is null || !target.IsValid)
                throw new ArgumentException("Invalid entity passed to CameraController.Follow", nameof(target));

            // If we were already following something, clear it first
            ClearFollow();

            _followTarget = target;

            // On each FrameRenderEvent, teleport the camera to the target’s current world position
            _onFrameRender = ev =>
            {
                if (_followTarget is null || !_followTarget.IsValid)
                {
                    // If the entity becomes invalid, stop following
                    ClearFollow();
                    return;
                }

                uint eid = (uint)_followTarget.Id;
                float wx, wy;

                // 1) Try to get the interpolated position from InterpolationSystem
                if (InterpolationSystem.TryGetCurrentVisualPosition(eid, out wx, out wy))
                {
                    Engine_SetCameraPosition(Engine.Instance.Context, wx, wy);
                }
                else
                {
                    // 2) No active interpolation → fallback to grid center
                    Engine_GetEntityWorldPosition(
                        Engine.Instance.Context,
                        new EntityId { id = eid },
                        out float gridX,
                        out float gridY);
                    Engine_SetCameraPosition(Engine.Instance.Context, gridX, gridY);
                }
            };

            // Immediately “snap” to the target right now, so there’s no frame of delay:
            {
                uint eid = (uint)target.Id;
                float wx, wy;
                if (InterpolationSystem.TryGetCurrentVisualPosition(eid, out wx, out wy))
                {
                    Engine_SetCameraPosition(Engine.Instance.Context, wx, wy);
                }
                else
                {
                    Engine_GetEntityWorldPosition(
                        Engine.Instance.Context,
                        new EntityId { id = eid },
                        out float gridX,
                        out float gridY);
                    Engine_SetCameraPosition(Engine.Instance.Context, gridX, gridY);
                }
            }

            // Subscribe to FrameRender so that _onFrameRender runs each frame
            EventBus.FrameRender += _onFrameRender;
        }

        /// <summary>
        /// Stop following any entity.  The camera will remain where it is.
        /// </summary>
        public static void ClearFollow()
        {
            if (_onFrameRender is not null)
            {
                EventBus.FrameRender -= _onFrameRender;
                _onFrameRender = null;
            }
            _followTarget = null;
        }
    }
}
