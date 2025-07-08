// ScriptHost/ImGuiInterop.cs
using System;
using System.Runtime.InteropServices;

namespace WanderSpire.Scripting.UI
{
    /// <summary>
    /// Low-level P/Invoke wrapper for ImGui native integration.
    /// This class handles direct communication with the EngineCore ImGui API.
    /// </summary>
    internal static class ImGuiInterop
    {
        private const string Dll = "EngineCore";

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ImGui_Initialize(IntPtr ctx);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGui_Shutdown(IntPtr ctx);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ImGui_ProcessEvent(IntPtr ctx, IntPtr sdlEvent);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGui_NewFrame(IntPtr ctx);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGui_Render(IntPtr ctx);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ImGui_WantCaptureMouse(IntPtr ctx);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ImGui_WantCaptureKeyboard(IntPtr ctx);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGui_SetDisplaySize(IntPtr ctx, float width, float height);

        // Helper to get window size through proper engine API
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_GetWindowSize(IntPtr ctx, out int width, out int height);

        /* ── Docking helpers ───────────────────────────── */
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGui_SetDockingEnabled(int enabled);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGui_DockSpaceOverViewport(int dockNodeFlags);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImGui_GetFontAwesome();
    }
}