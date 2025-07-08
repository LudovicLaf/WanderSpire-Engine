// WanderSpire/Scripting/UI/ImGuiSafetyWrapper.cs - FIXED VERSION
using System;
using System.Numerics;

namespace WanderSpire.Scripting.UI
{
    /// <summary>
    /// Safety wrapper for ImGui operations that automatically handles cleanup
    /// and provides exception safety for common ImGui patterns.
    /// FIXED: Proper table state management to prevent assertion errors.
    /// </summary>
    public static class ImGuiSafe
    {
        /// <summary>
        /// Safely execute code with style variables, ensuring they're always popped.
        /// </summary>
        public static void WithStyleVars(Action<StyleVarScope> action, params (ImGuiStyleVar var, object value)[] styleVars)
        {
            var scope = new StyleVarScope();
            try
            {
                foreach (var (styleVar, value) in styleVars)
                {
                    switch (value)
                    {
                        case float f:
                            ImGui.PushStyleVar(styleVar, f);
                            scope.Count++;
                            break;
                        case Vector2 v2:
                            ImGui.PushStyleVar(styleVar, v2);
                            scope.Count++;
                            break;
                        default:
                            throw new ArgumentException($"Unsupported style var value type: {value.GetType()}");
                    }
                }

                action(scope);
            }
            finally
            {
                if (scope.Count > 0)
                    ImGui.PopStyleVar(scope.Count);
            }
        }

        /// <summary>
        /// Safely execute code with style colors, ensuring they're always popped.
        /// </summary>
        public static void WithStyleColors(Action action, params (ImGuiCol col, Vector4 color)[] styleColors)
        {
            int colorCount = 0;
            try
            {
                foreach (var (col, color) in styleColors)
                {
                    ImGui.PushStyleColor(col, color);
                    colorCount++;
                }

                action();
            }
            finally
            {
                if (colorCount > 0)
                    ImGui.PopStyleColor(colorCount);
            }
        }

        /// <summary>
        /// FIXED: Safely create a table with automatic cleanup and proper state management.
        /// </summary>
        public static void WithTable(string id, int columns, Action<TableScope> action,
            ImGuiTableFlags flags = ImGuiTableFlags.None, Vector2 outerSize = default, float innerWidth = 0.0f)
        {
            var scope = new TableScope();
            bool tableStarted = false;

            try
            {
                // CRITICAL FIX: Check if BeginTable actually succeeded
                tableStarted = ImGui.BeginTable(id, columns, flags, outerSize, innerWidth);
                scope.IsActive = tableStarted;

                // Only call the action if the table was successfully created
                if (tableStarted)
                {
                    action(scope);
                }
                else
                {
                    // Log that table creation failed but don't throw - just skip the table content
                    Console.WriteLine($"[ImGuiSafe] Table '{id}' failed to create - skipping table content");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGuiSafe] Error in table '{id}': {ex.Message}");
            }
            finally
            {
                // CRITICAL FIX: Only call EndTable if BeginTable succeeded
                if (tableStarted)
                {
                    try
                    {
                        ImGui.EndTable();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[ImGuiSafe] Error ending table '{id}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Safely create a window with automatic cleanup (no close button).
        /// </summary>
        public static void WithWindow(string title, Action<WindowScope> action,
            ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        {
            var scope = new WindowScope();
            try
            {
                scope.ShouldRender = ImGui.Begin(title, flags);

                if (scope.ShouldRender)
                {
                    action(scope);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGuiSafe] Error in window '{title}': {ex.Message}");
            }
            finally
            {
                ImGui.End();
            }
        }

        /// <summary>
        /// Safely create a window with automatic cleanup (with close button).
        /// </summary>
        public static void WithWindow(string title, ref bool open, Action<WindowScope> action,
            ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        {
            var scope = new WindowScope();
            try
            {
                scope.ShouldRender = ImGui.Begin(title, ref open, flags);
                scope.IsOpen = open;

                if (scope.ShouldRender)
                {
                    action(scope);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGuiSafe] Error in window '{title}': {ex.Message}");
            }
            finally
            {
                ImGui.End();
            }
        }

        /// <summary>
        /// Safely push an ID with automatic cleanup.
        /// </summary>
        public static void WithID(int id, Action action)
        {
            ImGui.PushID(id);
            try
            {
                action();
            }
            finally
            {
                ImGui.PopID();
            }
        }

        /// <summary>
        /// Safely push an ID with automatic cleanup.
        /// </summary>
        public static void WithID(string id, Action action)
        {
            ImGui.PushID(id);
            try
            {
                action();
            }
            finally
            {
                ImGui.PopID();
            }
        }

        /// <summary>
        /// Safely use a font with automatic cleanup.
        /// </summary>
        public static void WithFont(IntPtr font, Action action)
        {
            if (font != IntPtr.Zero)
            {
                ImGui.PushFont(font);
                try
                {
                    action();
                }
                finally
                {
                    ImGui.PopFont();
                }
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Safely use icon font with ImGuiManager.
        /// </summary>
        public static void WithIconFont(Action action)
        {
            ImGuiManager.Instance?.PushIconFont();
            try
            {
                action();
            }
            finally
            {
                ImGuiManager.Instance?.PopIconFont();
            }
        }

        /// <summary>
        /// Render text with an icon safely.
        /// </summary>
        public static void IconText(string icon, string text, Vector4? iconColor = null)
        {
            WithIconFont(() =>
            {
                if (iconColor.HasValue)
                {
                    ImGui.TextColored(iconColor.Value, icon);
                }
                else
                {
                    ImGui.Text(icon);
                }
            });

            ImGui.SameLine();
            ImGui.Text(text);
        }

        /// <summary>
        /// Safely render a colored button with icon.
        /// </summary>
        public static bool IconButton(string icon, Vector4 color, string tooltip = null, Vector2 size = default)
        {
            bool clicked = false;

            WithIconFont(() =>
            {
                WithStyleColors(() =>
                {
                    Vector2 btnSize = size == default ?
                        new Vector2(ImGui.CalcTextSize(icon).X + 8, ImGui.GetTextLineHeight() + 4) :
                        size;

                    clicked = ImGui.Button(icon, btnSize);

                    if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
                        ImGui.SetTooltip(tooltip);
                },
                (ImGuiCol.Button, color * new Vector4(1, 1, 1, 0.8f)),
                (ImGuiCol.ButtonHovered, color),
                (ImGuiCol.Text, Vector4.One));
            });

            return clicked;
        }
    }

    /// <summary>
    /// Scope helper for style variables.
    /// </summary>
    public class StyleVarScope
    {
        public int Count { get; internal set; }
    }

    /// <summary>
    /// FIXED: Scope helper for tables with proper state tracking.
    /// </summary>
    public class TableScope
    {
        public bool IsActive { get; internal set; }

        public void SetupColumn(string label, ImGuiTableColumnFlags flags = 0, float width = 0.0f)
        {
            // CRITICAL FIX: Only call table operations if table is actually active
            if (IsActive)
            {
                try
                {
                    ImGui.TableSetupColumn(label, flags, width);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ImGuiSafe] TableSetupColumn failed: {ex.Message}");
                }
            }
        }

        public void HeadersRow()
        {
            // CRITICAL FIX: Only call table operations if table is actually active
            if (IsActive)
            {
                try
                {
                    ImGui.TableHeadersRow();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ImGuiSafe] TableHeadersRow failed: {ex.Message}");
                }
            }
        }

        public void NextRow()
        {
            // CRITICAL FIX: Only call table operations if table is actually active
            if (IsActive)
            {
                try
                {
                    ImGui.TableNextRow();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ImGuiSafe] TableNextRow failed: {ex.Message}");
                }
            }
        }

        public bool NextColumn()
        {
            // CRITICAL FIX: Only call table operations if table is actually active
            if (IsActive)
            {
                try
                {
                    return ImGui.TableNextColumn();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ImGuiSafe] TableNextColumn failed: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// ADDITIONAL: Safe method to set table background colors
        /// </summary>
        public void SetBgColor(ImGuiTableBgTarget target, uint color, int columnN = -1)
        {
            if (IsActive)
            {
                try
                {
                    ImGui.TableSetBgColor(target, color, columnN);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ImGuiSafe] TableSetBgColor failed: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Scope helper for windows.
    /// </summary>
    public class WindowScope
    {
        public bool ShouldRender { get; internal set; }
        public bool IsOpen { get; internal set; } = true;
    }

    /// <summary>
    /// Enhanced ImGuiWindowBase with built-in safety features.
    /// </summary>
    public abstract class SafeImGuiWindowBase : IImGuiWindow, IDisposable
    {
        public abstract string Title { get; }
        public virtual bool IsVisible { get; set; } = true;

        protected bool _disposed = false;

        public void Render()
        {
            if (_disposed) return;

            try
            {
                bool isVisible = IsVisible;
                ImGuiSafe.WithWindow(Title, ref isVisible, window =>
                {
                    if (window.ShouldRender)
                    {
                        RenderContent();
                    }
                }, GetWindowFlags());

                IsVisible = isVisible;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[{GetType().Name}] Render error: {ex.Message}");

                // Show error in window if still open
                if (IsVisible)
                {
                    ImGuiSafe.WithWindow($"{Title} - Error", window =>
                    {
                        ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), "Render Error:");
                        ImGui.TextWrapped(ex.Message);
                    });
                }
            }
        }

        protected abstract void RenderContent();

        protected virtual ImGuiWindowFlags GetWindowFlags()
        {
            return ImGuiWindowFlags.None;
        }

        public virtual void Dispose()
        {
            _disposed = true;
        }
    }
}