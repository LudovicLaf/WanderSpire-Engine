using SceneEditor.Services;
using SceneEditor.ViewModels;

namespace SceneEditor.Tools
{
    /// <summary>
    /// Scale tool for scaling entities
    /// </summary>
    public class ScaleTool : EditorToolBase
    {
        public override string Name => "Scale";
        public override string DisplayName => "Scale";
        public override string Description => "Scale entities";
        public override string Icon => "\uf424"; // expand icon

        public ScaleTool(EditorEngine engine, GameObjectService sceneService, CommandService commandService)
            : base(engine, sceneService, commandService)
        {
        }

        public override void OnMouseDown(float worldX, float worldY, ViewportInputModifiers modifiers)
        {
            // TODO: Implement scale gizmo interaction
        }
    }
}
