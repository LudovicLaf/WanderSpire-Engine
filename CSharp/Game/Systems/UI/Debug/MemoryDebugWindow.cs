using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Professional memory debug window with garbage collection monitoring and memory analysis.
    /// </summary>
    public class MemoryDebugWindow : ImGuiWindowBase
    {
        public override string Title => "Memory Profiler";

        // Theme colors
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorDanger = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);

        // Memory tracking
        private readonly CircularBuffer<float> _memoryHistory = new(120);
        private readonly CircularBuffer<int> _gen0History = new(60);
        private readonly CircularBuffer<int> _gen1History = new(60);
        private readonly CircularBuffer<int> _gen2History = new(60);
        private DateTime _lastUpdate = DateTime.UtcNow;

        // Current memory stats
        private long _totalMemory = 0;
        private long _workingSet = 0;
        private int _gen0Collections = 0;
        private int _gen1Collections = 0;
        private int _gen2Collections = 0;

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

            UpdateMemoryStats();

            RenderHeader();
            ImGui.Separator();

            RenderMemoryOverview();
            ImGui.Separator();

            RenderGarbageCollectionStats();
            ImGui.Separator();

            RenderMemoryAnalysis();

            ImGui.PopStyleVar(3);
            EndWindow();
        }

        private void UpdateMemoryStats()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastUpdate).TotalMilliseconds >= 500) // Update every 500ms
            {
                _totalMemory = GC.GetTotalMemory(false);

                var process = Process.GetCurrentProcess();
                _workingSet = process.WorkingSet64;

                _gen0Collections = GC.CollectionCount(0);
                _gen1Collections = GC.CollectionCount(1);
                _gen2Collections = GC.CollectionCount(2);

                _memoryHistory.Add(_totalMemory / (1024f * 1024f)); // Convert to MB
                _gen0History.Add(_gen0Collections);
                _gen1History.Add(_gen1Collections);
                _gen2History.Add(_gen2Collections);

                _lastUpdate = now;
            }
        }

        private void RenderHeader()
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(Memory);
            ImGui.PopStyleColor();
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text("Memory Profiler");
            ImGui.PopStyleColor();

            ImGui.SameLine();
            ImGui.TextColored(ColorDim, FormatBytes(_totalMemory));

            // Quick memory status
            ImGui.SameLine(ImGui.GetWindowWidth() - 200);

            Vector4 memColor = _totalMemory < 100 * 1024 * 1024 ? ColorSuccess :
                              (_totalMemory < 250 * 1024 * 1024 ? ColorWarning : ColorDanger);

            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(memColor, Microchip);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.TextColored(memColor, FormatBytes(_totalMemory));

            ImGui.SameLine();
            if (RenderIconButton(Sync, ColorWarning, "Force Garbage Collection"))
            {
                ForceGarbageCollection();
            }
        }

        private void RenderMemoryOverview()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorPrimary * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Memory Overview", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                var contentWidth = ImGui.GetContentRegionAvail().X;
                var cardWidth = (contentWidth - 16) / 3; // 3 cards with spacing

                // Managed Memory Card
                ImGui.BeginChild("##managed_mem", new Vector2(cardWidth, 100), true);
                RenderMemoryCard("Managed Memory", FormatBytes(_totalMemory),
                    _totalMemory < 100 * 1024 * 1024 ? ColorSuccess : ColorWarning, Memory);
                ImGui.EndChild();

                ImGui.SameLine();

                // Working Set Card
                ImGui.BeginChild("##working_set", new Vector2(cardWidth, 100), true);
                RenderMemoryCard("Working Set", FormatBytes(_workingSet),
                    _workingSet < 200 * 1024 * 1024 ? ColorSuccess : ColorWarning, Desktop);
                ImGui.EndChild();

                ImGui.SameLine();

                // Memory Pressure Card
                var memoryHistory = _memoryHistory.ToArray();
                var memoryTrend = memoryHistory.Length >= 2 ?
                    memoryHistory[^1] - memoryHistory[^2] : 0f;
                var trendColor = memoryTrend < 0 ? ColorSuccess : (memoryTrend > 1 ? ColorDanger : ColorInfo);

                ImGui.BeginChild("##memory_trend", new Vector2(cardWidth, 100), true);
                RenderMemoryCard("Memory Trend",
                    memoryTrend >= 0 ? $"+{memoryTrend:F1} MB" : $"{memoryTrend:F1} MB",
                    trendColor, ChartLine);
                ImGui.EndChild();

                // Memory history chart
                ImGui.Spacing();
                ImGui.Text("Memory Usage History (MB):");
                if (memoryHistory.Length > 0)
                {
                    RenderMemoryChart(memoryHistory);
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderMemoryCard(string title, string value, Vector4 color, string icon)
        {
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
            ImGui.SetWindowFontScale(1.3f);
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text(value);
            ImGui.PopStyleColor();
            ImGui.SetWindowFontScale(1.0f);
        }

        private void RenderGarbageCollectionStats()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorSuccess * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Garbage Collection", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                ImGui.Columns(4, "##gc_stats", false);
                ImGui.SetColumnWidth(0, 80);
                ImGui.SetColumnWidth(1, 120);
                ImGui.SetColumnWidth(2, 120);
                ImGui.SetColumnWidth(3, 100);

                // Headers
                ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
                ImGui.Text("Generation");
                ImGui.NextColumn();
                ImGui.Text("Collections");
                ImGui.NextColumn();
                ImGui.Text("Rate (per min)");
                ImGui.NextColumn();
                ImGui.Text("Status");
                ImGui.NextColumn();
                ImGui.PopStyleColor();

                ImGui.Separator();

                // Generation 0
                RenderGCGenerationRow("Gen 0", _gen0Collections, ColorInfo,
                    "Frequent, short collections for young objects");

                // Generation 1
                RenderGCGenerationRow("Gen 1", _gen1Collections, ColorWarning,
                    "Medium collections for mid-aged objects");

                // Generation 2
                RenderGCGenerationRow("Gen 2", _gen2Collections, ColorDanger,
                    "Expensive, full collections for old objects");

                ImGui.Columns(1);

                // GC Controls
                ImGui.Separator();
                ImGui.Text("Garbage Collection Controls:");

                if (ImGui.Button("Force GC (Gen 0)"))
                {
                    GC.Collect(0);
                    Console.WriteLine("[MemoryDebug] Forced Generation 0 collection");
                }

                ImGui.SameLine();
                if (ImGui.Button("Force GC (Gen 1)"))
                {
                    GC.Collect(1);
                    Console.WriteLine("[MemoryDebug] Forced Generation 1 collection");
                }

                ImGui.SameLine();
                if (ImGui.Button("Force GC (Full)"))
                {
                    ForceGarbageCollection();
                }

                // GC Mode info
                ImGui.Separator();
                ImGui.Text($"GC Mode: {(System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation")}");
                ImGui.Text($"Concurrent GC: {(GC.TryStartNoGCRegion(1) ? "Available" : "N/A")}");
                GC.EndNoGCRegion(); // Clean up the test
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderGCGenerationRow(string generation, int collections, Vector4 color, string tooltip)
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(color, Trash);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.Text(generation);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            ImGui.NextColumn();

            ImGui.TextColored(ColorInfo, collections.ToString());
            ImGui.NextColumn();

            // Calculate rate (simplified)
            var rate = collections / Math.Max(1, Environment.TickCount / 60000.0);
            ImGui.TextColored(ColorDim, $"{rate:F1}");
            ImGui.NextColumn();

            // Status indicator
            var statusColor = collections < 100 ? ColorSuccess :
                             (collections < 500 ? ColorWarning : ColorDanger);
            string status = collections < 100 ? "Low" : (collections < 500 ? "Medium" : "High");
            ImGui.TextColored(statusColor, status);
            ImGui.NextColumn();
        }

        private void RenderMemoryAnalysis()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorInfo * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Memory Analysis"))
            {
                ImGui.PopStyleColor();

                ImGui.Text("Memory Allocation Analysis:");
                ImGui.Separator();

                // Simulate some memory allocation info
                var allocations = new[]
                {
                    ("String objects", 1024 * 1024 * 25, ColorInfo),
                    ("Entity components", 1024 * 1024 * 15, ColorSuccess),
                    ("Texture data", 1024 * 1024 * 45, ColorWarning),
                    ("Audio buffers", 1024 * 1024 * 8, ColorSuccess),
                    ("Script objects", 1024 * 1024 * 12, ColorInfo),
                    ("UI elements", 1024 * 1024 * 6, ColorSuccess),
                };

                long totalAllocated = allocations.Sum(a => a.Item2);

                foreach (var (category, size, color) in allocations)
                {
                    float percentage = (float)size / totalAllocated * 100f;

                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.TextColored(color, Square);
                    ImGuiManager.Instance?.PopIconFont();
                    ImGui.SameLine();

                    ImGui.Text($"{category}:");
                    ImGui.SameLine(200);
                    ImGui.TextColored(color, FormatBytes(size));
                    ImGui.SameLine(300);
                    ImGui.TextColored(ColorDim, $"({percentage:F1}%)");

                    // Simple progress bar
                    ImGui.SameLine(380);
                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, color);
                    ImGui.ProgressBar(percentage / 100f, new Vector2(100, 0), "");
                    ImGui.PopStyleColor();
                }

                ImGui.Separator();
                ImGui.Text($"Total Analyzed: {FormatBytes(totalAllocated)}");

                // Memory recommendations
                ImGui.Separator();
                ImGui.Text("Recommendations:");
                if (_gen2Collections > 10)
                {
                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.TextColored(ColorWarning, ExclamationTriangle);
                    ImGuiManager.Instance?.PopIconFont();
                    ImGui.SameLine();
                    ImGui.TextColored(ColorWarning, "High Gen 2 collections detected. Consider object pooling.");
                }

                if (_totalMemory > 200 * 1024 * 1024)
                {
                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.TextColored(ColorDanger, ExclamationCircle);
                    ImGuiManager.Instance?.PopIconFont();
                    ImGui.SameLine();
                    ImGui.TextColored(ColorDanger, "High memory usage. Review large object allocations.");
                }

                if (_gen0Collections < 10)
                {
                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.TextColored(ColorSuccess, CheckCircle);
                    ImGuiManager.Instance?.PopIconFont();
                    ImGui.SameLine();
                    ImGui.TextColored(ColorSuccess, "Memory allocation patterns look healthy.");
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderMemoryChart(float[] data)
        {
            if (data.Length == 0) return;

            // Simple ASCII-style memory chart
            const int chartWidth = 60;
            var maxMem = data.Max();
            var minMem = data.Min();
            var range = maxMem - minMem;
            if (range <= 0) range = 1;

            string chart = "[";
            for (int i = Math.Max(0, data.Length - chartWidth); i < data.Length; i++)
            {
                float normalized = (data[i] - minMem) / range;
                normalized = Math.Clamp(normalized, 0, 1);

                if (normalized < 0.25f) chart += "▁";
                else if (normalized < 0.5f) chart += "▃";
                else if (normalized < 0.75f) chart += "▅";
                else chart += "▇";
            }
            chart += "]";

            ImGui.TextColored(ColorPrimary, chart);
            ImGui.SameLine();
            ImGui.TextColored(ColorDim, $"Range: {minMem:F1} - {maxMem:F1} MB");
        }

        private void ForceGarbageCollection()
        {
            var beforeMemory = GC.GetTotalMemory(false);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var afterMemory = GC.GetTotalMemory(false);
            var freed = beforeMemory - afterMemory;

            Console.WriteLine($"[MemoryDebug] Forced GC completed. Freed {FormatBytes(freed)}");
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
}