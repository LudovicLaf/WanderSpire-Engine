// ScriptHost/UI/IImGuiWindow.cs
namespace WanderSpire.Scripting.UI
{
    /// <summary>
    /// Interface for ImGui windows that can be registered with ImGuiManager.
    /// Implement this interface to create custom debug/editor windows.
    /// </summary>
    public interface IImGuiWindow
    {
        /// <summary>
        /// The title of the window (displayed in the title bar).
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Whether this window should be rendered. Set to false to hide the window.
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// Called every frame to render the window contents.
        /// Use ImGui.* calls here to build your UI.
        /// </summary>
        void Render();
    }

    /// <summary>
    /// Base class for ImGui windows providing common functionality.
    /// </summary>
    public abstract class ImGuiWindowBase : IImGuiWindow
    {
        public abstract string Title { get; }
        public virtual bool IsVisible { get; set; } = true;

        public abstract void Render();

        /// <summary>
        /// Helper method to begin a window with standard flags.
        /// Returns true if the window is visible and should be rendered.
        /// </summary>
        protected bool BeginWindow(ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        {
            bool open = IsVisible;
            bool shouldRender = ImGui.Begin(Title, ref open, flags);
            IsVisible = open;
            return shouldRender;
        }

        /// <summary>
        /// Helper method to end a window. Always call this after BeginWindow returns true.
        /// </summary>
        protected void EndWindow()
        {
            ImGui.End();
        }
    }

}