using System;
using System.Collections.Generic;
using System.Numerics;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Professional systems debug window for monitoring and controlling game systems.
    /// </summary>
    public class SystemsDebugWindow : ImGuiWindowBase
    {
        public override string Title => "Systems Debug";

        // Theme colors
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorDanger = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);

        // System data simulation
        private readonly Dictionary<string, SystemInfo> _systems = new()
        {
            { "MovementSystem", new SystemInfo { Enabled = true, TickTime = 1.2f, LastTick = DateTime.UtcNow, Priority = 1 } },
            { "RenderSystem", new SystemInfo { Enabled = true, TickTime = 8.5f, LastTick = DateTime.UtcNow, Priority = 0 } },
            { "PhysicsSystem", new SystemInfo { Enabled = true, TickTime = 2.1f, LastTick = DateTime.UtcNow, Priority = 2 } },
            { "AISystem", new SystemInfo { Enabled = true, TickTime = 4.3f, LastTick = DateTime.UtcNow, Priority = 3 } },
            { "AudioSystem", new SystemInfo { Enabled = true, TickTime = 0.8f, LastTick = DateTime.UtcNow, Priority = 4 } },
            { "InputSystem", new SystemInfo { Enabled = true, TickTime = 0.3f, LastTick = DateTime.UtcNow, Priority = -1 } },
            { "ScriptEngine", new SystemInfo { Enabled = true, TickTime = 3.2f, LastTick = DateTime.UtcNow, Priority = 5 } },
            { "DebugUISystem", new SystemInfo { Enabled = true, TickTime = 1.8f, LastTick = DateTime.UtcNow, Priority = 10 } },
        };

        private string _searchFilter = "";
        private bool _showDisabledSystems = true;

        public override void Render()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16, 16));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 6));

            if (!BeginWindow())
            {
                ImGui.PopStyleVar(3);
                EndWindow();
                return;
            }

            RenderHeader();
            ImGui.Separator();

            RenderControls();
            ImGui.Separator();

            RenderSystemsList();

            ImGui.PopStyleVar(3);
            EndWindow();
        }

        private void RenderHeader()
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(Cogs);
            ImGui.PopStyleColor();
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text("Systems Monitor");
            ImGui.PopStyleColor();

            var enabledCount = 0;
            var totalTickTime = 0f;
            foreach (var system in _systems.Values)
            {
                if (system.Enabled)
                {
                    enabledCount++;
                    totalTickTime += system.TickTime;
                }
            }

            ImGui.SameLine();
            ImGui.TextColored(ColorDim, $"({enabledCount}/{_systems.Count} active)");

            ImGui.SameLine(ImGui.GetWindowWidth() - 150);
            ImGui.TextColored(ColorInfo, $"Total: {totalTickTime:F1}ms");
        }

        private void RenderControls()
        {
            // Search filter
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0, 0, 0.3f));
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("##search", ref _searchFilter, 128);
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Search systems by name");

            ImGui.SameLine();
            ImGui.Checkbox("Show Disabled", ref _showDisabledSystems);

            ImGui.SameLine();
            if (RenderIconButton(Play, ColorSuccess, "Enable All Systems"))
            {
                foreach (var system in _systems.Values)
                    system.Enabled = true;
                Console.WriteLine("[SystemsDebug] Enabled all systems");
            }

            ImGui.SameLine();
            if (RenderIconButton(Pause, ColorWarning, "Disable All Systems"))
            {
                foreach (var system in _systems.Values)
                    system.Enabled = false;
                Console.WriteLine("[SystemsDebug] Disabled all systems");
            }

            ImGui.SameLine();
            if (RenderIconButton(Sync, ColorPrimary, "Reset Performance Timers"))
            {
                foreach (var system in _systems.Values)
                {
                    system.TickTime = 0f;
                    system.LastTick = DateTime.UtcNow;
                }
                Console.WriteLine("[SystemsDebug] Reset performance timers");
            }
        }

        private void RenderSystemsList()
        {
            ImGui.BeginChild("##systems_list", new Vector2(-1, -1), true);

            // Table headers
            ImGui.Columns(5, "##systems_table", true);
            ImGui.SetColumnWidth(0, 40);   // Status
            ImGui.SetColumnWidth(1, 200);  // Name
            ImGui.SetColumnWidth(2, 80);   // Priority
            ImGui.SetColumnWidth(3, 100);  // Tick Time
            ImGui.SetColumnWidth(4, 120);  // Last Tick

            // Headers
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(""); // Status column (just icon)
            ImGui.NextColumn();
            ImGui.Text("System Name");
            ImGui.NextColumn();
            ImGui.Text("Priority");
            ImGui.NextColumn();
            ImGui.Text("Tick Time");
            ImGui.NextColumn();
            ImGui.Text("Last Tick");
            ImGui.NextColumn();
            ImGui.PopStyleColor();

            ImGui.Separator();

            // Filter and sort systems
            var filteredSystems = new List<KeyValuePair<string, SystemInfo>>();
            foreach (var kvp in _systems)
            {
                if (!_showDisabledSystems && !kvp.Value.Enabled)
                    continue;

                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !kvp.Key.ToLower().Contains(_searchFilter.ToLower()))
                    continue;

                filteredSystems.Add(kvp);
            }

            // Sort by priority
            filteredSystems.Sort((a, b) => a.Value.Priority.CompareTo(b.Value.Priority));

            // Render systems
            foreach (var (name, info) in filteredSystems)
            {
                RenderSystemRow(name, info);
            }

            ImGui.Columns(1);
            ImGui.EndChild();
        }

        private void RenderSystemRow(string name, SystemInfo info)
        {
            ImGui.PushID(name);

            // Status column
            ImGuiManager.Instance?.PushIconFont();
            if (info.Enabled)
            {
                ImGui.TextColored(ColorSuccess, CheckCircle);
            }
            else
            {
                ImGui.TextColored(ColorDanger, TimesCircle);
            }
            ImGuiManager.Instance?.PopIconFont();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(info.Enabled ? "System is enabled" : "System is disabled");

            ImGui.NextColumn();

            // Name column with system icon
            ImGuiManager.Instance?.PushIconFont();
            ImGui.Text(GetSystemIcon(name));
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();

            if (info.Enabled)
                ImGui.Text(name);
            else
                ImGui.TextColored(ColorDim, name);

            // Context menu
            if (ImGui.BeginPopupContextItem($"##ctx_{name}"))
            {
                if (ImGui.MenuItem(info.Enabled ? "Disable" : "Enable"))
                {
                    info.Enabled = !info.Enabled;
                    Console.WriteLine($"[SystemsDebug] {(info.Enabled ? "Enabled" : "Disabled")} {name}");
                }
                ImGui.Separator();
                if (ImGui.MenuItem("Reset Timers"))
                {
                    info.TickTime = 0f;
                    info.LastTick = DateTime.UtcNow;
                    Console.WriteLine($"[SystemsDebug] Reset timers for {name}");
                }
                if (ImGui.MenuItem("Copy Name"))
                {
                    Console.WriteLine($"System name: {name}");
                }
                ImGui.EndPopup();
            }

            ImGui.NextColumn();

            // Priority column
            ImGui.Text(info.Priority.ToString());
            ImGui.NextColumn();

            // Tick Time column
            Vector4 timeColor = info.TickTime < 2.0f ? ColorSuccess :
                               (info.TickTime < 5.0f ? ColorWarning : ColorDanger);
            ImGui.TextColored(timeColor, $"{info.TickTime:F2}ms");
            ImGui.NextColumn();

            // Last Tick column
            var timeSinceLastTick = DateTime.UtcNow - info.LastTick;
            string lastTickText = timeSinceLastTick.TotalSeconds < 1 ? "Just now" :
                                 timeSinceLastTick.TotalSeconds < 60 ? $"{timeSinceLastTick.TotalSeconds:F0}s ago" :
                                 $"{timeSinceLastTick.TotalMinutes:F0}m ago";

            Vector4 lastTickColor = timeSinceLastTick.TotalSeconds < 1 ? ColorSuccess :
                                   (timeSinceLastTick.TotalSeconds < 10 ? ColorInfo : ColorDim);
            ImGui.TextColored(lastTickColor, lastTickText);
            ImGui.NextColumn();

            ImGui.PopID();
        }

        private string GetSystemIcon(string systemName)
        {
            return systemName.ToLower() switch
            {
                "movementsystem" => Running,
                "rendersystem" => PaintBrush,
                "physicssystem" => Atom,
                "aisystem" => Robot,
                "audiosystem" => VolumeUp,
                "inputsystem" => Keyboard,
                "scriptengine" => Code,
                "debuguisystem" => Bug,
                _ => Cog
            };
        }

        private bool RenderIconButton(string icon, Vector4 color, string tooltip = null)
        {
            ImGuiManager.Instance?.PushIconFont();

            ImGui.PushStyleColor(ImGuiCol.Button, color * new Vector4(1, 1, 1, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));

            Vector2 textSize = ImGui.CalcTextSize(icon);
            Vector2 btnSize = new Vector2(textSize.X + 8, textSize.Y + 4);

            bool clicked = ImGui.Button(icon, btnSize);

            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            ImGui.PopStyleColor(3);
            ImGuiManager.Instance?.PopIconFont();

            return clicked;
        }

        private class SystemInfo
        {
            public bool Enabled { get; set; }
            public float TickTime { get; set; }
            public DateTime LastTick { get; set; }
            public int Priority { get; set; }
        }
    }
}