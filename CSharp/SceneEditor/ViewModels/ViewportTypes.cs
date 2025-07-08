// File: CSharp/SceneEditor/ViewModels/ViewportTypes.cs
using System;

namespace SceneEditor.ViewModels;

/// <summary>
/// Types of mouse events that can occur in the viewport
/// </summary>
public enum ViewportMouseEventType
{
    /// <summary>Left mouse button click</summary>
    LeftClick,

    /// <summary>Right mouse button click</summary>
    RightClick,

    /// <summary>Middle mouse button click</summary>
    MiddleClick,

    /// <summary>Mouse drag operation</summary>
    Drag,

    /// <summary>Mouse wheel scroll</summary>
    Wheel,

    /// <summary>Mouse button down (press)</summary>
    MouseDown,

    /// <summary>Mouse button up (release)</summary>
    MouseUp,

    /// <summary>Mouse movement without buttons pressed</summary>
    MouseMove,

    /// <summary>Mouse enters the viewport area</summary>
    MouseEnter,

    /// <summary>Mouse leaves the viewport area</summary>
    MouseLeave,

    /// <summary>Double click</summary>
    DoubleClick
}

/// <summary>
/// Input modifier keys that can be held during viewport interactions
/// </summary>
[Flags]
public enum ViewportInputModifiers
{
    /// <summary>No modifiers pressed</summary>
    None = 0,

    /// <summary>Control key (Ctrl on Windows/Linux, Cmd on Mac)</summary>
    Control = 1 << 0,

    /// <summary>Shift key</summary>
    Shift = 1 << 1,

    /// <summary>Alt key</summary>
    Alt = 1 << 2,

    /// <summary>Meta/Windows/Cmd key</summary>
    Meta = 1 << 3,

    /// <summary>Caps Lock is active</summary>
    CapsLock = 1 << 4,

    /// <summary>Num Lock is active</summary>
    NumLock = 1 << 5,

    /// <summary>Scroll Lock is active</summary>
    ScrollLock = 1 << 6,

    /// <summary>Left mouse button is pressed</summary>
    LeftButton = 1 << 8,

    /// <summary>Right mouse button is pressed</summary>
    RightButton = 1 << 9,

    /// <summary>Middle mouse button is pressed</summary>
    MiddleButton = 1 << 10,

    /// <summary>Fourth mouse button (X1) is pressed</summary>
    X1Button = 1 << 11,

    /// <summary>Fifth mouse button (X2) is pressed</summary>
    X2Button = 1 << 12
}

/// <summary>
/// Viewport display and rendering modes
/// </summary>
public enum ViewportMode
{
    /// <summary>Normal scene editing mode</summary>
    Scene,

    /// <summary>Game preview mode (runtime view)</summary>
    Game,

    /// <summary>Wireframe rendering mode</summary>
    Wireframe,

    /// <summary>Shaded rendering without textures</summary>
    Shaded,

    /// <summary>Textured shaded rendering</summary>
    Textured,

    /// <summary>Lighting only mode</summary>
    Lighting,

    /// <summary>Show overdraw/performance debug</summary>
    Overdraw,

    /// <summary>UV coordinate visualization</summary>
    UV,

    /// <summary>Normal vectors visualization</summary>
    Normals,

    /// <summary>Collision shapes visualization</summary>
    Collision
}

/// <summary>
/// Viewport gizmo display modes
/// </summary>
public enum ViewportGizmoMode
{
    /// <summary>No gizmos displayed</summary>
    None,

    /// <summary>Only selection gizmos</summary>
    Selection,

    /// <summary>All transformation gizmos</summary>
    Transform,

    /// <summary>All gizmos including debug visualizations</summary>
    All
}

/// <summary>
/// Grid snapping modes for the viewport
/// </summary>
public enum ViewportSnapMode
{
    /// <summary>No snapping</summary>
    None,

    /// <summary>Snap to grid</summary>
    Grid,

    /// <summary>Snap to vertices of other objects</summary>
    Vertex,

    /// <summary>Snap to edges of other objects</summary>
    Edge,

    /// <summary>Snap to face centers</summary>
    Face,

    /// <summary>Snap to all available snap points</summary>
    All
}

/// <summary>
/// Arguments for viewport mouse events
/// </summary>
public class ViewportMouseEventArgs : EventArgs
{
    /// <summary>Type of mouse event</summary>
    public ViewportMouseEventType Type { get; set; }

    /// <summary>Screen X coordinate</summary>
    public int X { get; set; }

    /// <summary>Screen Y coordinate</summary>
    public int Y { get; set; }

    /// <summary>World X coordinate (if available)</summary>
    public float WorldX { get; set; }

    /// <summary>World Y coordinate (if available)</summary>
    public float WorldY { get; set; }

    /// <summary>Mouse wheel delta</summary>
    public float Delta { get; set; }

    /// <summary>Input modifier keys pressed</summary>
    public ViewportInputModifiers Modifiers { get; set; }

    /// <summary>Which mouse button was pressed (for button events)</summary>
    public int Button { get; set; }

    /// <summary>Number of clicks (for click events)</summary>
    public int ClickCount { get; set; } = 1;

    /// <summary>Whether this event has been handled</summary>
    public bool Handled { get; set; }

    /// <summary>Timestamp of the event</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>Check if a specific modifier is pressed</summary>
    public bool HasModifier(ViewportInputModifiers modifier)
    {
        return (Modifiers & modifier) != 0;
    }

    /// <summary>Check if Control key is pressed (platform-agnostic)</summary>
    public bool IsControlPressed => HasModifier(ViewportInputModifiers.Control);

    /// <summary>Check if Shift key is pressed</summary>
    public bool IsShiftPressed => HasModifier(ViewportInputModifiers.Shift);

    /// <summary>Check if Alt key is pressed</summary>
    public bool IsAltPressed => HasModifier(ViewportInputModifiers.Alt);

    /// <summary>Check if any mouse button is pressed</summary>
    public bool IsAnyMouseButtonPressed =>
        HasModifier(ViewportInputModifiers.LeftButton) ||
        HasModifier(ViewportInputModifiers.RightButton) ||
        HasModifier(ViewportInputModifiers.MiddleButton) ||
        HasModifier(ViewportInputModifiers.X1Button) ||
        HasModifier(ViewportInputModifiers.X2Button);
}

/// <summary>
/// Arguments for viewport keyboard events
/// </summary>
public class ViewportKeyEventArgs : EventArgs
{
    /// <summary>The key that was pressed</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Key code (if available)</summary>
    public int KeyCode { get; set; }

    /// <summary>Input modifier keys pressed</summary>
    public ViewportInputModifiers Modifiers { get; set; }

    /// <summary>Whether this is a key down event</summary>
    public bool IsDown { get; set; }

    /// <summary>Whether this is a repeat event</summary>
    public bool IsRepeat { get; set; }

    /// <summary>Whether this event has been handled</summary>
    public bool Handled { get; set; }

    /// <summary>Check if a specific modifier is pressed</summary>
    public bool HasModifier(ViewportInputModifiers modifier)
    {
        return (Modifiers & modifier) != 0;
    }
}

/// <summary>
/// Camera projection modes for the viewport
/// </summary>
public enum ViewportProjectionMode
{
    /// <summary>2D orthographic projection</summary>
    Orthographic2D,

    /// <summary>3D orthographic projection</summary>
    Orthographic3D,

    /// <summary>3D perspective projection</summary>
    Perspective
}

/// <summary>
/// Viewport overlay display options
/// </summary>
[Flags]
public enum ViewportOverlayFlags
{
    /// <summary>No overlays</summary>
    None = 0,

    /// <summary>Show coordinate grid</summary>
    Grid = 1 << 0,

    /// <summary>Show gizmos</summary>
    Gizmos = 1 << 1,

    /// <summary>Show performance statistics</summary>
    Stats = 1 << 2,

    /// <summary>Show help text</summary>
    Help = 1 << 3,

    /// <summary>Show rulers/measurements</summary>
    Rulers = 1 << 4,

    /// <summary>Show origin/center point</summary>
    Origin = 1 << 5,

    /// <summary>Show bounding boxes</summary>
    BoundingBoxes = 1 << 6,

    /// <summary>Show entity names/labels</summary>
    Labels = 1 << 7,

    /// <summary>Show collision shapes</summary>
    Collision = 1 << 8,

    /// <summary>Show navigation mesh</summary>
    NavMesh = 1 << 9,

    /// <summary>Show light visualization</summary>
    Lighting = 1 << 10,

    /// <summary>Show audio sources visualization</summary>
    Audio = 1 << 11,

    /// <summary>All overlays enabled</summary>
    All = ~0
}

/// <summary>
/// Selection modes for the viewport
/// </summary>
public enum ViewportSelectionMode
{
    /// <summary>Replace current selection</summary>
    Replace,

    /// <summary>Add to current selection</summary>
    Add,

    /// <summary>Remove from current selection</summary>
    Remove,

    /// <summary>Toggle selection state</summary>
    Toggle
}

/// <summary>
/// Tool operation modes
/// </summary>
public enum ViewportToolMode
{
    /// <summary>Select and manipulate objects</summary>
    Select,

    /// <summary>Move/translate objects</summary>
    Move,

    /// <summary>Rotate objects</summary>
    Rotate,

    /// <summary>Scale objects</summary>
    Scale,

    /// <summary>Paint tiles or textures</summary>
    Paint,

    /// <summary>Erase tiles or objects</summary>
    Erase,

    /// <summary>Rectangular selection</summary>
    RectSelect,

    /// <summary>Lasso selection</summary>
    LassoSelect,

    /// <summary>Measure distances</summary>
    Measure,

    /// <summary>Navigate/pan camera</summary>
    Navigate
}

/// <summary>
/// Coordinate space for transformations
/// </summary>
public enum ViewportCoordinateSpace
{
    /// <summary>World coordinate space</summary>
    World,

    /// <summary>Local coordinate space</summary>
    Local,

    /// <summary>Screen coordinate space</summary>
    Screen,

    /// <summary>Parent coordinate space</summary>
    Parent
}

/// <summary>
/// Viewport focus targets
/// </summary>
public enum ViewportFocusTarget
{
    /// <summary>Focus on selected objects</summary>
    Selection,

    /// <summary>Focus on all objects in scene</summary>
    All,

    /// <summary>Focus on origin (0,0)</summary>
    Origin,

    /// <summary>Focus on specific coordinates</summary>
    Coordinates,

    /// <summary>Focus on camera target</summary>
    CameraTarget
}