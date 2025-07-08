using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Professional event log window for monitoring game events and debugging.
    /// </summary>
    public class EventLogWindow : ImGuiWindowBase, IDisposable
    {
        public override string Title => "Event Log";

        // Theme colors
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorDanger = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);

        // Event tracking
        private readonly List<LogEntry> _events = new();
        private readonly Dictionary<string, int> _eventCounts = new();
        private readonly object _eventLock = new();

        // UI State
        private string _searchFilter = "";
        private LogLevel _filterLevel = LogLevel.All;
        private bool _autoScroll = true;
        private bool _showTimestamps = true;
        private bool _showEventCounts = true;
        private int _maxEvents = 1000;

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

            if (_showEventCounts)
            {
                RenderEventCounts();
                ImGui.Separator();
            }

            RenderEventLog();

            ImGui.PopStyleVar(3);
            EndWindow();
        }

        private void RenderHeader()
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(List);
            ImGui.PopStyleColor();
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text("Event Monitor");
            ImGui.PopStyleColor();

            lock (_eventLock)
            {
                ImGui.SameLine();
                ImGui.TextColored(ColorDim, $"({_events.Count} events)");

                // Event rate indicator
                var recentEvents = _events.Where(e => (DateTime.UtcNow - e.Timestamp).TotalSeconds < 1).Count();
                if (recentEvents > 0)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(ColorWarning, $"{recentEvents}/s");
                }
            }

            // Status indicators
            ImGui.SameLine(ImGui.GetWindowWidth() - 100);
            if (_autoScroll)
            {
                ImGuiManager.Instance?.PushIconFont();
                ImGui.TextColored(ColorSuccess, ArrowDown);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.TextColored(ColorSuccess, "Auto");
            }
        }

        private void RenderControls()
        {
            // Search filter
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0, 0, 0.3f));
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("##search", ref _searchFilter, 128);
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Search events by message or type");

            // Log level filter
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            int levelIndex = (int)_filterLevel;
            string[] levelNames = { "All", "Info", "Warning", "Error", "Debug" };
            if (ImGui.Combo("##level", ref levelIndex, levelNames, levelNames.Length))
            {
                _filterLevel = (LogLevel)levelIndex;
            }

            // Control buttons
            ImGui.SameLine();
            if (RenderIconButton(Trash, ColorDanger, "Clear All Events"))
            {
                lock (_eventLock)
                {
                    _events.Clear();
                    _eventCounts.Clear();
                }
                Console.WriteLine("[EventLog] Cleared all events");
            }

            ImGui.SameLine();
            if (RenderIconButton(_autoScroll ? Pause : Play, _autoScroll ? ColorWarning : ColorSuccess,
                _autoScroll ? "Disable Auto-scroll" : "Enable Auto-scroll"))
            {
                _autoScroll = !_autoScroll;
            }

            ImGui.SameLine();
            if (RenderIconButton(Save, ColorPrimary, "Export Event Log"))
            {
                ExportEventLog();
            }

            // Options
            ImGui.SameLine();
            ImGui.Checkbox("Timestamps", ref _showTimestamps);
            ImGui.SameLine();
            ImGui.Checkbox("Event Counts", ref _showEventCounts);
        }

        private void RenderEventCounts()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorInfo * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Event Statistics", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                lock (_eventLock)
                {
                    if (_eventCounts.Count > 0)
                    {
                        var sortedCounts = _eventCounts.OrderByDescending(kvp => kvp.Value).Take(10);

                        ImGui.Columns(3, "##event_stats", false);
                        ImGui.SetColumnWidth(0, 200);
                        ImGui.SetColumnWidth(1, 80);

                        ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
                        ImGui.Text("Event Type");
                        ImGui.NextColumn();
                        ImGui.Text("Count");
                        ImGui.NextColumn();
                        ImGui.Text("Frequency");
                        ImGui.NextColumn();
                        ImGui.PopStyleColor();

                        ImGui.Separator();

                        foreach (var (eventType, count) in sortedCounts)
                        {
                            ImGuiManager.Instance?.PushIconFont();
                            ImGui.Text(GetEventIcon(eventType));
                            ImGuiManager.Instance?.PopIconFont();
                            ImGui.SameLine();
                            ImGui.Text(eventType);
                            ImGui.NextColumn();

                            ImGui.TextColored(ColorInfo, count.ToString());
                            ImGui.NextColumn();

                            // Simple frequency bar
                            float maxCount = _eventCounts.Values.Max();
                            float fraction = count / (float)maxCount;
                            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, ColorPrimary);
                            ImGui.ProgressBar(fraction, new Vector2(80, 0), "");
                            ImGui.PopStyleColor();
                            ImGui.NextColumn();
                        }

                        ImGui.Columns(1);
                    }
                    else
                    {
                        ImGui.TextColored(ColorDim, "No events recorded yet");
                    }
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderEventLog()
        {
            ImGui.BeginChild("##event_log", new Vector2(-1, -1), true);

            List<LogEntry> filteredEvents;
            lock (_eventLock)
            {
                filteredEvents = _events.Where(FilterEvent).ToList();
            }

            // Table setup
            if (_showTimestamps)
            {
                ImGui.Columns(4, "##event_table", false);
                ImGui.SetColumnWidth(0, 30);   // Icon
                ImGui.SetColumnWidth(1, 120);  // Timestamp
                ImGui.SetColumnWidth(2, 100);  // Type
                // Message takes remaining space
            }
            else
            {
                ImGui.Columns(3, "##event_table", false);
                ImGui.SetColumnWidth(0, 30);   // Icon
                ImGui.SetColumnWidth(1, 100);  // Type
                // Message takes remaining space
            }

            // Headers
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(""); // Icon column
            ImGui.NextColumn();
            if (_showTimestamps)
            {
                ImGui.Text("Time");
                ImGui.NextColumn();
            }
            ImGui.Text("Type");
            ImGui.NextColumn();
            ImGui.Text("Message");
            ImGui.NextColumn();
            ImGui.PopStyleColor();

            ImGui.Separator();

            // Render events
            for (int i = filteredEvents.Count - 1; i >= 0; i--) // Newest first
            {
                var entry = filteredEvents[i];
                RenderEventEntry(entry);
            }

            // Auto-scroll to bottom
            if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }

            ImGui.Columns(1);
            ImGui.EndChild();
        }

        private void RenderEventEntry(LogEntry entry)
        {
            var color = GetLevelColor(entry.Level);
            var icon = GetLevelIcon(entry.Level);

            ImGui.PushID($"event_{entry.Id}");

            // Icon column
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(color, icon);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.NextColumn();

            // Timestamp column
            if (_showTimestamps)
            {
                ImGui.TextColored(ColorDim, entry.Timestamp.ToString("HH:mm:ss.fff"));
                ImGui.NextColumn();
            }

            // Type column
            ImGui.TextColored(color, entry.EventType);
            ImGui.NextColumn();

            // Message column
            ImGui.TextWrapped(entry.Message);

            // Context menu
            if (ImGui.BeginPopupContextItem($"##ctx_event_{entry.Id}"))
            {
                if (ImGui.MenuItem("Copy Message"))
                {
                    Console.WriteLine($"Event message: {entry.Message}");
                }
                if (ImGui.MenuItem("Copy Full Entry"))
                {
                    var fullEntry = $"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level}] {entry.EventType}: {entry.Message}";
                    Console.WriteLine($"Event entry: {fullEntry}");
                }
                ImGui.EndPopup();
            }

            ImGui.NextColumn();
            ImGui.PopID();
        }

        private bool FilterEvent(LogEntry entry)
        {
            // Level filter
            if (_filterLevel != LogLevel.All && entry.Level != _filterLevel)
                return false;

            // Search filter
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                var search = _searchFilter.ToLower();
                if (!entry.Message.ToLower().Contains(search) &&
                    !entry.EventType.ToLower().Contains(search))
                    return false;
            }

            return true;
        }

        private Vector4 GetLevelColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => ColorInfo,
                LogLevel.Warning => ColorWarning,
                LogLevel.Error => ColorDanger,
                LogLevel.Debug => ColorDim,
                _ => ColorPrimary
            };
        }

        private string GetLevelIcon(LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => InfoCircle,
                LogLevel.Warning => ExclamationTriangle,
                LogLevel.Error => ExclamationCircle,
                LogLevel.Debug => Bug,
                _ => Circle
            };
        }

        private string GetEventIcon(string eventType)
        {
            return eventType.ToLower() switch
            {
                "movement" or "move" => Running,
                "attack" or "combat" => Khanda,
                "death" or "hurt" => Skull,
                "input" => Keyboard,
                "render" => PaintBrush,
                "audio" => VolumeUp,
                "system" => Cogs,
                _ => Circle
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

        private void ExportEventLog()
        {
            lock (_eventLock)
            {
                Console.WriteLine($"[EventLog] Exporting {_events.Count} events");
                foreach (var entry in _events.TakeLast(50)) // Export last 50 events
                {
                    Console.WriteLine($"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level}] {entry.EventType}: {entry.Message}");
                }
            }
        }

        // Public methods for logging events
        public void LogEvent(string eventType, string message, LogLevel level = LogLevel.Info)
        {
            lock (_eventLock)
            {
                var entry = new LogEntry
                {
                    Id = _events.Count,
                    Timestamp = DateTime.UtcNow,
                    EventType = eventType,
                    Message = message,
                    Level = level
                };

                _events.Add(entry);
                _eventCounts[eventType] = _eventCounts.GetValueOrDefault(eventType, 0) + 1;

                // Trim events if we exceed the maximum
                while (_events.Count > _maxEvents)
                {
                    _events.RemoveAt(0);
                }
            }
        }

        public void Dispose()
        {
            lock (_eventLock)
            {
                _events.Clear();
                _eventCounts.Clear();
            }
        }

        private class LogEntry
        {
            public int Id { get; set; }
            public DateTime Timestamp { get; set; }
            public string EventType { get; set; } = "";
            public string Message { get; set; } = "";
            public LogLevel Level { get; set; }
        }

        public enum LogLevel
        {
            All = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Debug = 4
        }
    }
}
