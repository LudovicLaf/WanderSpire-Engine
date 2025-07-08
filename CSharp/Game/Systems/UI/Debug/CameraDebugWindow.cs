using Game.Camera;
using ScriptHost;
using System;
using System.Numerics;
using WanderSpire.Components;
using WanderSpire.Scripting;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Live camera monitor & controller wired to <see cref="CameraController"/> and engine API.
    /// </summary>
    public sealed class CameraDebugWindow : ImGuiWindowBase, IDisposable
    {
        public override string Title => "Camera Debug";

        // ───────────────────────── colours ─────────────────────────
        private readonly Vector4 _primary = new(0.22f, 0.58f, 0.93f, 1f);
        private readonly Vector4 _success = new(0.22f, 0.80f, 0.46f, 1f);
        private readonly Vector4 _danger = new(0.93f, 0.23f, 0.29f, 1f);
        private readonly Vector4 _dim = new(0.55f, 0.55f, 0.58f, 1f);

        // ───────────────────────── runtime state ─────────────────────────
        private Vector2 _pos;               // cached camera position (updated each frame)
        private bool _following;
        private Entity? _followEnt;
        private string _followTarget = "None";

        private float TileSize => Engine.Instance.TileSize;

        public override void Render()
        {
            RefreshPosition();

            if (!BeginWindow())
            {
                EndWindow();
                return;
            }

            DrawHeader();
            ImGui.Separator();
            DrawStatus();
            ImGui.Separator();
            DrawControls();
            ImGui.Separator();
            DrawPreciseInput();

            EndWindow();
        }

        // ───────────────────────── header ─────────────────────────
        private void DrawHeader()
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(_primary, FontAwesome5.Camera);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.Text("Camera Controller");

            ImGui.SameLine(ImGui.GetWindowWidth() - 100);
            ImGui.TextColored(_dim, _following ? "FOLLOW" : "FREE");
        }

        // ───────────────────────── status ─────────────────────────
        private void DrawStatus()
        {
            if (!ImGui.CollapsingHeader("Status", ImGuiTreeNodeFlags.DefaultOpen))
                return;

            ImGui.Columns(2, null, false);
            ImGui.Text("Position"); ImGui.NextColumn(); ImGui.Text($"({_pos.X:F1}, {_pos.Y:F1})"); ImGui.NextColumn();
            ImGui.Text("Mode"); ImGui.NextColumn(); ImGui.Text(_following ? $"Following {_followTarget}" : "Free"); ImGui.NextColumn();
            ImGui.Columns(1);
        }

        // ───────────────────────── controls ─────────────────────────
        private void DrawControls()
        {
            if (!ImGui.CollapsingHeader("Move / Follow", ImGuiTreeNodeFlags.DefaultOpen))
                return;

            float step = TileSize;
            if (RenderIconButton(ArrowUp, _primary, "Up")) Move(0, -step);
            ImGui.SameLine();
            if (RenderIconButton(ArrowDown, _primary, "Down")) Move(0, step);
            ImGui.SameLine();
            if (RenderIconButton(ArrowLeft, _primary, "Left")) Move(-step, 0);
            ImGui.SameLine();
            if (RenderIconButton(ArrowRight, _primary, "Right")) Move(step, 0);

            if (RenderIconButton(User, _success, "Follow Player")) FollowPlayer();
            ImGui.SameLine();
            if (RenderIconButton(Stop, _danger, "Stop Follow")) ClearFollow();
        }

        private void DrawPreciseInput()
        {
            if (!ImGui.CollapsingHeader("Set Position")) return;

            Vector2 p = _pos;
            ImGui.InputFloat2("##camPos", ref p, "%.1f");
            if (ImGui.Button("Teleport"))
            {
                ClearFollow();
                SetPosition(p);
            }
        }

        // ───────────────────────── helpers ─────────────────────────
        private void RefreshPosition()
        {
            if (_following && _followEnt != null && _followEnt.IsValid)
            {
                uint id = (uint)_followEnt.Id;
                if (InterpolationSystem.TryGetCurrentVisualPosition(id, out float x, out float y))
                {
                    _pos = new Vector2(x, y);
                    return;
                }
                EngineInterop.Engine_GetEntityWorldPosition(
                    Engine.Instance.Context,
                    new EntityId { id = id },
                    out float gx,
                    out float gy);
                _pos = new Vector2(gx, gy);
            }
        }

        private void Move(float dx, float dy)
        {
            ClearFollow();
            SetPosition(new Vector2(_pos.X + dx, _pos.Y + dy));
        }

        private void SetPosition(Vector2 world)
        {
            _pos = world;
            CameraController.MoveTo(world.X, world.Y);
        }

        private void FollowPlayer()
        {
            var player = FindPlayer();
            if (player == null)
            {
                Console.WriteLine("[CameraDebug] Player entity not found.");
                return;
            }
            _following = true;
            _followEnt = player;
            _followTarget = "Player";
            CameraController.Follow(player);
        }

        private void ClearFollow()
        {
            if (!_following) return;
            _following = false;
            _followEnt = null;
            _followTarget = "None";
            CameraController.ClearFollow();
        }

        private static Entity? FindPlayer()
        {
            Entity? res = null;
            World.ForEachEntity(e =>
            {
                if (res == null && e.HasComponent(nameof(PlayerTagComponent))) res = e;
            });
            return res;
        }

        private bool RenderIconButton(string icon, Vector4 col, string tooltip)
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Button, col * 0.8f);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, col);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));
            bool hit = ImGui.Button(icon);
            if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(tooltip)) ImGui.SetTooltip(tooltip);
            ImGui.PopStyleColor(3);
            ImGuiManager.Instance?.PopIconFont();
            return hit;
        }

        public void Dispose() => ClearFollow();
    }

}