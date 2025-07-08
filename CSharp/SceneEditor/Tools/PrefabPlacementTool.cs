using ReactiveUI;
using SceneEditor.Services;
using SceneEditor.ViewModels;
using System;
using System.Linq;

namespace SceneEditor.Tools
{
    /// <summary>
    /// Updated tool for placing prefab instances in the scene - now uses GameObjectService
    /// </summary>
    public class PrefabPlacementTool : EditorToolBase
    {
        private readonly GameObjectService _gameObjectService; // Updated to use GameObjectService
        private string _selectedPrefabName = string.Empty;
        private bool _isPlacing = false;

        public override string Name => "PrefabPlace";
        public override string DisplayName => "Place Prefabs";
        public override string Description => "Place prefab instances in the scene";
        public override string Icon => "\uf1b3"; // cube icon

        public string SelectedPrefabName
        {
            get => _selectedPrefabName;
            set => this.RaiseAndSetIfChanged(ref _selectedPrefabName, value ?? string.Empty);
        }

        public bool IsPlacing
        {
            get => _isPlacing;
            private set => this.RaiseAndSetIfChanged(ref _isPlacing, value);
        }

        public string[] AvailablePrefabs => _gameObjectService.GetAvailablePrefabNames();

        public PrefabPlacementTool(EditorEngine engine, GameObjectService gameObjectService, CommandService commandService)
            : base(engine, gameObjectService, commandService) // Updated constructor
        {
            _gameObjectService = gameObjectService;

            // Subscribe to prefab library changes
            _gameObjectService.PrefabLibraryChanged += OnPrefabLibraryChanged;

            // Set default prefab if available
            var prefabs = AvailablePrefabs;
            if (prefabs.Length > 0)
            {
                SelectedPrefabName = prefabs.First();
            }
        }

        public override void OnActivate()
        {
            base.OnActivate();
            Console.WriteLine($"[PrefabPlacementTool] Activated - Selected prefab: {SelectedPrefabName}");
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            IsPlacing = false;
            Console.WriteLine("[PrefabPlacementTool] Deactivated");
        }

        public override void OnMouseDown(float worldX, float worldY, ViewportInputModifiers modifiers)
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedPrefabName))
                {
                    Console.WriteLine("[PrefabPlacementTool] No prefab selected for placement");
                    return;
                }

                if (!_engine.IsInitialized)
                {
                    Console.WriteLine("[PrefabPlacementTool] Engine not initialized");
                    return;
                }

                IsPlacing = true;

                // Place prefab at world position
                var node = _gameObjectService.PlacePrefabAtWorldPosition(SelectedPrefabName, worldX, worldY);
                if (node != null)
                {
                    Console.WriteLine($"[PrefabPlacementTool] Placed {SelectedPrefabName} at ({worldX:F1}, {worldY:F1})");

                    // Select the newly placed GameObject
                    _gameObjectService.SelectGameObject(node);
                }
                else
                {
                    Console.Error.WriteLine($"[PrefabPlacementTool] Failed to place prefab {SelectedPrefabName}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[PrefabPlacementTool] OnMouseDown error: {ex}");
            }
            finally
            {
                IsPlacing = false;
            }
        }

        public override void OnRightClick(float worldX, float worldY, ViewportInputModifiers modifiers)
        {
            try
            {
                // Right-click to sample/select existing prefab at location
                var entity = FindEntityAtPosition(worldX, worldY);
                if (entity?.Entity?.IsValid == true)
                {
                    var prefabComponent = entity.Entity.GetComponent<WanderSpire.Components.PrefabIdComponent>("PrefabIdComponent");
                    if (prefabComponent != null && !string.IsNullOrEmpty(prefabComponent.PrefabName))
                    {
                        SelectedPrefabName = prefabComponent.PrefabName;
                        Console.WriteLine($"[PrefabPlacementTool] Sampled prefab: {SelectedPrefabName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[PrefabPlacementTool] OnRightClick error: {ex}");
            }
        }

        public override void OnKeyDown(string key, ViewportInputModifiers modifiers)
        {
            try
            {
                switch (key.ToLower())
                {
                    case "tab":
                        // Cycle through available prefabs
                        CycleSelectedPrefab();
                        break;

                    case "delete":
                        // Delete prefab at mouse position (would need mouse tracking)
                        Console.WriteLine("[PrefabPlacementTool] Delete mode - click on prefab to remove");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[PrefabPlacementTool] OnKeyDown error: {ex}");
            }
        }

        /// <summary>
        /// Set the selected prefab for placement
        /// </summary>
        public void SetSelectedPrefab(string prefabName)
        {
            var prefabs = AvailablePrefabs;
            if (prefabs.Contains(prefabName))
            {
                SelectedPrefabName = prefabName;
                Console.WriteLine($"[PrefabPlacementTool] Selected prefab: {prefabName}");
            }
            else
            {
                Console.Error.WriteLine($"[PrefabPlacementTool] Prefab not found: {prefabName}");
            }
        }

        /// <summary>
        /// Cycle to the next available prefab
        /// </summary>
        public void CycleSelectedPrefab()
        {
            var prefabs = AvailablePrefabs;
            if (prefabs.Length == 0) return;

            var currentIndex = Array.IndexOf(prefabs, SelectedPrefabName);
            var nextIndex = (currentIndex + 1) % prefabs.Length;
            SelectedPrefabName = prefabs[nextIndex];

            Console.WriteLine($"[PrefabPlacementTool] Cycled to prefab: {SelectedPrefabName}");
        }

        /// <summary>
        /// Get tool status for display
        /// </summary>
        public string GetStatusText()
        {
            if (!IsActive)
                return "Prefab Placement Tool (Inactive)";

            if (string.IsNullOrEmpty(SelectedPrefabName))
                return "Prefab Placement - No prefab selected";

            return $"Prefab Placement - Selected: {SelectedPrefabName}" +
                   (IsPlacing ? " [PLACING]" : "");
        }

        private void OnPrefabLibraryChanged(object? sender, EventArgs e)
        {
            try
            {
                // Update available prefabs
                this.RaisePropertyChanged(nameof(AvailablePrefabs));

                // If current selection is no longer valid, reset
                var prefabs = AvailablePrefabs;
                if (!string.IsNullOrEmpty(SelectedPrefabName) && !prefabs.Contains(SelectedPrefabName))
                {
                    SelectedPrefabName = prefabs.FirstOrDefault() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[PrefabPlacementTool] OnPrefabLibraryChanged error: {ex}");
            }
        }
    }

    /// <summary>
    /// Updated base class for editor tools to work with GameObjectService
    /// </summary>
    public abstract class EditorToolBase : ReactiveObject, IEditorTool
    {
        protected readonly EditorEngine _engine;
        protected readonly GameObjectService _gameObjectService; // Updated to GameObjectService
        protected readonly CommandService _commandService;
        private bool _isActive;

        public abstract string Name { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public abstract string Icon { get; }

        public bool IsActive
        {
            get => _isActive;
            protected set => this.RaiseAndSetIfChanged(ref _isActive, value);
        }

        protected EditorToolBase(EditorEngine engine, GameObjectService gameObjectService, CommandService commandService)
        {
            _engine = engine;
            _gameObjectService = gameObjectService;
            _commandService = commandService;
        }

        public virtual void OnActivate()
        {
            IsActive = true;
        }

        public virtual void OnDeactivate()
        {
            IsActive = false;
        }

        public virtual void OnMouseDown(float worldX, float worldY, ViewportInputModifiers modifiers) { }
        public virtual void OnMouseUp(float worldX, float worldY, ViewportInputModifiers modifiers) { }
        public virtual void OnDrag(float worldX, float worldY, ViewportInputModifiers modifiers) { }
        public virtual void OnRightClick(float worldX, float worldY, ViewportInputModifiers modifiers) { }
        public virtual void OnKeyDown(string key, ViewportInputModifiers modifiers) { }
        public virtual void OnKeyUp(string key, ViewportInputModifiers modifiers) { }

        /// <summary>
        /// Helper to find entity at world position
        /// </summary>
        protected SceneEditor.Models.SceneNode? FindEntityAtPosition(float worldX, float worldY)
        {
            // TODO: Implement proper entity picking
            // This would use spatial queries or collision detection
            // For now, return null as placeholder
            return null;
        }

        /// <summary>
        /// Helper to get currently selected entities
        /// </summary>
        protected System.Collections.Generic.List<SceneEditor.Models.SceneNode> GetSelectedEntities()
        {
            var selected = new System.Collections.Generic.List<SceneEditor.Models.SceneNode>();
            // TODO: Implement selection tracking with GameObjectService
            return selected;
        }
    }
}