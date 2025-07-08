// File: Game/Systems/UI/EngineDebugWindow.cs
using ScriptHost;
using System;
using System.Linq;
using WanderSpire.Scripting;
using WanderSpire.Scripting.UI;

namespace Game.Systems.UI
{
    /// <summary>
    /// Combined header + engine debug window.
    /// </summary>
    public class EngineDebugWindow : ImGuiWindowBase, IDisposable
    {
        public override string Title => "Engine Debug";

        // header state
        private bool _showToolsList = false;

        // test controls
        private float _testFloat = 1.0f;
        private int _testInt = 42;
        private bool _testBool = false;
        private string _testString = "Hello ImGui!";

        public override void Render()
        {
            // Enable the ImGui menu‐bar area
            if (!BeginWindow(ImGuiWindowFlags.MenuBar))
            {
                EndWindow();
                return;
            }

            // ── HEADER ROW ───────────────────────────────────────────────────
            ImGui.Text("Debug UI");
            ImGui.SameLine();
            if (ImGui.Button("Files")) { /* TODO: file menu */ }
            ImGui.SameLine();
            if (ImGui.Button("Tools"))
                _showToolsList = !_showToolsList;
            ImGui.SameLine();
            if (ImGui.Button("Pause")) { /* TODO: pause game */ }
            ImGui.SameLine();
            if (ImGui.Button("Play")) { /* TODO: resume game */ }

            // ── TOOLS LIST ──────────────────────────────────────────────────
            if (_showToolsList)
            {
                ImGui.Spacing();
                ImGui.Text("Toggle Windows:");
                var windows = ImGuiManager.Instance?
                                  .GetRegisteredWindows()
                                  .Where(w => w != this)
                                  .ToArray()
                              ?? Array.Empty<IImGuiWindow>();

                foreach (var w in windows)
                {
                    bool vis = w.IsVisible;
                    if (ImGui.Checkbox(w.Title, ref vis))
                        w.IsVisible = vis;
                }

                ImGui.Separator();
            }
            else
            {
                ImGui.Separator();
            }

            // ── ENGINE INFO ────────────────────────────────────────────────
            var engine = Engine.Instance;
            if (engine != null)
            {
                ImGui.Text($"Engine Tick:     {engine.TickCount}");
                ImGui.Text($"Tick Interval:   {engine.TickInterval:F4} s");
                ImGui.Text($"Tile Size:       {engine.TileSize} px");
                ImGui.Separator();
            }

            // ── TEST CONTROLS ───────────────────────────────────────────────
            ImGui.Text("Test Controls:");
            ImGui.InputFloat("Test Float", ref _testFloat);
            ImGui.InputInt("Test Int", ref _testInt);
            ImGui.Checkbox("Test Bool", ref _testBool);
            ImGui.InputText("Test String", ref _testString);

            if (ImGui.Button("Run Test"))
            {
                Console.WriteLine(
                  $"[EngineDebug] Float={_testFloat}, Int={_testInt}, Bool={_testBool}, Str='{_testString}'");
            }

            ImGui.Separator();

            // ── WORLD STATS ───────────────────────────────────────────────
            int entityCount = 0;
            World.ForEachEntity(_ => entityCount++);
            ImGui.Text($"Total Entities:  {entityCount}");

            long mem = GC.GetTotalMemory(false);
            ImGui.Text($"Managed Memory: {mem / 1024.0 / 1024.0:F2} MB");

            EndWindow();
        }

        public void Dispose()
        {
            // nothing to dispose here
        }
    }
}
