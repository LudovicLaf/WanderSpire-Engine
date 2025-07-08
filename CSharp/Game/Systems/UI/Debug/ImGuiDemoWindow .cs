// File: Game/Systems/UI/NativeImGuiDemoWindow.cs
using System;
using System.Numerics;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Wrapper for the native ImGui demo window that comes with the library.
    /// This shows all ImGui widgets, features, and serves as comprehensive documentation.
    /// </summary>
    public class ImGuiDemoWindow : ImGuiWindowBase, IDisposable
    {
        public override string Title => $"{Magic} ImGui Demo (Native)";

        // Theme colors for the wrapper
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorDanger = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);

        private bool _showDemoWindow = true;
        private bool _showMetricsWindow = false;
        private bool _showDebugLogWindow = false;
        private bool _showStackToolWindow = false;
        private bool _showAboutWindow = false;
        private bool _showStyleEditor = false;
        private bool _showUserGuide = false;

        public override void Render()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16, 16));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6.0f);

            if (!BeginWindow())
            {
                ImGui.PopStyleVar(2);
                EndWindow();
                return;
            }

            RenderControlPanel();

            ImGui.PopStyleVar(2);
            EndWindow();

            // Render the actual native ImGui windows
            RenderNativeWindows();
        }

        private void RenderControlPanel()
        {
            RenderHeader();
            ImGui.Separator();

            RenderWindowToggles();
            ImGui.Separator();

            RenderInformation();
        }

        private void RenderHeader()
        {
            RenderIcon(Magic, ColorPrimary);
            ImGui.SameLine();
            ImGui.TextColored(ColorPrimary, "Native ImGui Demo & Diagnostics");

            ImGui.SameLine(ImGui.GetWindowWidth() - 150);

            if (RenderIconButton(QuestionCircle, ColorInfo, "About ImGui"))
            {
                _showAboutWindow = !_showAboutWindow;
            }

            ImGui.SameLine();
            if (RenderIconButton(InfoCircle, ColorSuccess, "Show user guide"))
            {
                _showUserGuide = !_showUserGuide;
            }

            ImGui.TextColored(ColorDim, "Access to native ImGui demo and diagnostic windows");
        }

        private void RenderWindowToggles()
        {
            RenderSectionHeader(WindowMaximize, "Available Windows", ColorSuccess);

            // Main demo window
            if (ImGui.Checkbox("Demo Window", ref _showDemoWindow))
            {
                Console.WriteLine($"[NativeImGuiDemo] Demo window {(_showDemoWindow ? "enabled" : "disabled")}");
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("The main ImGui demo showcasing all widgets and features");

            // Metrics/Debugger window
            if (ImGui.Checkbox("Metrics/Debugger", ref _showMetricsWindow))
            {
                Console.WriteLine($"[NativeImGuiDemo] Metrics window {(_showMetricsWindow ? "enabled" : "disabled")}");
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Internal ImGui metrics, memory usage, and performance data");

            // Debug log window
            if (ImGui.Checkbox("Debug Log", ref _showDebugLogWindow))
            {
                Console.WriteLine($"[NativeImGuiDemo] Debug log {(_showDebugLogWindow ? "enabled" : "disabled")}");
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("ImGui's internal debug log and error messages");

            // Stack tool window
            if (ImGui.Checkbox("ID Stack Tool", ref _showStackToolWindow))
            {
                Console.WriteLine($"[NativeImGuiDemo] Stack tool {(_showStackToolWindow ? "enabled" : "disabled")}");
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Tool for debugging ImGui ID stack issues");

            // Style editor
            if (ImGui.Checkbox("Style Editor", ref _showStyleEditor))
            {
                Console.WriteLine($"[NativeImGuiDemo] Style editor {(_showStyleEditor ? "enabled" : "disabled")}");
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Live editor for ImGui's visual style and colors");

            // About window
            if (ImGui.Checkbox("About ImGui", ref _showAboutWindow))
            {
                Console.WriteLine($"[NativeImGuiDemo] About window {(_showAboutWindow ? "enabled" : "disabled")}");
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Information about ImGui version and credits");

            // User guide
            if (ImGui.Checkbox("User Guide", ref _showUserGuide))
            {
                Console.WriteLine($"[NativeImGuiDemo] User guide {(_showUserGuide ? "enabled" : "disabled")}");
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Basic ImGui usage guide and keyboard shortcuts");

            ImGui.Spacing();

            // Quick actions
            if (RenderStyledButton("Show All", ColorSuccess))
            {
                _showDemoWindow = true;
                _showMetricsWindow = true;
                _showDebugLogWindow = true;
                _showStackToolWindow = true;
                _showStyleEditor = true;
                _showAboutWindow = true;
                _showUserGuide = true;
            }

            ImGui.SameLine();
            if (RenderStyledButton("Hide All", ColorDanger))
            {
                _showDemoWindow = false;
                _showMetricsWindow = false;
                _showDebugLogWindow = false;
                _showStackToolWindow = false;
                _showStyleEditor = false;
                _showAboutWindow = false;
                _showUserGuide = false;
            }
        }

        private void RenderInformation()
        {
            RenderSectionHeader(InfoCircle, "Information", ColorInfo);

            ImGui.TextColored(ColorDim, "The native ImGui demo window showcases:");
            ImGui.BulletText("All available widgets and their usage");
            ImGui.BulletText("Advanced layout and styling techniques");
            ImGui.BulletText("Input handling and interaction patterns");
            ImGui.BulletText("Performance tips and best practices");

            ImGui.Spacing();
            ImGui.TextColored(ColorWarning, "Note:");
            ImGui.TextColored(ColorDim, "These are the original ImGui windows written in C++");
            ImGui.TextColored(ColorDim, "and provide the most comprehensive widget documentation.");

            ImGui.Spacing();

            // Current ImGui info
            RenderSectionHeader(Microchip, "Current ImGui Context", ColorWarning);

            var io = ImGui.GetIO();
            ImGui.Columns(2, "##imgui_info", false);
            ImGui.SetColumnWidth(0, 150);
            ImGui.Text("ImGui Status:");
            ImGui.NextColumn();
            ImGui.TextColored(ColorSuccess, "Active");
            ImGui.NextColumn();
            ImGui.Text("Want Capture:");
            ImGui.NextColumn();
            var manager = ImGuiManager.Instance;
            var captureText = $"Mouse: {(manager?.WantCaptureMouse == true ? "Yes" : "No")}, Keyboard: {(manager?.WantCaptureKeyboard == true ? "Yes" : "No")}";
            ImGui.TextColored(ColorDim, captureText);
            ImGui.NextColumn();
            ImGui.Text("Windows:");
            ImGui.NextColumn();
            var stats = manager?.GetStats();
            ImGui.TextColored(ColorInfo, $"{stats?.VisibleWindows ?? 0}/{stats?.TotalWindows ?? 0} visible");
            ImGui.Columns(1);
        }

        private void RenderNativeWindows()
        {
            // This is where we call the actual native ImGui demo functions
            // The exact method depends on your ImGui binding

            if (_showDemoWindow)
            {
                try
                {
                    // Method 1: If your binding exposes ShowDemoWindow directly
                    ImGui.ShowDemoWindow(ref _showDemoWindow);
                }
                catch (Exception ex)
                {
                    // Fallback: If ShowDemoWindow is not available, we'll show a message
                    Console.WriteLine($"[NativeImGuiDemo] ShowDemoWindow not available: {ex.Message}");

                    // Create a fallback window
                    if (ImGui.Begin("ImGui Demo (Fallback)", ref _showDemoWindow))
                    {
                        ImGui.TextColored(ColorWarning, "Native ImGui Demo Not Available");
                        ImGui.Separator();
                        ImGui.TextColored(ColorDim, "The native ImGui.ShowDemoWindow() function is not");
                        ImGui.TextColored(ColorDim, "exposed in your current ImGui binding.");
                        ImGui.Spacing();
                        ImGui.TextColored(ColorInfo, "To enable the native demo window:");
                        ImGui.BulletText("Ensure your ImGui binding exposes ImGui::ShowDemoWindow");
                        ImGui.BulletText("Add the demo source files to your project");
                        ImGui.BulletText("Enable IMGUI_DEMO_WINDOWS in your build");
                        ImGui.Spacing();
                        if (ImGui.Button("Check Console for Details"))
                        {
                            Console.WriteLine("[NativeImGuiDemo] To enable native demo windows:");
                            Console.WriteLine("1. In your C++ ImGui binding, ensure imgui_demo.cpp is compiled");
                            Console.WriteLine("2. Expose ImGui::ShowDemoWindow in your managed wrapper");
                            Console.WriteLine("3. Example: [DllImport] static extern void igShowDemoWindow(ref bool open);");
                        }
                    }
                    ImGui.End();
                }
            }

            if (_showMetricsWindow)
            {
                try
                {
                    ImGui.ShowMetricsWindow(ref _showMetricsWindow);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NativeImGuiDemo] ShowMetricsWindow not available: {ex.Message}");
                    RenderFallbackWindow("Metrics Window", ref _showMetricsWindow, "ImGui.ShowMetricsWindow()");
                }
            }

            if (_showDebugLogWindow)
            {
                try
                {
                    ImGui.ShowDebugLogWindow(ref _showDebugLogWindow);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NativeImGuiDemo] ShowDebugLogWindow not available: {ex.Message}");
                    RenderFallbackWindow("Debug Log", ref _showDebugLogWindow, "ImGui.ShowDebugLogWindow()");
                }
            }

            if (_showStackToolWindow)
            {
                try
                {
                    ImGui.ShowIDStackToolWindow(ref _showStackToolWindow);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NativeImGuiDemo] ShowIDStackToolWindow not available: {ex.Message}");
                    RenderFallbackWindow("ID Stack Tool", ref _showStackToolWindow, "ImGui.ShowIDStackToolWindow()");
                }
            }

            if (_showAboutWindow)
            {
                try
                {
                    ImGui.ShowAboutWindow(ref _showAboutWindow);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NativeImGuiDemo] ShowAboutWindow not available: {ex.Message}");
                    RenderFallbackAboutWindow();
                }
            }

            if (_showStyleEditor)
            {
                try
                {
                    if (ImGui.Begin("Dear ImGui Style Editor", ref _showStyleEditor))
                    {
                        // Try to get the current style pointer
                        try
                        {
                            var style = ImGui.GetStyle();
                            ImGui.ShowStyleEditor(style);
                        }
                        catch
                        {
                            // Fallback if GetStyle() isn't available
                            ImGui.TextColored(ColorWarning, "Style Editor Not Available");
                            ImGui.Separator();
                            ImGui.TextColored(ColorDim, "The style editor requires access to the current ImGui style.");
                            ImGui.TextColored(ColorDim, "This may not be exposed in your current binding.");
                            ImGui.Spacing();
                            ImGui.TextColored(ColorInfo, "The style editor allows you to:");
                            ImGui.BulletText("Modify colors and visual appearance");
                            ImGui.BulletText("Adjust spacing and sizing parameters");
                            ImGui.BulletText("Customize window and widget appearance");
                            ImGui.BulletText("Export/import style configurations");
                        }
                    }
                    ImGui.End();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NativeImGuiDemo] ShowStyleEditor not available: {ex.Message}");
                    RenderFallbackWindow("Style Editor", ref _showStyleEditor, "ImGui.ShowStyleEditor()");
                }
            }

            if (_showUserGuide)
            {
                RenderUserGuideWindow();
            }
        }

        private void RenderFallbackWindow(string title, ref bool isOpen, string functionName)
        {
            if (ImGui.Begin($"{title} (Not Available)", ref isOpen))
            {
                ImGui.TextColored(ColorWarning, $"{title} Not Available");
                ImGui.Separator();
                ImGui.TextColored(ColorDim, $"The {functionName} function is not");
                ImGui.TextColored(ColorDim, "exposed in your current ImGui binding.");
                ImGui.Spacing();
                ImGui.TextColored(ColorInfo, "This window provides advanced ImGui diagnostics");
                ImGui.TextColored(ColorInfo, "and debugging information when available.");
            }
            ImGui.End();
        }

        private void RenderFallbackAboutWindow()
        {
            if (ImGui.Begin("About Dear ImGui", ref _showAboutWindow))
            {
                ImGui.TextColored(ColorPrimary, "Dear ImGui");
                ImGui.TextColored(ColorDim, ImGui.GetVersion());
                ImGui.Separator();

                ImGui.Text("Dear ImGui is a bloat-free graphical user interface library for C++.");
                ImGui.Text("It outputs optimized vertex buffers that you can render anytime in your");
                ImGui.Text("3D-pipeline enabled application. It is fast, portable, renderer agnostic");
                ImGui.Text("and self-contained (no external dependencies).");

                ImGui.Spacing();
                ImGui.Text("By Omar Cornut and all Dear ImGui contributors.");
                ImGui.Text("Dear ImGui is licensed under the MIT License, see LICENSE for more information.");

                ImGui.Spacing();
                if (ImGui.Button("Visit GitHub Repository"))
                {
                    Console.WriteLine("[NativeImGuiDemo] Visit: https://github.com/ocornut/imgui");
                }
            }
            ImGui.End();
        }

        private void RenderUserGuideWindow()
        {
            if (ImGui.Begin("Dear ImGui User Guide", ref _showUserGuide))
            {
                ImGui.TextColored(ColorPrimary, "Dear ImGui User Guide");
                ImGui.Separator();

                if (ImGui.CollapsingHeader("BASIC USAGE"))
                {
                    ImGui.BulletText("Double-click on title bar to collapse window.");
                    ImGui.BulletText("Click and drag on lower corner to resize window\n(double-click to auto fit window to its contents).");
                    ImGui.BulletText("CTRL+Click on a slider or drag box to input value as text.");
                    ImGui.BulletText("TAB/SHIFT+TAB to cycle through keyboard editable fields.");
                    ImGui.BulletText("CTRL+Tab to select a window.");
                }

                if (ImGui.CollapsingHeader("KEYBOARD SHORTCUTS"))
                {
                    ImGui.BulletText("Escape to close popup, focus off");
                    ImGui.BulletText("Return/Enter to validate text input or toggle");
                    ImGui.BulletText("Arrow keys to navigate");
                    ImGui.BulletText("Space to activate item");
                    ImGui.BulletText("Page Up/Page Down to scroll");
                    ImGui.BulletText("Home/End to scroll to top/bottom");
                    ImGui.BulletText("Delete to delete character");
                    ImGui.BulletText("Backspace to delete character");
                    ImGui.BulletText("Cut/Copy/Paste with Ctrl+X/Ctrl+C/Ctrl+V");
                    ImGui.BulletText("Ctrl+Z,Ctrl+Y to undo/redo text edit");
                }

                if (ImGui.CollapsingHeader("MOUSE USAGE"))
                {
                    ImGui.BulletText("Click to focus, bring to front");
                    ImGui.BulletText("Click and drag to move window");
                    ImGui.BulletText("Click and drag on borders/corners to resize");
                    ImGui.BulletText("Right-click to open context menu");
                    ImGui.BulletText("Mouse wheel to scroll vertically");
                    ImGui.BulletText("Shift+Mouse wheel to scroll horizontally");
                    ImGui.BulletText("Ctrl+Mouse wheel to zoom");
                }
            }
            ImGui.End();
        }

        private void RenderSectionHeader(string icon, string title, Vector4 color)
        {
            RenderIcon(icon, color);
            ImGui.SameLine();
            ImGui.TextColored(color, title);
        }

        private void RenderIcon(string icon, Vector4 color)
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(color, icon);
            ImGuiManager.Instance?.PopIconFont();
        }

        private bool RenderIconButton(string icon, Vector4 color, string tooltip = null)
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Button, color * new Vector4(1, 1, 1, 0.2f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color * new Vector4(1, 1, 1, 0.4f));
            ImGui.PushStyleColor(ImGuiCol.Text, color);

            bool clicked = ImGui.Button(icon, new Vector2(24, 24));

            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            ImGui.PopStyleColor(3);
            ImGuiManager.Instance?.PopIconFont();
            return clicked;
        }

        private bool RenderStyledButton(string label, Vector4 color)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, color * new Vector4(1, 1, 1, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));

            bool clicked = ImGui.Button(label);

            ImGui.PopStyleColor(3);
            return clicked;
        }

        public void Dispose()
        {
            // No cleanup needed for this wrapper
        }
    }
}