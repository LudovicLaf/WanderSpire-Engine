// SceneEditor/Rendering/EditorRenderer.cs — Modern pipeline WITH proper Begin/End frame
using Avalonia.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using WanderSpire.Scripting;

namespace SceneEditor.Rendering
{
    public sealed class EditorRenderer : IDisposable
    {
        private readonly IntPtr _ctx;
        private readonly GlInterface _gl;
        private bool _init;
        private bool _disposed;

        // viewport
        private int _vpW = 800;
        private int _vpH = 600;

        // grid
        private const float GRID = 32f;
        private readonly Vector3 _gridCol = new(0.3f, 0.3f, 0.3f);
        private readonly Vector3 _axisCol = new(0.8f, 0.8f, 0.8f);

        // textures
        private readonly Dictionary<string, uint> _tex = new();
        private uint _white;

        public RenderStats Stats { get; } = new();

        public EditorRenderer(IntPtr engineCtx, GlInterface gl)
        {
            _ctx = engineCtx;
            _gl = gl;
        }

        #region lifecycle
        public bool Initialize()
        {
            if (_init) return true;
            try
            {
                _white = CreateWhiteTex();
                LoadDefaults();
                _init = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EditorRenderer] init failed: {ex}");
                return false;
            }
        }

        public void SetViewportSize(int w, int h)
        {
            _vpW = Math.Max(1, w);
            _vpH = Math.Max(1, h);
        }
        #endregion

        #region frame
        public void BeginFrame(EditorCamera cam)
        {
            if (!_init) return;

            Stats.Reset();

            // clear previous command list & start frame in engine
            try
            {
                EngineInterop.Render_ClearCommands(_ctx);
                EngineInterop.Engine_BeginEditorFrame(_ctx);
                EngineInterop.Engine_SetEditorViewport(_ctx, 0, 0, _vpW, _vpH);
                EngineInterop.EngineSetEditorCamera(_ctx, cam.X, cam.Y, cam.Zoom, _vpW, _vpH);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EditorRenderer] BeginFrame native error: {ex}");
            }

            _gl.Viewport(0, 0, _vpW, _vpH);
            _gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        }

        public void RenderScene(SceneRenderData data)
        {
            if (!_init) return;

            // clear BG — engine handles clear colour when executing frame
            EngineInterop.Render_SubmitClear(_ctx, 0.15f, 0.18f, 0.22f);
            Stats.DrawCalls++;

            if (data.ShowGrid) DrawGrid(data.Camera);
            DrawTilemaps(data.Camera);
            DrawEntities(data.AllEntities);
            if (data.ShowGizmos) DrawSelection(data.SelectedEntities);
        }

        public void EndFrame()
        {
            if (!_init) return;
            try
            {
                EngineInterop.Engine_EndEditorFrame(_ctx);
                EngineInterop.Render_ExecuteFrame(_ctx);
                _gl.Flush();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EditorRenderer] EndFrame error: {ex}");
            }
        }
        #endregion

        #region drawing helpers
        private void DrawGrid(EditorCamera cam)
        {
            var b = ViewBounds(cam);
            float sx = MathF.Floor(b.MinX / GRID) * GRID;
            float sy = MathF.Floor(b.MinY / GRID) * GRID;
            float ex = MathF.Ceiling(b.MaxX / GRID) * GRID;
            float ey = MathF.Ceiling(b.MaxY / GRID) * GRID;

            for (float x = sx; x <= ex; x += GRID)
            {
                EngineInterop.Engine_DrawDebugLine(_ctx, x, b.MinY, x, b.MaxY, _gridCol.X, _gridCol.Y, _gridCol.Z, 1f);
                Stats.DrawCalls++;
            }
            for (float y = sy; y <= ey; y += GRID)
            {
                EngineInterop.Engine_DrawDebugLine(_ctx, b.MinX, y, b.MaxX, y, _gridCol.X, _gridCol.Y, _gridCol.Z, 1f);
                Stats.DrawCalls++;
            }
            if (b.MinX <= 0 && b.MaxX >= 0)
            {
                EngineInterop.Engine_DrawDebugLine(_ctx, 0, b.MinY, 0, b.MaxY, _axisCol.X, _axisCol.Y, _axisCol.Z, 2f);
                Stats.DrawCalls++;
            }
            if (b.MinY <= 0 && b.MaxY >= 0)
            {
                EngineInterop.Engine_DrawDebugLine(_ctx, b.MinX, 0, b.MaxX, 0, _axisCol.X, _axisCol.Y, _axisCol.Z, 2f);
                Stats.DrawCalls++;
            }
        }

        private void DrawTilemaps(EditorCamera cam)
        {
            uint[] buf = new uint[512];
            int cnt = EngineInterop.Engine_GetAllEntities(_ctx, buf, buf.Length);
            var b = ViewBounds(cam);
            int vMinX = (int)MathF.Floor(b.MinX / GRID);
            int vMinY = (int)MathF.Floor(b.MinY / GRID);
            int vMaxX = (int)MathF.Ceiling(b.MaxX / GRID);
            int vMaxY = (int)MathF.Ceiling(b.MaxY / GRID);

            for (int i = 0; i < cnt; ++i)
            {
                var eid = new EntityId { id = buf[i] };
                if (EngineInterop.HasComponent(_ctx, eid, "TilemapComponent") == 0) continue;
                if (TilemapInterop.Tilemap_GetBounds(_ctx, eid, out int minX, out int minY, out int maxX, out int maxY) == 0) continue;
                int x0 = Math.Max(minX, vMinX);
                int y0 = Math.Max(minY, vMinY);
                int x1 = Math.Min(maxX, vMaxX);
                int y1 = Math.Min(maxY, vMaxY);

                for (int ty = y0; ty <= y1; ++ty)
                    for (int tx = x0; tx <= x1; ++tx)
                    {
                        int tid = TilemapInterop.Tilemap_GetTile(_ctx, eid, tx, ty);
                        if (tid == 0) continue;
                        var c = TileColor(tid);
                        EngineInterop.SubmitColoredRect(_ctx, tx * GRID, ty * GRID, GRID, GRID, c.X, c.Y, c.Z, RenderLayer.Terrain);
                        Stats.RenderedEntityCount++;
                        Stats.DrawCalls++;
                    }
            }
        }

        private void DrawEntities(Entity[] list)
        {
            foreach (var e in list)
            {
                if (!e.IsValid) continue;
                EngineInterop.Engine_GetEntityWorldPosition(_ctx, new EntityId { id = (uint)e.Id }, out float x, out float y);
                EngineInterop.SubmitColoredRect(_ctx, x - 8, y - 8, 16, 16, 1f, 0.5f, 0.5f, RenderLayer.Entities);
                Stats.RenderedEntityCount++;
                Stats.DrawCalls++;
            }
        }

        private void DrawSelection(Entity[] sel)
        {
            foreach (var e in sel)
            {
                if (!e.IsValid) continue;
                EngineInterop.Engine_RenderSelectionOutline(_ctx, new EntityId { id = (uint)e.Id }, 1f, 0.6f, 0f, 2f);
                Stats.DrawCalls++;
            }
        }
        #endregion

        #region util
        private ViewportBounds ViewBounds(EditorCamera cam)
        {
            float hw = _vpW / (2f * cam.Zoom);
            float hh = _vpH / (2f * cam.Zoom);
            return new ViewportBounds { MinX = cam.X - hw, MinY = cam.Y - hh, MaxX = cam.X + hw, MaxY = cam.Y + hh };
        }

        private static Vector3 TileColor(int id) => id switch
        {
            1 => new(0.2f, 0.8f, 0.2f),
            2 => new(0.6f, 0.4f, 0.2f),
            3 => new(0.5f, 0.5f, 0.5f),
            4 => new(0.2f, 0.4f, 0.8f),
            5 => new(0.8f, 0.8f, 0.4f),
            _ => new(0.8f, 0.8f, 0.8f)
        };

        private uint CreateWhiteTex()
        {
            byte[] pix = { 255, 255, 255, 255 };
            uint id = EngineInterop.Engine_CreateGLTexture(_ctx, 1, 1, 0x1908, 0x1908, 0x1401);
            unsafe { fixed (byte* p = pix) EngineInterop.Engine_UpdateTextureData(_ctx, id, 1, 1, 0x1908, 0x1401, (IntPtr)p); }
            return id;
        }

        private void LoadDefaults()
        {
            foreach (var n in new[] { "grass", "sand", "road" })
            {
                uint t = EngineInterop.Engine_LoadTexture(_ctx, $"textures/terrain/{n}.png");
                if (t != 0) _tex[n] = t;
            }
        }
        #endregion

        #region GL consts
        private const int GL_COLOR_BUFFER_BIT = 0x00004000;
        private const int GL_DEPTH_BUFFER_BIT = 0x00000100;
        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _tex.Clear();
            _disposed = true;
        }
    }

    #region Supporting Types

    public class EditorCamera
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Zoom { get; set; } = 1f;
    }

    public class SceneRenderData
    {
        public EditorCamera Camera { get; set; } = new EditorCamera();
        public Entity[] AllEntities { get; set; } = Array.Empty<Entity>();
        public Entity[] SelectedEntities { get; set; } = Array.Empty<Entity>();
        public bool ShowGrid { get; set; } = true;
        public bool ShowGizmos { get; set; } = true;
    }

    public struct ViewportBounds
    {
        public float MinX, MinY, MaxX, MaxY;
    }

    public class RenderStats
    {
        public int EntityCount { get; set; }
        public int RenderedEntityCount { get; set; }
        public int SelectedCount { get; set; }
        public int DrawCalls { get; set; }
        public long FrameNumber { get; set; }

        public void Reset()
        {
            RenderedEntityCount = 0;
            DrawCalls = 0;
            FrameNumber++;
        }
    }

    #endregion
}
