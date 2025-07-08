using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WanderSpire.Scripting;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Professional input debug window for monitoring input state and key bindings.
    /// </summary>
    public class InputDebugWindow : ImGuiWindowBase
    {
        public override string Title => "Input Monitor";

        // Theme colors
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorDanger = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);

        // Input tracking
        private readonly List<KeyState> _keyStates = new();
        private readonly Dictionary<string, List<KeyCode>> _keyBindings = new();
        private readonly List<InputEvent> _inputHistory = new();
        private Vector2 _mousePosition = Vector2.Zero;
        private MouseButtonState _mouseButtonState = new();

        // UI state
        private bool _showKeyboard = true;
        private bool _showMouse = true;
        private bool _showBindings = true;
        private bool _showHistory = false;
        private string _keyFilter = "";

        public InputDebugWindow()
        {
            InitializeKeyBindings();
            UpdateInputStates();
        }

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

            UpdateInputStates();

            RenderHeader();
            ImGui.Separator();

            if (_showMouse)
            {
                RenderMouseInput();
                ImGui.Separator();
            }

            if (_showKeyboard)
            {
                RenderKeyboardInput();
                ImGui.Separator();
            }

            if (_showBindings)
            {
                RenderKeyBindings();
                ImGui.Separator();
            }

            if (_showHistory)
            {
                RenderInputHistory();
            }

            ImGui.PopStyleVar(3);
            EndWindow();
        }

        private void InitializeKeyBindings()
        {
            _keyBindings["Movement"] = new List<KeyCode> { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
            _keyBindings["Combat"] = new List<KeyCode> { KeyCode.Space, KeyCode.LShift, KeyCode.LCtrl };
            _keyBindings["Interface"] = new List<KeyCode> { KeyCode.Tab, KeyCode.Escape, KeyCode.Return };
            _keyBindings["Function Keys"] = new List<KeyCode> { KeyCode.F1, KeyCode.F5, KeyCode.F9, KeyCode.F12 };
            _keyBindings["Debug"] = new List<KeyCode> { KeyCode.Grave, KeyCode.I, KeyCode.O, KeyCode.B };
        }

        private void UpdateInputStates()
        {
            // Update mouse position (simulated)
            _mousePosition = new Vector2(Input.MouseX, Input.MouseY);

            // Update mouse buttons
            _mouseButtonState.Left = Input.GetMouseButton(MouseButton.Left);
            _mouseButtonState.Right = Input.GetMouseButton(MouseButton.Right);
            _mouseButtonState.Middle = Input.GetMouseButton(MouseButton.Middle);

            // Update key states for common keys
            var commonKeys = new[]
            {
                KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D,
                KeyCode.Space, KeyCode.LShift, KeyCode.LCtrl, KeyCode.Tab,
                KeyCode.Escape, KeyCode.Return, KeyCode.Grave,
                KeyCode.F1, KeyCode.F5, KeyCode.F9, KeyCode.F12,
                KeyCode.I, KeyCode.O, KeyCode.B, KeyCode.R
            };

            _keyStates.Clear();
            foreach (var key in commonKeys)
            {
                var state = new KeyState
                {
                    Key = key,
                    IsPressed = Input.GetKey(key),
                    WasPressed = Input.GetKeyDown(key),
                    WasReleased = Input.GetKeyUp(key)
                };
                _keyStates.Add(state);

                // Add to input history
                if (state.WasPressed)
                {
                    _inputHistory.Add(new InputEvent
                    {
                        Type = InputEventType.KeyDown,
                        Key = key,
                        Timestamp = DateTime.UtcNow
                    });

                    // Limit history size
                    while (_inputHistory.Count > 50)
                        _inputHistory.RemoveAt(0);
                }
            }
        }

        private void RenderHeader()
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(Keyboard);
            ImGui.PopStyleColor();
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text("Input Monitor");
            ImGui.PopStyleColor();

            var pressedKeys = _keyStates.Count(k => k.IsPressed);
            ImGui.SameLine();
            ImGui.TextColored(ColorDim, $"({pressedKeys} keys pressed)");

            // Display toggles
            ImGui.SameLine(ImGui.GetWindowWidth() - 250);
            ImGui.Checkbox("Mouse", ref _showMouse);
            ImGui.SameLine();
            ImGui.Checkbox("Keyboard", ref _showKeyboard);
            ImGui.SameLine();
            ImGui.Checkbox("Bindings", ref _showBindings);
            ImGui.SameLine();
            ImGui.Checkbox("History", ref _showHistory);
        }

        private void RenderMouseInput()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorInfo * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Mouse Input", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                ImGui.Columns(2, "##mouse_input", false);
                ImGui.SetColumnWidth(0, 120);

                // Mouse position
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(MousePointer);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Position:");
                ImGui.NextColumn();
                ImGui.TextColored(ColorInfo, $"({_mousePosition.X:F0}, {_mousePosition.Y:F0})");
                ImGui.NextColumn();

                // Mouse buttons
                RenderMouseButton("Left Button:", _mouseButtonState.Left);
                RenderMouseButton("Right Button:", _mouseButtonState.Right);
                RenderMouseButton("Middle Button:", _mouseButtonState.Middle);

                ImGui.Columns(1);

                // Mouse visualization
                ImGui.Spacing();
                ImGui.Text("Mouse Buttons:");

                // Visual representation of mouse buttons
                var buttonSize = new Vector2(40, 30);
                var buttonColor = _mouseButtonState.Left ? ColorSuccess : ColorDim;
                ImGui.PushStyleColor(ImGuiCol.Button, buttonColor * new Vector4(1, 1, 1, 0.8f));
                ImGui.Button("L", buttonSize);
                ImGui.PopStyleColor();

                ImGui.SameLine();
                buttonColor = _mouseButtonState.Middle ? ColorWarning : ColorDim;
                ImGui.PushStyleColor(ImGuiCol.Button, buttonColor * new Vector4(1, 1, 1, 0.8f));
                ImGui.Button("M", buttonSize);
                ImGui.PopStyleColor();

                ImGui.SameLine();
                buttonColor = _mouseButtonState.Right ? ColorDanger : ColorDim;
                ImGui.PushStyleColor(ImGuiCol.Button, buttonColor * new Vector4(1, 1, 1, 0.8f));
                ImGui.Button("R", buttonSize);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderMouseButton(string label, bool isPressed)
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(isPressed ? ColorSuccess : ColorDim, isPressed ? CheckCircle : Circle);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.Text(label);
            ImGui.NextColumn();
            ImGui.TextColored(isPressed ? ColorSuccess : ColorDim, isPressed ? "Pressed" : "Released");
            ImGui.NextColumn();
        }

        private void RenderKeyboardInput()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorSuccess * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Keyboard Input", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                // Key filter
                ImGui.Text("Filter:");
                ImGui.SetNextItemWidth(200);
                ImGui.InputText("##key_filter", ref _keyFilter, 128);

                ImGui.Spacing();

                // Key states table
                var filteredKeys = _keyStates.Where(k =>
                    string.IsNullOrEmpty(_keyFilter) ||
                    k.Key.ToString().ToLower().Contains(_keyFilter.ToLower())
                ).ToList();

                ImGui.Columns(4, "##key_states", true);
                ImGui.SetColumnWidth(0, 30);  // Icon
                ImGui.SetColumnWidth(1, 120); // Key name
                ImGui.SetColumnWidth(2, 80);  // State
                ImGui.SetColumnWidth(3, 80);  // Events

                // Headers
                ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
                ImGui.Text(""); // Icon column
                ImGui.NextColumn();
                ImGui.Text("Key");
                ImGui.NextColumn();
                ImGui.Text("State");
                ImGui.NextColumn();
                ImGui.Text("Events");
                ImGui.NextColumn();
                ImGui.PopStyleColor();

                ImGui.Separator();

                // Render key states
                foreach (var keyState in filteredKeys)
                {
                    RenderKeyStateRow(keyState);
                }

                ImGui.Columns(1);

                // Axis display
                ImGui.Spacing();
                ImGui.Text("Input Axes:");
                RenderAxisDisplay("Horizontal", Input.GetAxis("Horizontal"));
                RenderAxisDisplay("Vertical", Input.GetAxis("Vertical"));
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderKeyStateRow(KeyState keyState)
        {
            var stateColor = keyState.IsPressed ? ColorSuccess : ColorDim;
            var keyIcon = GetKeyIcon(keyState.Key);

            // Icon
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(stateColor, keyIcon);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.NextColumn();

            // Key name
            ImGui.Text(keyState.Key.ToString());
            ImGui.NextColumn();

            // State
            ImGui.TextColored(stateColor, keyState.IsPressed ? "Pressed" : "Released");
            ImGui.NextColumn();

            // Events
            string events = "";
            if (keyState.WasPressed) events += "↓ ";
            if (keyState.WasReleased) events += "↑ ";

            if (!string.IsNullOrEmpty(events))
                ImGui.TextColored(ColorWarning, events);
            else
                ImGui.Text("");

            ImGui.NextColumn();
        }

        private void RenderAxisDisplay(string axisName, float value)
        {
            ImGui.Text($"{axisName}:");
            ImGui.SameLine(100);

            var axisColor = Math.Abs(value) > 0.1f ? ColorPrimary : ColorDim;
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, axisColor);
            ImGui.ProgressBar((value + 1) * 0.5f, new Vector2(100, 0), $"{value:F2}");
            ImGui.PopStyleColor();
        }

        private void RenderKeyBindings()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorPrimary * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Key Bindings", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                foreach (var (category, keys) in _keyBindings)
                {
                    ImGui.PushStyleColor(ImGuiCol.Header, ColorWarning * new Vector4(1, 1, 1, 0.2f));
                    if (ImGui.CollapsingHeader(category))
                    {
                        ImGui.PopStyleColor();

                        ImGui.Indent();
                        foreach (var key in keys)
                        {
                            var keyState = _keyStates.FirstOrDefault(k => k.Key == key);
                            var isPressed = keyState?.IsPressed ?? false;
                            var stateColor = isPressed ? ColorSuccess : ColorDim;

                            ImGuiManager.Instance?.PushIconFont();
                            ImGui.TextColored(stateColor, GetKeyIcon(key));
                            ImGuiManager.Instance?.PopIconFont();
                            ImGui.SameLine();
                            ImGui.TextColored(stateColor, key.ToString());

                            if (isPressed)
                            {
                                ImGui.SameLine();
                                ImGui.TextColored(ColorSuccess, "[ACTIVE]");
                            }
                        }
                        ImGui.Unindent();
                    }
                    else
                    {
                        ImGui.PopStyleColor();
                    }
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderInputHistory()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorWarning * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Input History", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                if (RenderIconButton(Trash, ColorDanger, "Clear History"))
                {
                    _inputHistory.Clear();
                }

                ImGui.Separator();

                // Show recent input events
                var recentEvents = _inputHistory.TakeLast(20).Reverse();
                foreach (var inputEvent in recentEvents)
                {
                    var timeSince = DateTime.UtcNow - inputEvent.Timestamp;
                    var timeColor = timeSince.TotalSeconds < 1 ? ColorSuccess :
                                   (timeSince.TotalSeconds < 5 ? ColorWarning : ColorDim);

                    ImGui.TextColored(ColorDim, $"[{inputEvent.Timestamp:HH:mm:ss.fff}]");
                    ImGui.SameLine();

                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.TextColored(timeColor, GetKeyIcon(inputEvent.Key));
                    ImGuiManager.Instance?.PopIconFont();
                    ImGui.SameLine();

                    ImGui.Text($"{inputEvent.Key} {inputEvent.Type}");
                }

                if (_inputHistory.Count == 0)
                {
                    ImGui.TextColored(ColorDim, "No input events recorded yet.");
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private string GetKeyIcon(KeyCode key)
        {
            return key switch
            {
                KeyCode.W or KeyCode.A or KeyCode.S or KeyCode.D => ArrowsAlt,
                KeyCode.Space => Square,
                KeyCode.LShift or KeyCode.RShift => ArrowUp,
                KeyCode.LCtrl or KeyCode.RCtrl => ArrowDown,
                KeyCode.Tab => List,
                KeyCode.Escape => Times,
                KeyCode.Return => Check,
                KeyCode.Grave => Terminal,
                >= KeyCode.F1 and <= KeyCode.F12 => Cog,
                _ => Keyboard
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

        private class KeyState
        {
            public KeyCode Key { get; set; }
            public bool IsPressed { get; set; }
            public bool WasPressed { get; set; }
            public bool WasReleased { get; set; }
        }

        private class MouseButtonState
        {
            public bool Left { get; set; }
            public bool Right { get; set; }
            public bool Middle { get; set; }
        }

        private class InputEvent
        {
            public InputEventType Type { get; set; }
            public KeyCode Key { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private enum InputEventType
        {
            KeyDown,
            KeyUp
        }
    }
}