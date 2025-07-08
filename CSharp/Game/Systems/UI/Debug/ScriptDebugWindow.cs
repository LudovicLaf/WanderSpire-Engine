// File: Game/Systems/UI/ScriptDebugWindow.cs
using ScriptHost;
using System;
using WanderSpire.Scripting.UI;

namespace Game.Systems.UI
{
    /// <summary>
    /// Script debug window for monitoring managed scripts and behaviours.
    /// </summary>
    public class ScriptDebugWindow : ImGuiWindowBase
    {
        public override string Title => "Script Debug";

        public override void Render()
        {
            if (!BeginWindow())
            {
                EndWindow();
                return;
            }

            var engine = ScriptEngine.Current;
            if (engine != null)
            {
                ImGui.Text("Script Engine Active: Yes");
                ImGui.Text($"Quests: {engine.Quests.Count}");
                ImGui.Text($"Encounters: {engine.Encounters.Count}");

                if (ImGui.CollapsingHeader("Active Quests"))
                    foreach (var q in engine.Quests)
                        ImGui.Text($"• {q.Id}: {q.Title}");

                if (ImGui.CollapsingHeader("Available Encounters"))
                    foreach (var c in engine.Encounters)
                        ImGui.Text($"• {c.Id}");
            }
            else
            {
                ImGui.Text("Script Engine: Not Active");
            }

            ImGui.Separator();

            int behaviourCount = 0;
            World.ForEachEntity(ent =>
            {
                var scripts = ent.GetScriptData<string[]>("scripts");
                if (scripts?.Length > 0)
                    behaviourCount += scripts.Length;
            });

            ImGui.Text($"Active Behaviours: {behaviourCount}");

            if (ImGui.Button("Hot Reload Scripts"))
            {
                Console.WriteLine("[ScriptDebug] Hot reload requested - use F9");
            }

            EndWindow();
        }
    }
}
