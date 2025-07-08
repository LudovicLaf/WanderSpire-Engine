// SceneEditor/Tools/TilePaintTool.cs - Enhanced Version with Proper Integration
using ReactiveUI;
using SceneEditor.Services;
using SceneEditor.ViewModels;
using System;
using WanderSpire.Scripting;

namespace SceneEditor.Tools
{
    /// <summary>
    /// Enhanced tile painting tool with proper tilemap integration
    /// </summary>
    public class TilePaintTool : EditorToolBase
    {
        private readonly TilemapService _tilemapService;
        private readonly TilePaletteService _tilePaletteService;
        private readonly TilePaintingService _tilePaintingService;

        private int _selectedTileId = 1;
        private BrushType _brushType = BrushType.Single;
        private int _brushSize = 1;
        private bool _isPainting = false;
        private (int x, int y) _lastPaintedTile = (-1, -1);

        public override string Name => "TilePaint";
        public override string DisplayName => "Paint Tiles";
        public override string Description => "Paint tiles on tilemap layers";
        public override string Icon => "\uf1fc"; // paint brush icon

        public int SelectedTileId
        {
            get => _selectedTileId;
            set => this.RaiseAndSetIfChanged(ref _selectedTileId, value);
        }

        public BrushType BrushType
        {
            get => _brushType;
            set => this.RaiseAndSetIfChanged(ref _brushType, value);
        }

        public int BrushSize
        {
            get => _brushSize;
            set => this.RaiseAndSetIfChanged(ref _brushSize, Math.Max(1, Math.Min(10, value)));
        }

        public bool IsPainting
        {
            get => _isPainting;
            private set => this.RaiseAndSetIfChanged(ref _isPainting, value);
        }

        public TilePaintTool(EditorEngine engine, GameObjectService sceneService, CommandService commandService)
            : base(engine, sceneService, commandService)
        {
            _tilemapService = App.GetService<TilemapService>();
            _tilePaletteService = App.GetService<TilePaletteService>();
            _tilePaintingService = App.GetService<TilePaintingService>();

            // Subscribe to palette changes
            _tilePaletteService.WhenAnyValue(x => x.SelectedTileId)
                .Subscribe(tileId => SelectedTileId = tileId);
        }

        public override void OnActivate()
        {
            base.OnActivate();
            Console.WriteLine("[TilePaintTool] Activated");

            // Ensure we have a valid tilemap to paint on
            EnsureTilemapExists();
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            // End any ongoing paint operation
            if (IsPainting)
            {
                EndPaintOperation();
            }

            Console.WriteLine("[TilePaintTool] Deactivated");
        }

        public override void OnMouseDown(float worldX, float worldY, ViewportInputModifiers modifiers)
        {
            try
            {
                if (!_engine.IsInitialized || _tilemapService.ActiveLayer == null)
                    return;

                // Convert world coordinates to tile coordinates
                var tilePos = WorldToTile(worldX, worldY);

                // Start painting operation
                BeginPaintOperation(tilePos.x, tilePos.y);

                Console.WriteLine($"[TilePaintTool] Started painting at tile ({tilePos.x}, {tilePos.y}) with tile ID {SelectedTileId}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] OnMouseDown error: {ex}");
            }
        }

        public override void OnDrag(float worldX, float worldY, ViewportInputModifiers modifiers)
        {
            try
            {
                if (!IsPainting || !_engine.IsInitialized || _tilemapService.ActiveLayer == null)
                    return;

                var tilePos = WorldToTile(worldX, worldY);

                // Only paint if we've moved to a different tile
                if (tilePos.x != _lastPaintedTile.x || tilePos.y != _lastPaintedTile.y)
                {
                    ContinuePaintOperation(tilePos.x, tilePos.y);
                    _lastPaintedTile = tilePos;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] OnDrag error: {ex}");
            }
        }

        public override void OnMouseUp(float worldX, float worldY, ViewportInputModifiers modifiers)
        {
            try
            {
                if (IsPainting)
                {
                    EndPaintOperation();
                    Console.WriteLine("[TilePaintTool] Finished painting");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] OnMouseUp error: {ex}");
            }
        }

        public override void OnRightClick(float worldX, float worldY, ViewportInputModifiers modifiers)
        {
            try
            {
                // Right-click to sample tile (eyedropper tool)
                var tilePos = WorldToTile(worldX, worldY);
                SampleTile(tilePos.x, tilePos.y);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] OnRightClick error: {ex}");
            }
        }

        public override void OnKeyDown(string key, ViewportInputModifiers modifiers)
        {
            try
            {
                switch (key.ToLower())
                {
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                        // Quick tile selection
                        if (int.TryParse(key, out int tileId))
                        {
                            SelectedTileId = tileId;
                            _tilePaletteService.SelectedTileId = tileId;
                            Console.WriteLine($"[TilePaintTool] Selected tile ID: {tileId}");
                        }
                        break;

                    case "b":
                        // Cycle brush types
                        BrushType = (BrushType)(((int)BrushType + 1) % Enum.GetValues<BrushType>().Length);
                        Console.WriteLine($"[TilePaintTool] Brush type: {BrushType}");
                        break;

                    case "bracketleft": // [
                        // Decrease brush size
                        BrushSize = Math.Max(1, BrushSize - 1);
                        Console.WriteLine($"[TilePaintTool] Brush size: {BrushSize}");
                        break;

                    case "bracketright": // ]
                        // Increase brush size
                        BrushSize = Math.Min(10, BrushSize + 1);
                        Console.WriteLine($"[TilePaintTool] Brush size: {BrushSize}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] OnKeyDown error: {ex}");
            }
        }

        private void BeginPaintOperation(int tileX, int tileY)
        {
            try
            {
                IsPainting = true;
                _lastPaintedTile = (tileX, tileY);

                // Start painting session through the service
                if (_tilePaintingService != null)
                {
                    TilemapInterop.TilePaint_Begin(_engine.Context, _tilemapService.ActiveLayer.EntityId, tileX, tileY);
                }

                // Paint the initial tile
                PaintTileAt(tileX, tileY);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] BeginPaintOperation error: {ex}");
            }
        }

        private void ContinuePaintOperation(int tileX, int tileY)
        {
            try
            {
                if (_tilePaintingService != null)
                {
                    TilemapInterop.TilePaint_Continue(_engine.Context, _tilemapService.ActiveLayer.EntityId, tileX, tileY);
                }

                PaintTileAt(tileX, tileY);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] ContinuePaintOperation error: {ex}");
            }
        }

        private void EndPaintOperation()
        {
            try
            {
                IsPainting = false;
                _lastPaintedTile = (-1, -1);

                if (_tilePaintingService != null && _tilemapService.ActiveLayer != null)
                {
                    TilemapInterop.TilePaint_End(_engine.Context, _tilemapService.ActiveLayer.EntityId);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] EndPaintOperation error: {ex}");
            }
        }

        private void PaintTileAt(int tileX, int tileY)
        {
            try
            {
                if (_tilemapService.ActiveLayer?.EntityId.IsValid != true)
                    return;

                switch (BrushType)
                {
                    case BrushType.Single:
                        PaintSingleTile(tileX, tileY);
                        break;

                    case BrushType.Rectangle:
                        PaintRectangleBrush(tileX, tileY);
                        break;

                    case BrushType.Circle:
                        PaintCircleBrush(tileX, tileY);
                        break;

                    default:
                        PaintSingleTile(tileX, tileY);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] PaintTileAt error: {ex}");
            }
        }

        private void PaintSingleTile(int tileX, int tileY)
        {
            TilemapInterop.Tilemap_SetTile(_engine.Context, _tilemapService.ActiveLayer.EntityId, tileX, tileY, SelectedTileId);
        }

        private void PaintRectangleBrush(int centerX, int centerY)
        {
            int halfSize = BrushSize / 2;

            for (int y = centerY - halfSize; y <= centerY + halfSize; y++)
            {
                for (int x = centerX - halfSize; x <= centerX + halfSize; x++)
                {
                    TilemapInterop.Tilemap_SetTile(_engine.Context, _tilemapService.ActiveLayer.EntityId, x, y, SelectedTileId);
                }
            }
        }

        private void PaintCircleBrush(int centerX, int centerY)
        {
            float radius = BrushSize / 2f;
            int radiusInt = (int)Math.Ceiling(radius);

            for (int y = centerY - radiusInt; y <= centerY + radiusInt; y++)
            {
                for (int x = centerX - radiusInt; x <= centerX + radiusInt; x++)
                {
                    float distance = MathF.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    if (distance <= radius)
                    {
                        TilemapInterop.Tilemap_SetTile(_engine.Context, _tilemapService.ActiveLayer.EntityId, x, y, SelectedTileId);
                    }
                }
            }
        }

        private void SampleTile(int tileX, int tileY)
        {
            try
            {
                if (_tilemapService.ActiveLayer?.EntityId.IsValid != true)
                    return;

                int sampledTileId = TilemapInterop.Tilemap_GetTile(_engine.Context, _tilemapService.ActiveLayer.EntityId, tileX, tileY);

                if (sampledTileId > 0)
                {
                    SelectedTileId = sampledTileId;
                    _tilePaletteService.SelectedTileId = sampledTileId;
                    Console.WriteLine($"[TilePaintTool] Sampled tile ID {sampledTileId} at ({tileX}, {tileY})");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] SampleTile error: {ex}");
            }
        }

        private (int x, int y) WorldToTile(float worldX, float worldY)
        {
            try
            {
                if (_engine.IsInitialized)
                {
                    EngineInterop.Coord_WorldToTile(_engine.Context, worldX, worldY, out int tileX, out int tileY);
                    return (tileX, tileY);
                }
                else
                {
                    // Fallback calculation
                    int tileX = (int)Math.Floor(worldX / _engine.TileSize);
                    int tileY = (int)Math.Floor(worldY / _engine.TileSize);
                    return (tileX, tileY);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] WorldToTile error: {ex}");
                return ((int)Math.Floor(worldX / 32f), (int)Math.Floor(worldY / 32f));
            }
        }

        private void EnsureTilemapExists()
        {
            try
            {
                if (_tilemapService.ActiveLayer == null)
                {
                    // Try to refresh layers from the engine
                    _tilemapService.RefreshLayers();

                    if (_tilemapService.ActiveLayer == null)
                    {
                        Console.WriteLine("[TilePaintTool] No active tilemap layer found - creating default");
                        // The engine should have created a default tilemap during initialization
                    }
                }

                if (_tilemapService.ActiveLayer != null)
                {
                    Console.WriteLine($"[TilePaintTool] Using tilemap layer: {_tilemapService.ActiveLayer.Name}");
                }
                else
                {
                    Console.Error.WriteLine("[TilePaintTool] No tilemap layer available for painting");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintTool] EnsureTilemapExists error: {ex}");
            }
        }

        /// <summary>
        /// Get tool status information for display
        /// </summary>
        public string GetStatusText()
        {
            if (!IsActive)
                return "Tile Paint Tool (Inactive)";

            return $"Tile Paint - ID: {SelectedTileId}, Brush: {BrushType} ({BrushSize}x{BrushSize})" +
                   (IsPainting ? " [PAINTING]" : "");
        }

        /// <summary>
        /// Get available tile IDs for quick selection
        /// </summary>
        public int[] GetAvailableTileIds()
        {
            return new int[] { 1, 2, 3, 4, 5 }; // Basic tile set
        }
    }
}