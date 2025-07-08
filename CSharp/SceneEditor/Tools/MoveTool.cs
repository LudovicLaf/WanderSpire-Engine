using SceneEditor.Services;
using SceneEditor.ViewModels;

namespace SceneEditor.Tools
{
    /// <summary>
    /// Move tool for translating entities
    /// </summary>
    public class MoveTool : EditorToolBase
    {
        public override string Name => "Move";
        public override string DisplayName => "Move";
        public override string Description => "Move entities";
        public override string Icon => "\uf047"; // arrows icon

        public MoveTool(EditorEngine engine, GameObjectService sceneService, CommandService commandService)
            : base(engine, sceneService, commandService)
        {
        }

        public override void OnMouseDown(float worldX, float worldY, ViewportInputModifiers modifiers)
        {
            // TODO: Implement move gizmo interaction
        }
    }
}
