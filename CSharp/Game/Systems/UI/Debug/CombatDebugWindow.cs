using Game.Dto;
using Game.Events;
using ScriptHost;
using System;
using System.Collections.Generic;
using System.Numerics;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Sleek, icon‑driven combat monitor with real‑time statistics and event log.
    /// </summary>
    public sealed class CombatDebugWindow : ImGuiWindowBase, IDisposable
    {
        public override string Title => "Combat Monitor";

        // ───────────────────────── theme ─────────────────────────
        private readonly Vector4 _primary = new(0.22f, 0.58f, 0.93f, 1f);
        private readonly Vector4 _success = new(0.22f, 0.80f, 0.46f, 1f);
        private readonly Vector4 _danger = new(0.93f, 0.23f, 0.29f, 1f);
        private readonly Vector4 _warning = new(0.97f, 0.76f, 0.35f, 1f);
        private readonly Vector4 _dim = new(0.55f, 0.55f, 0.58f, 1f);

        // ───────────────────────── state ─────────────────────────
        private readonly List<LogEntry> _log = new();
        private readonly object _lock = new();
        private const int _maxLog = 200;

        private int _alive;
        private int _dead;
        private int _totalDamage;

        private bool _autoScroll = true;
        private bool _showStats = true;
        private bool _showLog = true;
        private bool _freeze = false;

        public CombatDebugWindow()
        {
            GameEventBus.Event<AttackEvent>.Subscribe(OnAttack);
            GameEventBus.Event<HurtEvent>.Subscribe(OnHurt);
            GameEventBus.Event<DeathEvent>.Subscribe(OnDeath);
        }

        public override void Render()
        {
            if (!_freeze) UpdateStats();

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(14, 12));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 6));

            if (!BeginWindow(ImGuiWindowFlags.MenuBar))
            {
                ImGui.PopStyleVar(2);
                EndWindow();
                return;
            }

            RenderMenuBar();

            if (_showStats)
            {
                RenderStats();
                ImGui.Separator();
            }

            if (_showLog)
            {
                RenderLog();
            }

            ImGui.PopStyleVar(2);
            EndWindow();
        }

        // ───────────────────────── menu bar ─────────────────────────
        private void RenderMenuBar()
        {
            if (!ImGui.BeginMenuBar()) return;

            ImGuiManager.Instance?.PushIconFont();

            string statsLabel = $"{Sitemap} Stats";
            string logLabel = $"{ClipboardList} Log";
            string freezeLabel = $"{Snowflake} Freeze";
            string clearLabel = $"{Trash} Clear";

            if (ImGui.MenuItem(statsLabel, null, _showStats))
                _showStats = !_showStats;
            if (ImGui.MenuItem(logLabel, null, _showLog))
                _showLog = !_showLog;
            if (ImGui.MenuItem(freezeLabel, null, _freeze))
                _freeze = !_freeze;
            if (ImGui.MenuItem(clearLabel))
                ClearLog();

            ImGuiManager.Instance?.PopIconFont();
            ImGui.EndMenuBar();
        }

        // ───────────────────────── statistics ─────────────────────────
        private void UpdateStats()
        {
            _alive = 0;
            _dead = 0;
            World.ForEachEntity(e =>
            {
                var st = e.GetScriptData<StatsComponent>(nameof(StatsComponent));
                if (st == null) return;
                if (st.CurrentHitpoints > 0) _alive++; else _dead++;
            });
        }

        private void RenderStats()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, _primary * new Vector4(1, 1, 1, 0.15f));
            if (ImGui.CollapsingHeader("Summary " + ChartBar, ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();
                ImGui.Columns(2, "##combat_stats", false);
                ImGui.SetColumnWidth(0, 120);

                StatRow("Alive", _alive, _success);
                StatRow("Dead", _dead, _danger);
                StatRow("Damage", _totalDamage, _warning);

                ImGui.Columns(1);
            }
            else ImGui.PopStyleColor();
        }

        private void StatRow(string label, int value, Vector4 color)
        {
            ImGuiManager.Instance?.PushIconFont();
            string icon = label switch
            {
                "Alive" => Heart,
                "Dead" => Skull,
                _ => Fire,
            };
            ImGui.TextColored(color, icon);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.Text(label);
            ImGui.NextColumn();
            ImGui.TextColored(color, value.ToString());
            ImGui.NextColumn();
        }

        // ───────────────────────── log ─────────────────────────
        private void RenderLog()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, _warning * new Vector4(1, 1, 1, 0.15f));
            if (ImGui.CollapsingHeader("Event Log " + Scroll, ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                Vector2 size = new(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - 4);
                if (ImGui.BeginChild("##combat_log", size, true, ImGuiWindowFlags.HorizontalScrollbar))
                {
                    lock (_lock)
                    {
                        foreach (var e in _log)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, e.Color);
                            ImGui.TextUnformatted(e.Message);
                            ImGui.PopStyleColor();
                        }
                        if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4)
                            ImGui.SetScrollHereY(1f);
                    }
                    ImGui.EndChild();
                }
            }
            else ImGui.PopStyleColor();
        }

        private void AddLog(string label, string icon, Vector4 color)
        {
            string ts = DateTime.Now.ToString("HH:mm:ss.fff");
            string msg = $"[{ts}] {icon} {label}";
            lock (_lock)
            {
                _log.Add(new LogEntry { Message = msg, Color = color });
                if (_log.Count > _maxLog) _log.RemoveAt(0);
            }
        }

        private void ClearLog()
        {
            lock (_lock) _log.Clear();
            _totalDamage = 0;
        }

        // ───────────────────────── event hooks ─────────────────────────
        private void OnAttack(AttackEvent ev) => AddLog($"Attack {ev.AttackerId} → {ev.VictimId}", FistRaised, _primary);

        private void OnHurt(HurtEvent ev)
        {
            if (ev.Damage <= 0) return;
            _totalDamage += ev.Damage;
            AddLog($"Hurt  {ev.EntityId} −{ev.Damage} HP", FireAlt, _warning);
        }

        private void OnDeath(DeathEvent ev) => AddLog($"Death {ev.EntityId}", SkullCrossbones, _danger);

        public void Dispose()
        {
            GameEventBus.Event<AttackEvent>.Unsubscribe(OnAttack);
            GameEventBus.Event<HurtEvent>.Unsubscribe(OnHurt);
            GameEventBus.Event<DeathEvent>.Unsubscribe(OnDeath);
        }

        private sealed class LogEntry
        {
            public string Message { get; init; } = string.Empty;
            public Vector4 Color { get; init; }
        }
    }
}
