using SceneEditor.Services;
using SceneEditor.ViewModels;
using System;

namespace SceneEditor.Tools
{
    /// <summary>
    /// Tile erasing tool
    /// </summary>
    public class TileEraseTool : EditorToolBase
    {
        public override string Name => "TileErase";
        public override string DisplayName => "Erase Tiles";
        public override string Description => "Erase tiles from tilemap layers";
        public override string Icon => "\uf12d"; // eraser icon

        public TileEraseTool(EditorEngine engine, GameObjectService sceneService, CommandService commandService)
            : base(engine, sceneService, commandService)
        {
        }

        public override void OnMouseDown(float worldX, float worldY, ViewportInputModifiers modifiers)
        {
            EraseTile(worldX, worldY);
        }

        public override void OnDrag(float worldX, float worldY, ViewportInputModifiers modifiers)
        {
            EraseTile(worldX, worldY);
        }

        private void EraseTile(float worldX, float worldY)
        {
            var tileX = (int)Math.Floor(worldX / _engine.TileSize);
            var tileY = (int)Math.Floor(worldY / _engine.TileSize);

            // TODO: Find active tilemap layer and erase tile (set to 0)
            Console.WriteLine($"Erase tile at ({tileX}, {tileY})");
        }
    }
}
