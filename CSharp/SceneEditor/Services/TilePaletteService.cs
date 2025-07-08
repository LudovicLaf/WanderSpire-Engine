using ReactiveUI;
using SceneEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WanderSpire.Scripting;

namespace SceneEditor.Services
{
    /// <summary>
    /// Service for managing tile palettes and selection
    /// </summary>
    public class TilePaletteService : ReactiveObject
    {
        private readonly EditorEngine _engine;
        private readonly ObservableCollection<TileDefinition> _tiles = new();
        private int _selectedTileId = 1;
        private int _activePaletteId = -1;

        public IReadOnlyList<TileDefinition> Tiles => _tiles;

        public int SelectedTileId
        {
            get => _selectedTileId;
            set => this.RaiseAndSetIfChanged(ref _selectedTileId, value);
        }

        public int ActivePaletteId
        {
            get => _activePaletteId;
            set => this.RaiseAndSetIfChanged(ref _activePaletteId, value);
        }

        public event EventHandler? PaletteChanged;

        public TilePaletteService(EditorEngine engine)
        {
            _engine = engine;
            InitializeDefaultPalette();
        }

        /// <summary>
        /// Create a default tile palette with basic terrain tiles
        /// </summary>
        private void InitializeDefaultPalette()
        {
            try
            {
                if (_engine?.IsInitialized == true)
                {
                    // Create default palette
                    ActivePaletteId = TilemapInterop.TilePalette_Create(_engine.Context, "Default", "default", 32, 32);

                    if (ActivePaletteId >= 0)
                    {
                        // Add basic tiles
                        AddBasicTiles();
                        LoadPaletteTiles();

                        Console.WriteLine("[TilePaletteService] Created default palette with basic tiles");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaletteService] InitializeDefaultPalette error: {ex}");
                CreateFallbackPalette();
            }
        }

        /// <summary>
        /// Add basic tile definitions
        /// </summary>
        private void AddBasicTiles()
        {
            try
            {
                if (ActivePaletteId >= 0 && _engine?.IsInitialized == true)
                {
                    // Add basic terrain tiles
                    TilemapInterop.TilePalette_AddTile(_engine.Context, ActivePaletteId, 1, "Grass", "default", 0, 0, 1, 0);
                    TilemapInterop.TilePalette_AddTile(_engine.Context, ActivePaletteId, 2, "Dirt", "default", 1, 0, 1, 0);
                    TilemapInterop.TilePalette_AddTile(_engine.Context, ActivePaletteId, 3, "Stone", "default", 2, 0, 1, 1);
                    TilemapInterop.TilePalette_AddTile(_engine.Context, ActivePaletteId, 4, "Water", "default", 3, 0, 0, 2);
                    TilemapInterop.TilePalette_AddTile(_engine.Context, ActivePaletteId, 5, "Sand", "default", 4, 0, 1, 0);

                    // Register tile definitions in engine
                    TilemapInterop.TileDef_RegisterTile(_engine.Context, 1, "default", "grass", 1, 0);
                    TilemapInterop.TileDef_RegisterTile(_engine.Context, 2, "default", "dirt", 1, 0);
                    TilemapInterop.TileDef_RegisterTile(_engine.Context, 3, "default", "stone", 1, 1);
                    TilemapInterop.TileDef_RegisterTile(_engine.Context, 4, "default", "water", 0, 2);
                    TilemapInterop.TileDef_RegisterTile(_engine.Context, 5, "default", "sand", 1, 0);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaletteService] AddBasicTiles error: {ex}");
            }
        }

        /// <summary>
        /// Load tiles from the active palette
        /// </summary>
        private void LoadPaletteTiles()
        {
            try
            {
                _tiles.Clear();

                if (ActivePaletteId >= 0 && _engine?.IsInitialized == true)
                {
                    int tileCount = TilemapInterop.TilePalette_GetTileCount(_engine.Context, ActivePaletteId);

                    for (int i = 0; i < tileCount; i++)
                    {
                        var nameBuffer = new byte[256];

                        if (TilemapInterop.TilePalette_GetTileInfo(_engine.Context, ActivePaletteId, i, out int tileId,
                            nameBuffer, nameBuffer.Length, out int atlasX, out int atlasY, out int walkable, out int collisionType) != 0)
                        {
                            var name = System.Text.Encoding.UTF8.GetString(nameBuffer).TrimEnd('\0');

                            var tile = new TileDefinition
                            {
                                TileId = tileId,
                                Name = name,
                                AtlasX = atlasX,
                                AtlasY = atlasY,
                                IsWalkable = walkable != 0,
                                CollisionType = collisionType
                            };

                            _tiles.Add(tile);
                        }
                    }
                }

                PaletteChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilePaletteService] LoadPaletteTiles error: {ex}");
                CreateFallbackPalette();
            }
        }

        /// <summary>
        /// Create a fallback palette if engine operations fail
        /// </summary>
        private void CreateFallbackPalette()
        {
            _tiles.Clear();

            // Add basic fallback tiles
            _tiles.Add(new TileDefinition { TileId = 1, Name = "Grass", AtlasX = 0, AtlasY = 0, IsWalkable = true });
            _tiles.Add(new TileDefinition { TileId = 2, Name = "Dirt", AtlasX = 1, AtlasY = 0, IsWalkable = true });
            _tiles.Add(new TileDefinition { TileId = 3, Name = "Stone", AtlasX = 2, AtlasY = 0, IsWalkable = true, CollisionType = 1 });
            _tiles.Add(new TileDefinition { TileId = 4, Name = "Water", AtlasX = 3, AtlasY = 0, IsWalkable = false, CollisionType = 2 });
            _tiles.Add(new TileDefinition { TileId = 5, Name = "Sand", AtlasX = 4, AtlasY = 0, IsWalkable = true });

            PaletteChanged?.Invoke(this, EventArgs.Empty);

            Console.WriteLine("[TilePaletteService] Created fallback palette");
        }

        /// <summary>
        /// Get tile definition by ID
        /// </summary>
        public TileDefinition? GetTileDefinition(int tileId)
        {
            foreach (var tile in _tiles)
            {
                if (tile.TileId == tileId)
                    return tile;
            }
            return null;
        }
    }
}
