using ReactiveUI;
using System;
using WanderSpire.Scripting;

namespace SceneEditor.Services
{
    /// <summary>
    /// Service for painting tiles with various brush types
    /// </summary>
    public class TilePaintingService : ReactiveObject
    {
        private readonly EditorEngine _engine;
        private readonly TilemapService _tilemapService;
        private readonly TilePaletteService _paletteService;
        private readonly CommandService _commandService;

        private BrushType _brushType = BrushType.Single;
        private int _brushSize = 1;
        private bool _isPainting = false;
        private (int x, int y) _lastPaintPosition = (-1, -1);

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

        public TilePaintingService(EditorEngine engine, TilemapService tilemapService,
            TilePaletteService paletteService, CommandService commandService)
        {
            _engine = engine;
            _tilemapService = tilemapService;
            _paletteService = paletteService;
            _commandService = commandService;
        }

        /// <summary>
        /// Begin painting at world coordinates
        /// </summary>
        public void BeginPaint(float worldX, float worldY)
        {
            try
            {
                if (_engine?.IsInitialized != true || _tilemapService.ActiveLayer?.EntityId.IsValid != true)
                    return;

                // Convert world coordinates to tile coordinates
                EngineInterop.Coord_WorldToTile(_engine.Context, worldX, worldY, out int tileX, out int tileY);

                IsPainting = true;
                _lastPaintPosition = (tileX, tileY);

                // Start tile painting session
                TilemapInterop.TilePaint_Begin(_engine.Context, _tilemapService.ActiveLayer.EntityId, tileX, tileY);

                // Paint the initial tile
                PaintTileAt(tileX, tileY);

                Console.WriteLine($"[TilePaintingService] Begin paint at tile ({tileX}, {tileY})");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintingService] BeginPaint error: {ex}");
            }
        }

        /// <summary>
        /// Continue painting at world coordinates
        /// </summary>
        public void ContinuePaint(float worldX, float worldY)
        {
            try
            {
                if (!IsPainting || _engine?.IsInitialized != true || _tilemapService.ActiveLayer?.EntityId.IsValid != true)
                    return;

                // Convert world coordinates to tile coordinates
                EngineInterop.Coord_WorldToTile(_engine.Context, worldX, worldY, out int tileX, out int tileY);

                // Only paint if we've moved to a different tile
                if (_lastPaintPosition.x != tileX || _lastPaintPosition.y != tileY)
                {
                    _lastPaintPosition = (tileX, tileY);

                    // Continue tile painting session
                    TilemapInterop.TilePaint_Continue(_engine.Context, _tilemapService.ActiveLayer.EntityId, tileX, tileY);

                    // Paint the tile
                    PaintTileAt(tileX, tileY);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintingService] ContinuePaint error: {ex}");
            }
        }

        /// <summary>
        /// End painting session
        /// </summary>
        public void EndPaint()
        {
            try
            {
                if (!IsPainting)
                    return;

                if (_engine?.IsInitialized == true && _tilemapService.ActiveLayer?.EntityId.IsValid == true)
                {
                    // End tile painting session
                    TilemapInterop.TilePaint_End(_engine.Context, _tilemapService.ActiveLayer.EntityId);
                }

                IsPainting = false;
                _lastPaintPosition = (-1, -1);

                Console.WriteLine("[TilePaintingService] End paint");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintingService] EndPaint error: {ex}");
            }
        }

        /// <summary>
        /// Paint tile at specific tile coordinates
        /// </summary>
        private void PaintTileAt(int tileX, int tileY)
        {
            try
            {
                if (_tilemapService.ActiveLayer?.EntityId.IsValid != true)
                    return;

                int selectedTileId = _paletteService.SelectedTileId;

                switch (BrushType)
                {
                    case BrushType.Single:
                        _tilemapService.SetTile(tileX, tileY, selectedTileId);
                        break;

                    case BrushType.Rectangle:
                        PaintRectangle(tileX, tileY, selectedTileId);
                        break;

                    case BrushType.Circle:
                        PaintCircle(tileX, tileY, selectedTileId);
                        break;

                    default:
                        _tilemapService.SetTile(tileX, tileY, selectedTileId);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintingService] PaintTileAt error: {ex}");
            }
        }

        /// <summary>
        /// Paint a rectangular area
        /// </summary>
        private void PaintRectangle(int centerX, int centerY, int tileId)
        {
            int halfSize = BrushSize / 2;

            for (int y = centerY - halfSize; y <= centerY + halfSize; y++)
            {
                for (int x = centerX - halfSize; x <= centerX + halfSize; x++)
                {
                    _tilemapService.SetTile(x, y, tileId);
                }
            }
        }

        /// <summary>
        /// Paint a circular area
        /// </summary>
        private void PaintCircle(int centerX, int centerY, int tileId)
        {
            float radius = BrushSize / 2f;
            int radiusInt = (int)Math.Ceiling(radius);

            for (int y = centerY - radiusInt; y <= centerY + radiusInt; y++)
            {
                for (int x = centerX - radiusInt; x <= centerX + radiusInt; x++)
                {
                    float distance = (float)Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    if (distance <= radius)
                    {
                        _tilemapService.SetTile(x, y, tileId);
                    }
                }
            }
        }

        /// <summary>
        /// Erase tile at world coordinates
        /// </summary>
        public void EraseTile(float worldX, float worldY)
        {
            try
            {
                if (_engine?.IsInitialized != true)
                    return;

                EngineInterop.Coord_WorldToTile(_engine.Context, worldX, worldY, out int tileX, out int tileY);
                _tilemapService.SetTile(tileX, tileY, 0); // 0 = empty tile

                Console.WriteLine($"[TilePaintingService] Erased tile at ({tileX}, {tileY})");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintingService] EraseTile error: {ex}");
            }
        }

        /// <summary>
        /// Sample tile at world coordinates (eyedropper tool)
        /// </summary>
        public void SampleTile(float worldX, float worldY)
        {
            try
            {
                if (_engine?.IsInitialized != true)
                    return;

                EngineInterop.Coord_WorldToTile(_engine.Context, worldX, worldY, out int tileX, out int tileY);
                int tileId = _tilemapService.GetTile(tileX, tileY);

                if (tileId > 0)
                {
                    _paletteService.SelectedTileId = tileId;
                    Console.WriteLine($"[TilePaintingService] Sampled tile ID {tileId} at ({tileX}, {tileY})");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaintingService] SampleTile error: {ex}");
            }
        }
    }
}
