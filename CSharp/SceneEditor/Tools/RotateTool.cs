using SceneEditor.Services;
using SceneEditor.ViewModels;

namespace SceneEditor.Tools
{
    /// <summary>
    /// Rotate tool for rotating entities
    /// </summary>
    public class RotateTool : EditorToolBase
    {
        public override string Name => "Rotate";
        public override string DisplayName => "Rotate";
        public override string Description => "Rotate entities";
        public override string Icon => "\uf2f1"; // rotate icon

        public RotateTool(EditorEngine engine, GameObjectService sceneService, CommandService commandService)
            : base(engine, sceneService, commandService)
        {
        }

        public override void OnMouseDown(float worldX, float worldY, ViewportInputModifiers modifiers)
        {
            // TODO: Implement rotation gizmo interaction
        }
    }
}
