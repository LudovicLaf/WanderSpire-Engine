// EngineInterop.cs - Core Engine Functionality
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WanderSpire.Scripting
{
    #region Core Types and Enums

    /// <summary>Entity handle for the ECS system</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EntityId
    {
        public uint id;
        public bool IsValid => id != 0xFFFFFFFF;
        public static readonly EntityId Invalid = new EntityId { id = 0xFFFFFFFF };
    }


    /// <summary>Standard render layers - use custom values between these for fine control</summary>
    public enum RenderLayer
    {
        Background = -1000,  // Clear operations, skybox
        Terrain = 0,         // Ground tiles, background elements  
        Entities = 100,      // Game objects, sprites, characters
        Effects = 200,       // Particles, visual effects
        UI = 1000,          // User interface elements
        Debug = 2000,       // Debug overlays, gizmos
        PostProcess = 3000   // Screen effects, filters
    }

    /// <summary>Brush types for tile painting</summary>
    public enum BrushType
    {
        Single = 0,
        Rectangle = 1,
        Circle = 2,
        Line = 3,
        Pattern = 4,
        Multi = 5
    }

    /// <summary>Blend modes for tile painting</summary>
    public enum BlendMode
    {
        Replace = 0,
        Add = 1,
        Subtract = 2,
        Overlay = 3
    }

    #endregion

    #region Data Structures

    /// <summary>
    /// Frame rendering statistics
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FrameStats
    {
        public float frameTime;
        public float renderTime;
        public float updateTime;
        public int drawCalls;
        public int triangles;
        public int entities;
        public long memoryUsed;
    }

    /// <summary>
    /// Performance metrics
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PerformanceMetrics
    {
        public float avgFrameTime;
        public float minFrameTime;
        public float maxFrameTime;
        public float avgFPS;
        public int totalDrawCalls;
        public int totalTriangles;
        public long totalMemoryUsed;
        public long peakMemoryUsed;
    }

    /// <summary>
    /// Profiling section result
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ProfileSection
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string name;
        public float totalTime;
        public float avgTime;
        public float minTime;
        public float maxTime;
        public int callCount;
    }

    /// <summary>
    /// Texture information
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TextureInfo
    {
        public int width;
        public int height;
        public int channels;
        public int format;
        public long memorySize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string path;
    }

    #endregion

    #region Editor Render Flags

    [Flags]
    public enum EditorRenderFlags
    {
        None = 0,
        ShowGrid = 1 << 0,
        ShowGizmos = 1 << 1,
        ShowBounds = 1 << 2,
        ShowWireframe = 1 << 3,
        ShowNormals = 1 << 4,
        ShowColliders = 1 << 5,
        ShowLights = 1 << 6,
        ShowCameras = 1 << 7,
        ShowAudio = 1 << 8,
        ShowParticles = 1 << 9,
        ShowUI = 1 << 10,
        All = 0x7FF
    }

    [Flags]
    public enum DebugRenderFlags
    {
        None = 0,
        EntityBounds = 1 << 0,
        CollisionShapes = 1 << 1,
        Pathfinding = 1 << 2,
        Physics = 1 << 3,
        Audio = 1 << 4,
        Lighting = 1 << 5,
        Performance = 1 << 6,
        All = 0x7F
    }

    public enum GizmoType
    {
        Translation = 0,
        Rotation = 1,
        Scale = 2,
        Universal = 3
    }

    #endregion

    /// <summary>
    /// Main engine interop class providing core functionality
    /// </summary>
    public static class EngineInterop
    {
        private const string DLL = "EngineCore";

        /// <summary>
        /// Invalid entity constant - matches WS_INVALID_ENTITY in C++
        /// </summary>
        public const uint WS_INVALID_ENTITY = 0xFFFFFFFF;

        #region Delegate Types

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RenderCallback(IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ScriptEventCallback(IntPtr eventName, IntPtr payload, int payloadSize, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RawAction(IntPtr user);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RunInCtx_t(RawAction fn, IntPtr user);
        #endregion

        #region Core Engine Lifecycle

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateEngineContext();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyEngineContext(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EngineInit(IntPtr ctx, int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] argv);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EngineIterate(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void EngineQuit(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EngineEvent(IntPtr ctx, IntPtr rawEvent);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Engine_GetTileSize(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Engine_GetWindow(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_GetWindowSize(IntPtr ctx, out int outWidth, out int outHeight);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_GetMouseTile(IntPtr ctx, out int outX, out int outY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Engine_GetTickInterval(IntPtr ctx);

        #endregion

        #region SDL3 Direct Functions

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_ShowWindow(IntPtr window);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_RaiseWindow(IntPtr window);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SDL_PollEvent(IntPtr sdlEventPtr);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_GetWindowSizeInPixels(IntPtr window, out int width, out int height);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_GetMouseState(out float x, out float y);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GL_GetCurrentWindow();

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GL_GetCurrentContext();

        #endregion

        #region Render Pipeline API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Render_SubmitSprite(
            IntPtr ctx, uint textureID,
            float posX, float posY, float sizeX, float sizeY, float rotation,
            float colorR, float colorG, float colorB,
            float uvOffsetX, float uvOffsetY, float uvSizeX, float uvSizeY,
            int layer, int order);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Render_SubmitCustom(
            IntPtr ctx, RenderCallback callback, IntPtr userData, int layer, int order);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Render_SubmitClear(IntPtr ctx, float r, float g, float b);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Render_GetCommandCount(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Render_ClearCommands(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Render_ExecuteFrame(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Render_GetViewProjectionMatrix(IntPtr ctx, [Out] float[] outMatrix);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Render_GetCameraBounds(
            IntPtr ctx, out float outMinX, out float outMinY, out float outMaxX, out float outMaxY);

        #endregion

        #region Entity Management API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern EntityId CreateEntity(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyEntity(IntPtr ctx, EntityId eid);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_GetAllEntities(IntPtr ctx, [Out] uint[] outArr, int maxCount);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_GetEntityWorldPosition(IntPtr ctx, EntityId eid, out float outX, out float outY);

        #endregion

        #region Component Reflection API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int HasComponent(IntPtr ctx, EntityId eid, [MarshalAs(UnmanagedType.LPStr)] string compName);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetComponentField(
            IntPtr ctx, EntityId eid,
            [MarshalAs(UnmanagedType.LPStr)] string comp,
            [MarshalAs(UnmanagedType.LPStr)] string field,
            IntPtr outBuf, int bufSize);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetComponentField(
            IntPtr ctx, EntityId eid,
            [MarshalAs(UnmanagedType.LPStr)] string comp,
            [MarshalAs(UnmanagedType.LPStr)] string field,
            IntPtr data, int dataSize);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetComponentJson(
            IntPtr ctx, EntityId eid,
            [MarshalAs(UnmanagedType.LPStr)] string comp,
            [MarshalAs(UnmanagedType.LPStr)] string jsonStr);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetComponentJson(
            IntPtr ctx, EntityId eid,
            [MarshalAs(UnmanagedType.LPStr)] string compName,
            [Out] byte[] outJson, int outJsonSize);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int RemoveComponent(
            IntPtr ctx, EntityId eid,
            [MarshalAs(UnmanagedType.LPStr)] string compName);

        #endregion

        #region Script Data API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetScriptDataValue(
            IntPtr ctx, EntityId eid,
            [MarshalAs(UnmanagedType.LPStr)] string key,
            [Out] byte[] outJson, int outJsonSize);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetScriptDataValue(
            IntPtr ctx, EntityId eid,
            [MarshalAs(UnmanagedType.LPStr)] string key,
            [MarshalAs(UnmanagedType.LPStr)] string jsonValue);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int RemoveScriptDataValue(
            IntPtr ctx, EntityId eid,
            [MarshalAs(UnmanagedType.LPStr)] string key);

        #endregion

        #region Prefab System API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern EntityId Prefab_InstantiateAtTile(
            IntPtr ctx,
            [MarshalAs(UnmanagedType.LPStr)] string prefabName,
            int tileX, int tileY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern EntityId InstantiatePrefab(
            IntPtr ctx,
            [MarshalAs(UnmanagedType.LPStr)] string prefabName,
            float worldX, float worldY);

        #endregion

        #region Event System API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Script_SubscribeEvent(
            IntPtr ctx,
            [MarshalAs(UnmanagedType.LPStr)] string eventName,
            ScriptEventCallback callback, IntPtr userData);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Script_PublishEvent(
            IntPtr ctx,
            [MarshalAs(UnmanagedType.LPStr)] string eventName,
            IntPtr payload, int payloadSize);

        #endregion

        #region Camera API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetPlayerEntity(IntPtr ctx, EntityId player);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetCameraTarget(IntPtr ctx, EntityId target);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_ClearCameraTarget(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetCameraPosition(IntPtr ctx, float worldX, float worldY);

        #endregion

        #region Overlay Rendering API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_OverlayClear(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_OverlayRect(
            IntPtr ctx, float wx, float wy, float w, float h, uint colourRGBA);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_OverlayPresent();

        #endregion

        #region Pathfinding API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Engine_FindPath(
            IntPtr ctx, int startX, int startY, int targetX, int targetY, int maxRange);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Engine_FindPathAdvanced(
            IntPtr ctx, int startX, int startY, int targetX, int targetY,
            int maxRange, EntityId tilemapLayer);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_FreeString(IntPtr str);

        #endregion

        #region Scene Management API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SceneManager_SaveScene(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string path);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SceneManager_LoadScene(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string path,
            out uint outPlayer, out float outPlayerX, out float outPlayerY, out uint outMainTilemap);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SceneManager_LoadTilemap(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string path,
            float positionX, float positionY, out uint outTilemap);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool SceneManager_SaveTilemap(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string path, uint tilemapEntity);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SceneManager_GetSupportedFormatsCount(
            IntPtr ctx, [MarshalAs(UnmanagedType.I1)] bool forLoading);

        #endregion

        #region Scene Hierarchy API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern EntityId SceneHierarchy_CreateGameObject(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string name, EntityId parent);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SceneHierarchy_SetParent(IntPtr ctx, EntityId child, EntityId parent);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SceneHierarchy_GetChildren(
            IntPtr ctx, EntityId parent, [Out] uint[] outChildren, int maxCount);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern EntityId SceneHierarchy_GetParent(IntPtr ctx, EntityId child);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SceneHierarchy_GetRootObjects(
            IntPtr ctx, [Out] uint[] outRoots, int maxCount);

        #endregion

        #region Selection API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Selection_SelectEntity(IntPtr ctx, EntityId entity);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Selection_AddToSelection(IntPtr ctx, EntityId entity);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Selection_DeselectAll(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Selection_GetSelectedEntities(
            IntPtr ctx, [Out] uint[] outEntities, int maxCount);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Selection_SelectInBounds(
            IntPtr ctx, float minX, float minY, float maxX, float maxY);

        #endregion

        #region Layer Management API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Layer_Create(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Layer_Remove(IntPtr ctx, int layerId);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Layer_SetVisible(IntPtr ctx, int layerId, int visible);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Layer_SetEntityLayer(IntPtr ctx, EntityId entity, int layerId);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Layer_GetEntityLayer(IntPtr ctx, EntityId entity);

        #endregion

        #region Command System API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Command_Execute(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string commandJson);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Command_Undo(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Command_Redo(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Command_CanUndo(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Command_CanRedo(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Command_GetUndoDescription(
            IntPtr ctx, [Out] byte[] outDescription, int bufferSize);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Command_GetRedoDescription(
            IntPtr ctx, [Out] byte[] outDescription, int bufferSize);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Command_GetHistorySize(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Command_SetMaxHistorySize(IntPtr ctx, int maxSize);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Command_ClearHistory(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Command_MoveSelection(IntPtr ctx, float deltaX, float deltaY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Command_DeleteSelection(IntPtr ctx);

        #endregion

        #region Grid Operations API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Grid_SnapPosition(
            IntPtr ctx, float inX, float inY, out float outX, out float outY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Grid_GetTileSize(IntPtr ctx);

        #endregion

        #region Coordinate Conversion API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Coord_WorldToTile(
            IntPtr ctx, float worldX, float worldY, out int outTileX, out int outTileY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Coord_TileToWorld(
            IntPtr ctx, int tileX, int tileY, out float outWorldX, out float outWorldY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Coord_GetTileSize(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Coord_SetTileSize(IntPtr ctx, float tileSize);

        #endregion

        #region Helper Methods

        /// <summary>Submit a sprite with default values</summary>
        public static void SubmitSprite(IntPtr ctx, uint textureID, float x, float y,
                                       float width, float height, RenderLayer layer = RenderLayer.Entities,
                                       int order = 0)
        {
            Render_SubmitSprite(ctx, textureID, x, y, width, height, 0f,
                               1f, 1f, 1f, 0f, 0f, 1f, 1f, (int)layer, order);
        }

        /// <summary>Submit a colored rectangle</summary>
        public static void SubmitColoredRect(IntPtr ctx, float x, float y, float width, float height,
                                           float r, float g, float b, RenderLayer layer = RenderLayer.UI,
                                           int order = 0)
        {
            Render_SubmitSprite(ctx, 0, x, y, width, height, 0f, r, g, b, 0f, 0f, 1f, 1f,
                               (int)layer, order);
        }

        /// <summary>Helper to get component field as typed value</summary>
        public static T GetComponentField<T>(IntPtr ctx, EntityId entity, string component, string field) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                int result = GetComponentField(ctx, entity, component, field, buffer, size);
                if (result > 0)
                {
                    return Marshal.PtrToStructure<T>(buffer);
                }
                return default(T);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>Helper to set component field from typed value</summary>
        public static bool SetComponentField<T>(IntPtr ctx, EntityId entity, string component, string field, T value) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(value, buffer, false);
                int result = SetComponentField(ctx, entity, component, field, buffer, size);
                return result == 0;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>Helper to get string from native function</summary>
        public static string GetStringFromBuffer(byte[] buffer, int length)
        {
            if (buffer == null || length <= 0) return string.Empty;
            return Encoding.UTF8.GetString(buffer, 0, length);
        }

        /// <summary>Helper to marshal string to path finding result</summary>
        public static string GetPathfindingResult(IntPtr strPtr)
        {
            if (strPtr == IntPtr.Zero) return string.Empty;
            try
            {
                return Marshal.PtrToStringUTF8(strPtr) ?? string.Empty;
            }
            finally
            {
                Engine_FreeString(strPtr);
            }
        }

        #endregion

        #region Imgui

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ImGui_Initialize(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGui_Shutdown(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ImGui_ProcessEvent(IntPtr ctx, IntPtr sdlEvent);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGui_NewFrame(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGui_Render(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ImGui_WantCaptureMouse(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ImGui_WantCaptureKeyboard(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGui_SetDisplaySize(IntPtr ctx, float width, float height);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGui_SetDockingEnabled(int enabled);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImGui_GetFontAwesome();

        #endregion

        #region Editor-Specific Engine Functions

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EngineInitEditor(IntPtr context, int argc, string[] argv);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EngineIterateEditor(IntPtr context);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EngineInitRendering(IntPtr context, int width, int height);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void EngineRenderFrame(IntPtr context);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void EngineSetEditorCamera(IntPtr context, float x, float y, float zoom, float width, float height);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int EngineCanRender(IntPtr context);

        /// <summary>
        /// Initialize engine in editor mode with specific settings
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_InitializeEditor(IntPtr ctx, int width, int height, int flags);

        /// <summary>
        /// Set viewport size for embedded rendering
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetViewportSize(IntPtr ctx, int width, int height);

        /// <summary>
        /// Get current viewport size
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_GetViewportSize(IntPtr ctx, out int width, out int height);

        /// <summary>
        /// Enable/disable editor-specific rendering features
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetEditorRenderFlags(IntPtr ctx, int flags);

        /// <summary>
        /// Get frame statistics
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_GetFrameStats(IntPtr ctx, out FrameStats stats);

        #endregion

        #region Entity Picking and Selection

        /// <summary>
        /// Pick entity at screen coordinates
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern EntityId Engine_PickEntity(IntPtr ctx, int screenX, int screenY);

        /// <summary>
        /// Pick multiple entities in a rectangular region
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_PickEntitiesInRect(IntPtr ctx, int x1, int y1, int x2, int y2,
            [Out] uint[] outEntities, int maxEntities);

        /// <summary>
        /// Get entity bounding box in screen space
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_GetEntityScreenBounds(IntPtr ctx, EntityId entity,
            out float minX, out float minY, out float maxX, out float maxY);

        /// <summary>
        /// Get entity bounding box in world space
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_GetEntityWorldBounds(IntPtr ctx, EntityId entity,
            out float minX, out float minY, out float maxX, out float maxY);

        #endregion

        #region Camera and Viewport Controls

        /// <summary>
        /// Set camera zoom level
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetCameraZoom(IntPtr ctx, float zoom);

        /// <summary>
        /// Get current camera zoom level
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Engine_GetCameraZoom(IntPtr ctx);

        /// <summary>
        /// Convert screen coordinates to world coordinates
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_ScreenToWorld(IntPtr ctx, int screenX, int screenY,
            out float worldX, out float worldY);

        /// <summary>
        /// Convert world coordinates to screen coordinates
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_WorldToScreen(IntPtr ctx, float worldX, float worldY,
            out int screenX, out int screenY);

        /// <summary>
        /// Get camera view matrix
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_GetCameraViewMatrix(IntPtr ctx, [Out] float[] matrix);

        /// <summary>
        /// Get camera projection matrix
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_GetCameraProjectionMatrix(IntPtr ctx, [Out] float[] matrix);

        #endregion

        #region Grid and Gizmo Rendering

        /// <summary>
        /// Enable/disable grid rendering
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetGridVisible(IntPtr ctx, int visible);

        /// <summary>
        /// Set grid properties
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetGridProperties(IntPtr ctx, float size, int subdivisions,
            float colorR, float colorG, float colorB, float alpha);

        /// <summary>
        /// Render selection outline around entity
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_RenderSelectionOutline(IntPtr ctx, EntityId entity,
            float colorR, float colorG, float colorB, float width);

        /// <summary>
        /// Render transformation gizmo at position
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_RenderTransformGizmo(IntPtr ctx, float worldX, float worldY,
            float scale, int gizmoType);

        #endregion

        #region Debug and Visualization

        /// <summary>
        /// Enable/disable debug rendering
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetDebugRenderEnabled(IntPtr ctx, int enabled);

        /// <summary>
        /// Set debug render flags
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetDebugRenderFlags(IntPtr ctx, int flags);

        /// <summary>
        /// Draw debug line in world space
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_DrawDebugLine(IntPtr ctx, float x1, float y1, float x2, float y2,
            float colorR, float colorG, float colorB, float width);

        /// <summary>
        /// Draw debug circle in world space
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_DrawDebugCircle(IntPtr ctx, float centerX, float centerY, float radius,
            float colorR, float colorG, float colorB, int segments);

        /// <summary>
        /// Draw debug rectangle in world space
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_DrawDebugRect(IntPtr ctx, float x, float y, float width, float height,
            float colorR, float colorG, float colorB, int filled);

        #endregion

        #region Performance and Profiling

        /// <summary>
        /// Get detailed performance metrics
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_GetPerformanceMetrics(IntPtr ctx, out PerformanceMetrics metrics);

        /// <summary>
        /// Start performance profiling section
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_BeginProfileSection(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string name);

        /// <summary>
        /// End performance profiling section
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_EndProfileSection(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string name);

        /// <summary>
        /// Get profiling results
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_GetProfilingResults(IntPtr ctx, [Out] ProfileSection[] sections, int maxSections);

        #endregion

        #region Asset Management

        /// <summary>
        /// Load texture and return handle
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Engine_LoadTexture(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string path);

        /// <summary>
        /// Unload texture by handle
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_UnloadTexture(IntPtr ctx, uint textureHandle);

        /// <summary>
        /// Get texture info
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_GetTextureInfo(IntPtr ctx, uint textureHandle, out TextureInfo info);

        /// <summary>
        /// Reload asset by path
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_ReloadAsset(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string path);

        #endregion

        #region Entity Manipulation

        /// <summary>
        /// Clone entity with all components
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern EntityId Engine_CloneEntity(IntPtr ctx, EntityId source);

        /// <summary>
        /// Move entity in hierarchy
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_MoveEntityInHierarchy(IntPtr ctx, EntityId entity, EntityId newParent, int siblingIndex);

        /// <summary>
        /// Get entity depth in hierarchy
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_GetEntityDepth(IntPtr ctx, EntityId entity);

        /// <summary>
        /// Check if entity is ancestor of another
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_IsEntityAncestorOf(IntPtr ctx, EntityId ancestor, EntityId descendant);

        #endregion
        // Add these to your EngineInterop.cs class

        #region OpenGL Context and Framebuffer Management

        /// <summary>
        /// Initialize OpenGL context sharing with external context
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_InitializeSharedGL(IntPtr ctx, IntPtr sharedContext);

        /// <summary>
        /// Create render target framebuffer with color and depth textures
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Engine_CreateRenderTarget(IntPtr ctx, int width, int height,
            out uint colorTexture, out uint depthTexture);

        /// <summary>
        /// Destroy render target and associated textures
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_DestroyRenderTarget(IntPtr ctx, uint framebuffer,
            uint colorTexture, uint depthTexture);

        /// <summary>
        /// Resize render target framebuffer
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_ResizeRenderTarget(IntPtr ctx, uint framebuffer,
            uint colorTexture, uint depthTexture, int newWidth, int newHeight);

        /// <summary>
        /// Set engine to render to specific framebuffer
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetRenderTarget(IntPtr ctx, uint framebuffer, int width, int height);

        /// <summary>
        /// Restore default framebuffer (screen)
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_RestoreDefaultFramebuffer(IntPtr ctx);

        /// <summary>
        /// Render one frame to current render target
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_RenderToTarget(IntPtr ctx, IntPtr nativeWindow, int width, int height);

        /// <summary>
        /// Render frame to specific framebuffer
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_RenderToFramebuffer(IntPtr ctx, uint framebuffer, int width, int height);

        /// <summary>
        /// Blit framebuffer to screen or another framebuffer
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_BlitFramebuffer(IntPtr ctx, uint srcFBO, uint dstFBO,
            int srcX0, int srcY0, int srcX1, int srcY1,
            int dstX0, int dstY0, int dstX1, int dstY1, uint mask, uint filter);

        #endregion

        #region OpenGL State Management

        /// <summary>
        /// Get current OpenGL context from engine
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Engine_GetGLContext(IntPtr ctx);

        /// <summary>
        /// Make engine's OpenGL context current
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_MakeGLContextCurrent(IntPtr ctx);

        /// <summary>
        /// Share OpenGL resources with external context
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_ShareGLContext(IntPtr ctx, IntPtr externalContext);

        /// <summary>
        /// Sync OpenGL state between contexts
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SyncGLState(IntPtr ctx);

        /// <summary>
        /// Get OpenGL texture handle from engine texture ID
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Engine_GetGLTextureHandle(IntPtr ctx, uint engineTextureId);

        #endregion

        #region Texture Management

        /// <summary>
        /// Create OpenGL texture with specific format
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Engine_CreateGLTexture(IntPtr ctx, int width, int height,
            uint internalFormat, uint format, uint type);

        /// <summary>
        /// Update texture data
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_UpdateTextureData(IntPtr ctx, uint textureId,
            int width, int height, uint format, uint type, IntPtr data);

        /// <summary>
        /// Get texture data (for readback)
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_GetTextureData(IntPtr ctx, uint textureId,
            uint format, uint type, IntPtr outData, int bufferSize);

        #endregion

        #region Editor-Specific Rendering

        /// <summary>
        /// Begin editor frame rendering
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_BeginEditorFrame(IntPtr ctx);

        /// <summary>
        /// End editor frame rendering
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_EndEditorFrame(IntPtr ctx);

        /// <summary>
        /// Set editor viewport transformation
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetEditorViewport(IntPtr ctx, int x, int y, int width, int height);

        /// <summary>
        /// Render scene with editor overlays
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_RenderSceneWithOverlays(IntPtr ctx);

        #endregion

        #region Headless Mode and Context Sharing Extensions

        /// <summary>
        /// Check if engine supports external OpenGL context
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_SupportsExternalGL(IntPtr ctx);

        /// <summary>
        /// Get engine OpenGL capabilities
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_GetGLCapabilities(IntPtr ctx, [Out] byte[] capabilities, int bufferSize);

        /// <summary>
        /// Validate that shared context is compatible
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_ValidateSharedContext(IntPtr ctx, IntPtr externalContext);

        /// <summary>
        /// Get last OpenGL error from engine
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint Engine_GetLastGLError(IntPtr ctx);

        /// <summary>
        /// Set engine to use immediate mode rendering (for fallback)
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_SetImmediateModeRendering(IntPtr ctx, int enabled);

        /// <summary>
        /// Check if engine is running in headless mode
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Engine_IsHeadless(IntPtr ctx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetProcAddressDelegate(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string procName);

        // P/Invoke for Engine_BindOpenGLContext
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_BindOpenGLContext(GetProcAddressDelegate getProc);

        #endregion

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_RegisterRunInContext(RunInCtx_t cb);

        // (optional – rarely called from C#)
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Engine_RunInContext(RawAction fn, IntPtr user);


        #region Safe Component Access Helpers

        /// <summary>
        /// Safely get component JSON with error handling
        /// </summary>
        public static string GetComponentJsonSafe(IntPtr ctx, EntityId entity, string componentName)
        {
            try
            {
                var buffer = new byte[4096]; // Reasonable buffer size
                int result = GetComponentJson(ctx, entity, componentName, buffer, buffer.Length);

                if (result > 0 && result < buffer.Length)
                {
                    return System.Text.Encoding.UTF8.GetString(buffer, 0, result);
                }
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error getting component {componentName}: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// Safely set component JSON with error handling
        /// </summary>
        public static bool SetComponentJsonSafe(IntPtr ctx, EntityId entity, string componentName, string jsonData)
        {
            try
            {
                int result = SetComponentJson(ctx, entity, componentName, jsonData);
                return result == 0;
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error setting component {componentName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if entity is valid before operations
        /// </summary>
        public static bool IsEntityValidSafe(IntPtr ctx, EntityId entity)
        {
            try
            {
                return entity.IsValid && HasComponent(ctx, entity, "TagComponent") != 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }


}