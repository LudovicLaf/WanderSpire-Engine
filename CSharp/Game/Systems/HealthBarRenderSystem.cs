// Game/Systems/HealthBarRenderSystem.cs
// — now draws via Render_SubmitCustom at EFFECTS layer instead of UI —
using Game.Dto;
using Game.Events;
using ScriptHost;
using System;
using System.Collections.Generic;
using WanderSpire.Components;
using WanderSpire.Scripting;
using static WanderSpire.Scripting.EngineInterop;

namespace Game.Systems
{
    public sealed class HealthBarRenderSystem : ITickReceiver, IDisposable
    {
        public const float COMBAT_DISPLAY_SECONDS = 5f;
        public const float BAR_WIDTH_FRAC = 0.90f;
        public const float BAR_HEIGHT_FRAC = 0.10f;
        public const float BAR_OFFSET_Y_TILES = -0.60f;

        public const uint COLOUR_FULL = 0xFF3CB043;
        public const uint COLOUR_EMPTY = 0xFFFF3434;
        public const uint COLOUR_BG = 0x80000000;

        private readonly float _tile, _barW, _barH;
        private readonly Dictionary<int, float> _combatTimers = new();

        public HealthBarRenderSystem()
        {
            _tile = Engine.Instance!.TileSize;
            _barW = _tile * BAR_WIDTH_FRAC;
            _barH = _tile * BAR_HEIGHT_FRAC;

            GameEventBus.Event<HurtEvent>.Subscribe(OnHurt);
            GameEventBus.Event<DeathEvent>.Subscribe(OnDeath);
            EventBus.FrameRender += _ => RenderBars();
        }

        public void OnTick(float dt)
        {
            var expired = new List<int>();
            foreach (var kv in _combatTimers)
            {
                _combatTimers[kv.Key] = kv.Value - dt;
                if (_combatTimers[kv.Key] <= 0f)
                    expired.Add(kv.Key);
            }
            foreach (var id in expired)
                _combatTimers.Remove(id);
        }

        private void RenderBars()
        {
            var ctx = Engine.Instance.Context;

            World.ForEachEntity(entity =>
            {
                if (!_combatTimers.ContainsKey(entity.Id))
                    return;

                var stats = entity.GetScriptData<StatsComponent>(nameof(StatsComponent))
                         ?? entity.GetComponent<StatsComponent>(nameof(StatsComponent));
                if (stats == null || stats.MaxHitpoints <= 0) return;

                if (!TryGetWorldPosition(entity, out var worldX, out var worldY))
                    return;

                worldY += _tile * BAR_OFFSET_Y_TILES;

                // Background
                var (br, bg, bb) = ToFloatRGB(COLOUR_BG);
                SubmitColoredRect(
                    ctx,
                    worldX,
                    worldY,
                    _barW,
                    _barH,
                    br, bg, bb,
                    RenderLayer.Entities,
                    order: 0);

                // Foreground
                float pct = Math.Clamp((float)stats.CurrentHitpoints / stats.MaxHitpoints, 0f, 1f);
                float fgW = _barW * pct;
                uint col = LerpColour(COLOUR_EMPTY, COLOUR_FULL, pct);
                var (fr, fgc, fb) = ToFloatRGB(col);
                float fgX = worldX - (_barW - fgW) * 0.5f;

                SubmitColoredRect(
                    ctx,
                    fgX,
                    worldY,
                    fgW,
                    _barH,
                    fr, fgc, fb,
                    RenderLayer.Entities,
                    order: 1);
            });
        }

        public void Dispose()
        {
            GameEventBus.Event<HurtEvent>.Unsubscribe(OnHurt);
            GameEventBus.Event<DeathEvent>.Unsubscribe(OnDeath);
            EventBus.FrameRender -= _ => RenderBars();
        }

        private static bool TryGetWorldPosition(Entity entity, out float worldX, out float worldY)
        {
            worldX = worldY = 0f;
            var t = entity.GetComponent<TransformComponent>(nameof(TransformComponent));
            if (t?.LocalPosition != null && t.LocalPosition.Length >= 2)
            {
                worldX = t.LocalPosition[0];
                worldY = t.LocalPosition[1];
                return true;
            }
            var gp = entity.GetComponent<GridPositionComponent>(nameof(GridPositionComponent));
            if (gp != null)
            {
                var (x, y) = gp.AsTuple();
                float ts = Engine.Instance!.TileSize;
                worldX = x * ts + ts * 0.5f;
                worldY = y * ts + ts * 0.5f;
                return true;
            }
            return false;
        }

        private void OnHurt(HurtEvent ev)
        {
            if (ev.Damage > 0)
                _combatTimers[(int)ev.EntityId] = COMBAT_DISPLAY_SECONDS;
        }

        private void OnDeath(DeathEvent ev)
        {
            _combatTimers.Remove((int)ev.EntityId);
        }

        private static (float r, float g, float b) ToFloatRGB(uint colour)
        {
            byte r = (byte)((colour >> 16) & 0xFF);
            byte g = (byte)((colour >> 8) & 0xFF);
            byte b = (byte)(colour & 0xFF);
            return (r / 255f, g / 255f, b / 255f);
        }

        private static uint LerpColour(uint from, uint to, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            byte r0 = (byte)(from >> 16), g0 = (byte)(from >> 8), b0 = (byte)from;
            byte r1 = (byte)(to >> 16), g1 = (byte)(to >> 8), b1 = (byte)to;
            byte r = (byte)(r0 + (r1 - r0) * t);
            byte g = (byte)(g0 + (g1 - g0) * t);
            byte b = (byte)(b0 + (b1 - b0) * t);
            return (uint)(0xFF << 24 | r << 16 | g << 8 | b);
        }
    }
}
