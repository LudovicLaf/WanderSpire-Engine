// Game/Systems/UI/DebugUIUtilities.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WanderSpire.Scripting.UI;

namespace Game.Systems.UI
{
    /// <summary>
    /// Utility methods for debug UI windows.
    /// </summary>
    public static class DebugUIUtilities
    {
        /// <summary>
        /// Renders a generic object's properties using reflection and ImGui.
        /// </summary>
        public static void RenderObjectProperties(object obj, string title = "Properties")
        {
            if (obj == null)
            {
                ImGui.Text("Object is null");
                return;
            }

            if (ImGui.CollapsingHeader(title))
            {
                var type = obj.GetType();
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                // Render properties
                foreach (var prop in properties)
                {
                    try
                    {
                        if (prop.CanRead)
                        {
                            var value = prop.GetValue(obj);
                            RenderValue(prop.Name, value, prop.PropertyType);
                        }
                    }
                    catch (Exception ex)
                    {
                        ImGui.TextColored(1.0f, 0.5f, 0.5f, 1.0f, $"{prop.Name}: Error - {ex.Message}");
                    }
                }

                // Render fields
                foreach (var field in fields)
                {
                    try
                    {
                        var value = field.GetValue(obj);
                        RenderValue(field.Name, value, field.FieldType);
                    }
                    catch (Exception ex)
                    {
                        ImGui.TextColored(1.0f, 0.5f, 0.5f, 1.0f, $"{field.Name}: Error - {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Renders a value with appropriate ImGui controls based on its type.
        /// </summary>
        private static void RenderValue(string name, object? value, Type type)
        {
            if (value == null)
            {
                ImGui.Text($"{name}: null");
                return;
            }

            // Handle different types
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    ImGui.Text($"{name}: {(bool)value}");
                    break;
                case TypeCode.Int32:
                    ImGui.Text($"{name}: {(int)value}");
                    break;
                case TypeCode.Single:
                    ImGui.Text($"{name}: {(float)value:F3}");
                    break;
                case TypeCode.Double:
                    ImGui.Text($"{name}: {(double)value:F3}");
                    break;
                case TypeCode.String:
                    ImGui.Text($"{name}: \"{value}\"");
                    break;
                default:
                    if (type.IsArray)
                    {
                        RenderArrayValue(name, value);
                    }
                    else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                        RenderDictionaryValue(name, value);
                    }
                    else
                    {
                        ImGui.Text($"{name}: {value}");
                    }
                    break;
            }
        }

        /// <summary>
        /// Renders an array value.
        /// </summary>
        private static void RenderArrayValue(string name, object arrayValue)
        {
            if (arrayValue is Array array)
            {
                if (ImGui.TreeNode($"{name} [{array.Length}]"))
                {
                    for (int i = 0; i < Math.Min(array.Length, 20); i++) // Limit to 20 items
                    {
                        var item = array.GetValue(i);
                        ImGui.Text($"[{i}]: {item}");
                    }

                    if (array.Length > 20)
                    {
                        ImGui.Text($"... and {array.Length - 20} more items");
                    }

                    ImGui.TreePop();
                }
            }
        }

        /// <summary>
        /// Renders a dictionary value.
        /// </summary>
        private static void RenderDictionaryValue(string name, object dictValue)
        {
            if (dictValue is System.Collections.IDictionary dict)
            {
                if (ImGui.TreeNode($"{name} [{dict.Count}]"))
                {
                    int count = 0;
                    foreach (var key in dict.Keys)
                    {
                        if (count >= 20) break; // Limit to 20 items
                        var value = dict[key];
                        ImGui.Text($"{key}: {value}");
                        count++;
                    }

                    if (dict.Count > 20)
                    {
                        ImGui.Text($"... and {dict.Count - 20} more items");
                    }

                    ImGui.TreePop();
                }
            }
        }

        /// <summary>
        /// Creates a simple color from RGB values (0-255).
        /// </summary>
        public static void TextColored(byte r, byte g, byte b, string text)
        {
            ImGui.TextColored(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f, text);
        }

        /// <summary>
        /// Renders a progress bar with text.
        /// </summary>
        public static void ProgressBar(float fraction, string text)
        {
            // ImGui doesn't have progress bar in our current implementation
            // So we'll simulate it with colored text
            int barWidth = 20;
            int filled = (int)(fraction * barWidth);
            string bar = "[" + new string('=', filled) + new string('-', barWidth - filled) + "]";
            ImGui.Text($"{text}: {bar} {fraction * 100:F1}%");
        }

        /// <summary>
        /// Formats bytes into a human-readable string.
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:F2} {suffixes[counter]}";
        }

        /// <summary>
        /// Creates a tooltip when hovering over the last item.
        /// </summary>
        public static void SetTooltip(string text)
        {
            // ImGui tooltip would go here if available in our API
            // For now, we'll just log it
        }

        /// <summary>
        /// Renders a tree node that can be expanded/collapsed.
        /// </summary>
        public static bool TreeNodeWithCount(string label, int count)
        {
            return ImGui.TreeNode($"{label} ({count})");
        }

        /// <summary>
        /// Helper to format time spans nicely.
        /// </summary>
        public static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
            if (timeSpan.TotalMinutes >= 1)
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            return $"{timeSpan.TotalSeconds:F1}s";
        }
    }


    /// <summary>
    /// Performance profiler for tracking method execution times.
    /// </summary>
    public class PerformanceProfiler
    {
        private readonly Dictionary<string, CircularBuffer<float>> _timings = new();
        private readonly Dictionary<string, DateTime> _startTimes = new();

        public void StartTimer(string name)
        {
            _startTimes[name] = DateTime.UtcNow;
        }

        public void EndTimer(string name)
        {
            if (_startTimes.TryGetValue(name, out var startTime))
            {
                var elapsed = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;

                if (!_timings.ContainsKey(name))
                {
                    _timings[name] = new CircularBuffer<float>(60); // Store last 60 measurements
                }

                _timings[name].Add(elapsed);
                _startTimes.Remove(name);
            }
        }

        public float GetAverageTime(string name)
        {
            if (_timings.TryGetValue(name, out var buffer))
            {
                var values = buffer.ToArray();
                return values.Length > 0 ? values.Average() : 0f;
            }
            return 0f;
        }

        public float GetMaxTime(string name)
        {
            if (_timings.TryGetValue(name, out var buffer))
            {
                var values = buffer.ToArray();
                return values.Length > 0 ? values.Max() : 0f;
            }
            return 0f;
        }

        public void Clear()
        {
            _timings.Clear();
            _startTimes.Clear();
        }

        public IEnumerable<string> GetTrackedNames() => _timings.Keys;
    }

    /// <summary>
    /// Event tracker for monitoring game events.
    /// </summary>
    public class EventTracker
    {
        private readonly Dictionary<string, int> _eventCounts = new();
        private readonly Dictionary<string, DateTime> _lastEventTimes = new();
        private readonly object _lock = new();

        public void RecordEvent(string eventName)
        {
            lock (_lock)
            {
                _eventCounts[eventName] = _eventCounts.GetValueOrDefault(eventName, 0) + 1;
                _lastEventTimes[eventName] = DateTime.UtcNow;
            }
        }

        public int GetEventCount(string eventName)
        {
            lock (_lock)
            {
                return _eventCounts.GetValueOrDefault(eventName, 0);
            }
        }

        public DateTime? GetLastEventTime(string eventName)
        {
            lock (_lock)
            {
                return _lastEventTimes.TryGetValue(eventName, out var time) ? time : null;
            }
        }

        public Dictionary<string, int> GetAllEventCounts()
        {
            lock (_lock)
            {
                return new Dictionary<string, int>(_eventCounts);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _eventCounts.Clear();
                _lastEventTimes.Clear();
            }
        }
    }
}