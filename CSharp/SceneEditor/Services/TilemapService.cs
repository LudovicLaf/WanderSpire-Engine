using ReactiveUI;
using SceneEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WanderSpire.Scripting;

namespace SceneEditor.Services
{
    /// <summary>
    /// Service for managing tilemaps in the scene
    /// </summary>
    public class TilemapService : ReactiveObject
    {
        private readonly EditorEngine _engine;
        private readonly ObservableCollection<TilemapLayer> _layers = new();
        private TilemapLayer? _activeLayer;
        private EntityId _activeTilemapEntity = EntityId.Invalid;

        public IReadOnlyList<TilemapLayer> Layers => _layers;

        public TilemapLayer? ActiveLayer
        {
            get => _activeLayer;
            set => this.RaiseAndSetIfChanged(ref _activeLayer, value);
        }

        public EntityId ActiveTilemapEntity
        {
            get => _activeTilemapEntity;
            set => this.RaiseAndSetIfChanged(ref _activeTilemapEntity, value);
        }

        public event EventHandler? LayersChanged;

        public TilemapService(EditorEngine engine)
        {
            _engine = engine;
            InitializeDefaultTilemap();
        }

        /// <summary>
        /// Create a default tilemap if none exists
        /// </summary>
        private void InitializeDefaultTilemap()
        {
            try
            {
                if (_engine?.IsInitialized == true)
                {
                    // Create main tilemap entity
                    var tilemapEntity = TilemapInterop.Tilemap_Create(_engine.Context, "Main Tilemap");
                    if (tilemapEntity.IsValid)
                    {
                        ActiveTilemapEntity = tilemapEntity;

                        // Create default layer
                        var defaultLayer = CreateLayer("Terrain");
                        if (defaultLayer != null)
                        {
                            ActiveLayer = defaultLayer;
                        }

                        Console.WriteLine("[TilemapService] Created default tilemap and terrain layer");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilemapService] InitializeDefaultTilemap error: {ex}");
            }
        }

        /// <summary>
        /// Create a new tilemap layer
        /// </summary>
        public TilemapLayer? CreateLayer(string name)
        {
            try
            {
                if (!ActiveTilemapEntity.IsValid || _engine?.IsInitialized != true)
                    return null;

                var layerEntity = TilemapInterop.Tilemap_CreateLayer(_engine.Context, ActiveTilemapEntity, name);
                if (layerEntity.IsValid)
                {
                    var layer = new TilemapLayer
                    {
                        Name = name,
                        EntityId = layerEntity,
                        IsVisible = true,
                        IsLocked = false,
                        Opacity = 1.0f,
                        SortOrder = _layers.Count
                    };

                    _layers.Add(layer);
                    LayersChanged?.Invoke(this, EventArgs.Empty);

                    Console.WriteLine($"[TilemapService] Created layer: {name}");
                    return layer;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilemapService] CreateLayer error: {ex}");
            }

            return null;
        }

        /// <summary>
        /// Set a tile in the active layer
        /// </summary>
        public void SetTile(int tileX, int tileY, int tileId)
        {
            try
            {
                if (ActiveLayer?.EntityId.IsValid == true && _engine?.IsInitialized == true)
                {
                    TilemapInterop.Tilemap_SetTile(_engine.Context, ActiveLayer.EntityId, tileX, tileY, tileId);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilemapService] SetTile error: {ex}");
            }
        }

        /// <summary>
        /// Get a tile from the active layer
        /// </summary>
        public int GetTile(int tileX, int tileY)
        {
            try
            {
                if (ActiveLayer?.EntityId.IsValid == true && _engine?.IsInitialized == true)
                {
                    return TilemapInterop.Tilemap_GetTile(_engine.Context, ActiveLayer.EntityId, tileX, tileY);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilemapService] GetTile error: {ex}");
            }

            return 0;
        }

        /// <summary>
        /// Refresh layers from engine
        /// </summary>
        public void RefreshLayers()
        {
            try
            {
                _layers.Clear();

                if (ActiveTilemapEntity.IsValid && _engine?.IsInitialized == true)
                {
                    var layerIds = new uint[10];
                    int count = TilemapInterop.TilemapLayer_GetAllInTilemap(_engine.Context, ActiveTilemapEntity, layerIds, layerIds.Length);

                    for (int i = 0; i < count; i++)
                    {
                        var layerId = new EntityId { id = layerIds[i] };
                        var nameBuffer = new byte[256];

                        if (TilemapInterop.TilemapLayer_GetInfo(_engine.Context, layerId, nameBuffer, nameBuffer.Length,
                            out int visible, out int locked, out float opacity, out int sortOrder) != 0)
                        {
                            var name = System.Text.Encoding.UTF8.GetString(nameBuffer).TrimEnd('\0');

                            var layer = new TilemapLayer
                            {
                                Name = name,
                                EntityId = layerId,
                                IsVisible = visible != 0,
                                IsLocked = locked != 0,
                                Opacity = opacity,
                                SortOrder = sortOrder
                            };

                            _layers.Add(layer);
                        }
                    }
                }

                LayersChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[TilemapService] RefreshLayers error: {ex}");
            }
        }
    }




}
