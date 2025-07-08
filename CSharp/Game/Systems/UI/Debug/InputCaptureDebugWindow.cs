// Game/Systems/UI/InputCaptureDebugWindow.cs - SIMPLIFIED
using System;
using WanderSpire.Scripting;
using WanderSpire.Scripting.UI;

namespace Game.Systems.UI
{
    public class InputCaptureDebugWindow : IImGuiWindow, IDisposable
    {
        public string Title => "Input Capture Debug";
        public bool IsVisible { get; set; } = false;

        private float testSlider = 50.0f;
        private string testText = "Type here...";

        public void Render()
        {
            if (!IsVisible) return;

            bool isVisible = IsVisible;
            if (ImGui.Begin(Title, ref isVisible))
            {
                IsVisible = isVisible;

                var debugSystem = DebugUISystem.Instance;
                var imguiManager = ImGuiManager.Instance;

                // Current state
                ImGui.Text("=== ImGui Input Capture ===");

                if (imguiManager != null)
                {
                    bool wantsMouse = imguiManager.WantCaptureMouse;
                    bool wantsKeyboard = imguiManager.WantCaptureKeyboard;

                    ImGui.Text($"WantCaptureMouse: {wantsMouse}");
                    ImGui.Text($"WantCaptureKeyboard: {wantsKeyboard}");

                    bool wantsInput = debugSystem?.WantsInput() == true;
                    ImGui.Text($"WantsInput(): {wantsInput}");

                    if (wantsInput)
                    {
                        ImGui.SameLine();
                        ImGui.Text("*** BLOCKING GAME ***");
                    }
                }
                else
                {
                    ImGui.Text("ImGuiManager: NULL");
                }

                ImGui.Separator();

                // Test area
                ImGui.Text("=== Test Area ===");
                ImGui.Text("Type/click here to test capture:");

                if (ImGui.Button("Test Button"))
                {
                    Console.WriteLine("[InputCaptureDebug] Button clicked!");
                }

                ImGui.InputText("Test Input", ref testText, 256);
                ImGui.SliderFloat("Test Slider", ref testSlider, 0.0f, 100.0f);

                if (ImGui.Button("Log State"))
                {
                    LogState();
                }
            }
            else
            {
                IsVisible = isVisible;
            }
            ImGui.End();
        }

        private void LogState()
        {
            var mgr = ImGuiManager.Instance;
            var debug = DebugUISystem.Instance;

            Console.WriteLine("=== Input Capture State ===");
            Console.WriteLine($"WantCaptureMouse: {mgr?.WantCaptureMouse}");
            Console.WriteLine($"WantCaptureKeyboard: {mgr?.WantCaptureKeyboard}");
            Console.WriteLine($"WantsInput(): {debug?.WantsInput()}");
            Console.WriteLine($"Mouse: ({Input.MouseX}, {Input.MouseY})");
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}