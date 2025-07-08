using SceneEditor.Services;
using SceneEditor.ViewModels;
using System;

namespace SceneEditor.Tools;

/// <summary>
/// Default selection and manipulation tool
/// </summary>
public class SelectTool : EditorToolBase
{
    private readonly GameObjectService _sceneService;
    public override string Name => "Select";
    public override string DisplayName => "Select";
    public override string Description => "Select and manipulate entities";
    public override string Icon => "\uf245"; // pointer icon

    private bool _isDragging;
    private float _dragStartX, _dragStartY;

    public SelectTool(EditorEngine engine,
                      GameObjectService sceneService,
                      CommandService commandService)
        : base(engine, sceneService, commandService)
    {
        _sceneService = sceneService;
    }

    public override void OnMouseDown(float worldX, float worldY, ViewportInputModifiers modifiers)
    {
        var entity = FindEntityAtPosition(worldX, worldY);

        if (entity != null)
        {
            // Select the entity
            _sceneService.SelectGameObject(entity);

            // Start potential drag operation
            _isDragging = true;
            _dragStartX = worldX;
            _dragStartY = worldY;
        }
        else if (!modifiers.HasFlag(ViewportInputModifiers.Control))
        {
            // Clear selection if not holding control
            // TODO: Implement selection clearing
        }
    }

    public override void OnMouseUp(float worldX, float worldY, ViewportInputModifiers modifiers)
    {
        _isDragging = false;
    }

    public override void OnDrag(float worldX, float worldY, ViewportInputModifiers modifiers)
    {
        if (!_isDragging)
            return;

        var deltaX = worldX - _dragStartX;
        var deltaY = worldY - _dragStartY;

        // TODO: Move selected entities
        Console.WriteLine($"Dragging entities by ({deltaX:F2}, {deltaY:F2})");
    }
}