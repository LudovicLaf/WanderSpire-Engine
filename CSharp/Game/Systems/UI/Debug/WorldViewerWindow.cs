// File: Game/Systems/UI/WorldViewerWindow.cs
using ScriptHost;
using System.Collections.Generic;
using System.Linq;
using WanderSpire.Components;
using WanderSpire.Scripting.UI;

namespace Game.Systems.UI
{
    /// <summary>
    /// World viewer window showing grid and entity information.
    /// </summary>
    public class WorldViewerWindow : ImGuiWindowBase
    {
        public override string Title => "World Viewer";

        private bool _showGrid = true;
        private bool _showEntities = true;
        private bool _showPaths = false;
        private int _viewRadius = 10;

        public override void Render()
        {
            if (!BeginWindow())
            {
                EndWindow();
                return;
            }

            ImGui.Checkbox("Show Grid", ref _showGrid);
            ImGui.Checkbox("Show Entities", ref _showEntities);
            ImGui.Checkbox("Show Paths", ref _showPaths);
            ImGui.SliderInt("View Radius", ref _viewRadius, 5, 50);

            ImGui.Separator();

            var entityStats = new Dictionary<string, int>();
            int totalEntities = 0;

            World.ForEachEntity(entity =>
            {
                totalEntities++;
                if (entity.HasComponent(nameof(PrefabIdComponent)))
                {
                    try
                    {
                        var prefab = entity.GetComponent<PrefabIdComponent>(nameof(PrefabIdComponent));
                        if (prefab != null)
                        {
                            string name = prefab.PrefabName ?? "Unknown";
                            entityStats[name] = entityStats.GetValueOrDefault(name, 0) + 1;
                        }
                    }
                    catch { }
                }
            });

            ImGui.Text($"Total Entities: {totalEntities}");

            if (ImGui.CollapsingHeader("Entity Types"))
            {
                foreach (var kvp in entityStats.OrderByDescending(x => x.Value))
                {
                    ImGui.Text($"  {kvp.Key}: {kvp.Value}");
                }
            }

            ImGui.Separator();

            ImGui.Text("Camera Information:");
            ImGui.Text("  Position: N/A");
            ImGui.Text("  Zoom: N/A");

            EndWindow();
        }
    }
}
