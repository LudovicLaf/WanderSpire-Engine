using System;
using System.Linq;
using System.Numerics;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Professional performance monitoring window with real-time metrics and charts.
    /// </summary>
    public class PerformanceWindow : ImGuiWindowBase
    {
        public override string Title => "Performance Monitor";

        // Theme colors
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorDanger = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);

        // Performance metrics
        private float _fps = 60.0f;
        private float _frameTime = 16.67f;
        private long _memoryUsage = 1024 * 1024 * 128; // 128 MB
        private int _entityCount = 42;
        private int _activeSystemsCount = 8;
        private float _cpuUsage = 25.5f;

        // History for charts
        private readonly CircularBuffer<float> _fpsHistory = new(120);
        private readonly CircularBuffer<float> _frameTimeHistory = new(120);
        private readonly CircularBuffer<float> _memoryHistory = new(120);
        private DateTime _lastUpdate = DateTime.UtcNow;

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

            UpdateMetrics();

            RenderHeader();
            ImGui.Separator();

            RenderMetricsOverview();
            ImGui.Separator();

            RenderDetailedMetrics();
            ImGui.Separator();

            RenderSystemPerformance();

            ImGui.PopStyleVar(3);
            EndWindow();
        }

        private void UpdateMetrics()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastUpdate).TotalMilliseconds >= 100) // Update every 100ms
            {
                // Simulate performance metrics (in real implementation, get from actual systems)
                _fps = 60.0f + (float)(Math.Sin(now.TimeOfDay.TotalSeconds) * 5);
                _frameTime = 1000.0f / _fps;
                _memoryUsage += (long)(Math.Sin(now.TimeOfDay.TotalSeconds * 2) * 1024 * 1024 * 10);
                _cpuUsage = 25.0f + (float)(Math.Sin(now.TimeOfDay.TotalSeconds * 0.5) * 15);

                _fpsHistory.Add(_fps);
                _frameTimeHistory.Add(_frameTime);
                _memoryHistory.Add(_memoryUsage / (1024f * 1024f)); // Convert to MB

                _lastUpdate = now;
            }
        }

        private void RenderHeader()
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(TachometerAlt);
            ImGui.PopStyleColor();
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text("Performance Monitor");
            ImGui.PopStyleColor();

            ImGui.SameLine();
            ImGui.TextColored(ColorDim, $"FPS: {_fps:F1}");

            // Quick status indicators
            ImGui.SameLine(ImGui.GetWindowWidth() - 150);

            // FPS status
            Vector4 fpsColor = _fps >= 55 ? ColorSuccess : (_fps >= 30 ? ColorWarning : ColorDanger);
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(fpsColor, Signal);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.TextColored(fpsColor, $"{_fps:F0}");

            ImGui.SameLine();
            // Memory status
            Vector4 memColor = _memoryUsage < 200 * 1024 * 1024 ? ColorSuccess :
                              (_memoryUsage < 500 * 1024 * 1024 ? ColorWarning : ColorDanger);
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(memColor, Memory);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.TextColored(memColor, FormatBytes(_memoryUsage));
        }

        private void RenderMetricsOverview()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorPrimary * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Real-time Metrics", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                // Performance cards in a 2x2 grid
                var avail = ImGui.GetContentRegionAvail();
                float cardWidth = (avail.X - 8) / 2; // Account for spacing

                // FPS Card
                ImGui.BeginChild("##fps_card", new Vector2(cardWidth, 80), true);
                RenderMetricCard("FPS", $"{_fps:F1}", _fps >= 55 ? ColorSuccess : (_fps >= 30 ? ColorWarning : ColorDanger), Signal);
                ImGui.EndChild();

                ImGui.SameLine();

                // Frame Time Card
                ImGui.BeginChild("##frametime_card", new Vector2(cardWidth, 80), true);
                RenderMetricCard("Frame Time", $"{_frameTime:F2}ms", _frameTime <= 20 ? ColorSuccess : (_frameTime <= 33 ? ColorWarning : ColorDanger), Clock);
                ImGui.EndChild();

                // Memory Card
                ImGui.BeginChild("##memory_card", new Vector2(cardWidth, 80), true);
                RenderMetricCard("Memory", FormatBytes(_memoryUsage), _memoryUsage < 200 * 1024 * 1024 ? ColorSuccess : ColorWarning, Memory);
                ImGui.EndChild();

                ImGui.SameLine();

                // CPU Card
                ImGui.BeginChild("##cpu_card", new Vector2(cardWidth, 80), true);
                RenderMetricCard("CPU Usage", $"{_cpuUsage:F1}%", _cpuUsage < 50 ? ColorSuccess : (_cpuUsage < 80 ? ColorWarning : ColorDanger), Microchip);
                ImGui.EndChild();
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderMetricCard(string title, string value, Vector4 color, string icon)
        {
            var cardSize = ImGui.GetContentRegionAvail();

            // Icon
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text(icon);
            ImGui.PopStyleColor();
            ImGuiManager.Instance?.PopIconFont();

            // Title
            ImGui.SameLine();
            ImGui.Text(title);

            // Value (larger text)
            ImGui.SetWindowFontScale(1.2f);
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text(value);
            ImGui.PopStyleColor();
            ImGui.SetWindowFontScale(1.0f);
        }

        private void RenderDetailedMetrics()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorInfo * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Detailed Metrics"))
            {
                ImGui.PopStyleColor();

                ImGui.Columns(2, "##detailed_metrics", false);

                // Left column - Current values
                ImGui.Text("Current Values:");
                ImGui.Separator();

                RenderDetailedMetricRow("Frame Rate", $"{_fps:F2} FPS", Signal);
                RenderDetailedMetricRow("Frame Time", $"{_frameTime:F3} ms", Clock);
                RenderDetailedMetricRow("Memory Usage", FormatBytes(_memoryUsage), Memory);
                RenderDetailedMetricRow("CPU Usage", $"{_cpuUsage:F1}%", Microchip);
                RenderDetailedMetricRow("Entity Count", _entityCount.ToString(), Cube);
                RenderDetailedMetricRow("Active Systems", _activeSystemsCount.ToString(), Cogs);

                ImGui.NextColumn();

                // Right column - Statistics
                ImGui.Text("Statistics:");
                ImGui.Separator();

                var fpsData = _fpsHistory.ToArray();
                if (fpsData.Length > 0)
                {
                    RenderDetailedMetricRow("FPS Min", $"{fpsData.Min():F1}", ArrowDown);
                    RenderDetailedMetricRow("FPS Max", $"{fpsData.Max():F1}", ArrowUp);
                    RenderDetailedMetricRow("FPS Avg", $"{fpsData.Average():F1}", Minus);
                }

                var frameData = _frameTimeHistory.ToArray();
                if (frameData.Length > 0)
                {
                    RenderDetailedMetricRow("Frame Min", $"{frameData.Min():F2}ms", ArrowDown);
                    RenderDetailedMetricRow("Frame Max", $"{frameData.Max():F2}ms", ArrowUp);
                    RenderDetailedMetricRow("Frame Avg", $"{frameData.Average():F2}ms", Minus);
                }

                ImGui.Columns(1);

                // Simple charts (text-based since we don't have ImPlot)
                ImGui.Separator();
                ImGui.Text("Performance History (last 120 frames):");

                if (fpsData.Length > 0)
                {
                    ImGui.Text("FPS:");
                    ImGui.SameLine();
                    RenderSimpleChart(fpsData, 30, 120);

                    ImGui.Text("Frame Time:");
                    ImGui.SameLine();
                    RenderSimpleChart(frameData, 0, 50);
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderDetailedMetricRow(string label, string value, string icon)
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(ColorDim, icon);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.Text($"{label}:");
            ImGui.SameLine(120);
            ImGui.TextColored(ColorInfo, value);
        }

        private void RenderSimpleChart(float[] data, float minVal, float maxVal)
        {
            if (data.Length == 0) return;

            // Create a simple ASCII-style chart
            const int chartWidth = 40;
            var range = maxVal - minVal;
            if (range <= 0) range = 1;

            string chart = "[";
            for (int i = Math.Max(0, data.Length - chartWidth); i < data.Length; i++)
            {
                float normalized = (data[i] - minVal) / range;
                normalized = Math.Clamp(normalized, 0, 1);

                if (normalized < 0.25f) chart += "▁";
                else if (normalized < 0.5f) chart += "▃";
                else if (normalized < 0.75f) chart += "▅";
                else chart += "▇";
            }
            chart += "]";

            ImGui.TextColored(ColorPrimary, chart);
        }

        private void RenderSystemPerformance()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorSuccess * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("System Performance"))
            {
                ImGui.PopStyleColor();

                ImGui.Text("Active Systems:");
                ImGui.Separator();

                // Simulate system performance data
                var systems = new[]
                {
                    ("MovementSystem", 2.3f, ColorSuccess),
                    ("RenderSystem", 8.7f, ColorWarning),
                    ("PhysicsSystem", 1.2f, ColorSuccess),
                    ("AISystem", 4.5f, ColorInfo),
                    ("AudioSystem", 0.8f, ColorSuccess),
                    ("InputSystem", 0.3f, ColorSuccess),
                    ("ScriptEngine", 3.2f, ColorInfo),
                    ("DebugUISystem", 1.8f, ColorSuccess)
                };

                foreach (var (name, time, color) in systems)
                {
                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.TextColored(color, Cog);
                    ImGuiManager.Instance?.PopIconFont();
                    ImGui.SameLine();
                    ImGui.Text(name);
                    ImGui.SameLine(200);
                    ImGui.TextColored(color, $"{time:F1}ms");

                    // Simple progress bar
                    ImGui.SameLine(280);
                    float fraction = Math.Min(time / 10.0f, 1.0f);
                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, color);
                    ImGui.ProgressBar(fraction, new Vector2(100, 0), "");
                    ImGui.PopStyleColor();
                }

                ImGui.Separator();

                // Control buttons
                if (RenderIconButton(Sync, ColorPrimary, "Reset Performance Counters"))
                {
                    _fpsHistory.Clear();
                    _frameTimeHistory.Clear();
                    _memoryHistory.Clear();
                    Console.WriteLine("[Performance] Reset performance counters");
                }

                ImGui.SameLine();
                if (RenderIconButton(Save, ColorSuccess, "Export Performance Data"))
                {
                    Console.WriteLine("[Performance] Export performance data");
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
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

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:F1} {suffixes[counter]}";
        }
    }

    // Helper class for circular buffer (same as in DebugUIUtilities.cs)
    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _index = 0;
        private bool _full = false;

        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
        }

        public void Add(T item)
        {
            _buffer[_index] = item;
            _index = (_index + 1) % _buffer.Length;
            if (_index == 0) _full = true;
        }

        public T[] ToArray()
        {
            if (!_full)
            {
                var result = new T[_index];
                Array.Copy(_buffer, 0, result, 0, _index);
                return result;
            }
            else
            {
                var result = new T[_buffer.Length];
                Array.Copy(_buffer, _index, result, 0, _buffer.Length - _index);
                Array.Copy(_buffer, 0, result, _buffer.Length - _index, _index);
                return result;
            }
        }

        public void Clear()
        {
            _index = 0;
            _full = false;
            Array.Clear(_buffer, 0, _buffer.Length);
        }

        public int Count => _full ? _buffer.Length : _index;
        public int Capacity => _buffer.Length;
    }
}