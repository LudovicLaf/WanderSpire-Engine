// Game/Behaviours/PlayerController.cs - Precise ImGui input checking
using Game.Events;
using System;
using WanderSpire.ScriptHost;
using WanderSpire.Scripting;
using WanderSpire.Scripting.UI; // For ImGuiManager
using static WanderSpire.Scripting.Input;
using static WanderSpire.Scripting.KeyCode;

namespace Game.Behaviours
{
    public class PlayerController : Behaviour, IDisposable
    {
        private bool _runToggled = false;
        private Action<WanderSpire.Core.Events.FrameRenderEvent>? _onFrameRender;

        protected override void Start()
        {
            Console.WriteLine($"[PlayerController] Start() called on entity {Entity.Uuid:X16}");

            GameEventBus.Event<TileClickEvent>.Subscribe(OnTileClick);

            _onFrameRender = ev => OnFrame(ev);
            EventBus.FrameRender += _onFrameRender;
        }

        public override void Update(float dt)
        {
            // No logic here
        }

        private void OnFrame(WanderSpire.Core.Events.FrameRenderEvent _)
        {
            // Check if ImGui wants keyboard input before processing keyboard
            if (ImGuiManager.Instance?.WantCaptureKeyboard == true)
            {
                return;
            }

            if (GetKeyDown(R))
            {
                _runToggled = !_runToggled;
                Console.WriteLine(_runToggled
                    ? "[PlayerController] Run mode ON"
                    : "[PlayerController] Run mode OFF");
            }
        }

        private void OnTileClick(TileClickEvent ev)
        {
            // Check if ImGui wants mouse input before processing mouse clicks
            if (ImGuiManager.Instance?.WantCaptureMouse == true)
            {
                return;
            }

            if (!Entity.IsValid)
                return;

            GameEventBus.Event<MovementIntentEvent>.Publish(
                new MovementIntentEvent((uint)Entity.Id, ev.X, ev.Y, _runToggled));

            Console.WriteLine($"[PlayerController] Moving to ({ev.X}, {ev.Y}) - Run: {_runToggled}");
        }

        public void Dispose()
        {
            GameEventBus.Event<TileClickEvent>.Unsubscribe(OnTileClick);

            if (_onFrameRender != null)
            {
                EventBus.FrameRender -= _onFrameRender;
                _onFrameRender = null;
            }
        }
    }
}