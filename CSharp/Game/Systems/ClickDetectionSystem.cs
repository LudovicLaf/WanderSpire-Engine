// Game/Systems/ClickDetectionSystem.cs - SIMPLIFIED
using Game.Events;
using ScriptHost;
using System;
using WanderSpire.Core.Events;
using WanderSpire.Scripting;
using static WanderSpire.Scripting.EngineInterop;

namespace Game.Systems
{
    /// <summary>
    /// Simple click detector that respects ImGui input capture.
    /// </summary>
    public sealed class ClickDetectionSystem : ITickReceiver, IDisposable
    {
        private readonly Action<FrameRenderEvent> _frameHandler;

        public ClickDetectionSystem()
        {
            _frameHandler = ev => OnFrameRender(ev);
            EventBus.FrameRender += _frameHandler;
        }

        public void OnTick(float dt)
        {
            // no per‐tick logic
        }

        private void OnFrameRender(FrameRenderEvent _)
        {
            bool mouseDown = Input.GetMouseButtonDown(MouseButton.Left);
            if (!mouseDown)
                return;

            // Simple check: does ImGui want mouse input?
            if (DebugUISystem.Instance?.WantsInput() == true)
                return;

            // Convert mouse → tile and publish click event
            var ctx = Engine.Instance.Context;
            Engine_GetMouseTile(ctx, out int tx, out int ty);

            GameEventBus.Event<TileClickEvent>.Publish(new TileClickEvent(tx, ty));
        }

        public void Dispose()
        {
            EventBus.FrameRender -= _frameHandler;
        }
    }
}