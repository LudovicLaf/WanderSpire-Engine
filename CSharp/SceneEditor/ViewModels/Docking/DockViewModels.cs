using Dock.Model.ReactiveUI.Controls;

namespace SceneEditor.ViewModels;

/// <summary>
/// Document view model for the viewport
/// </summary>
public class ViewportDocumentViewModel : Document
{
    public ViewportDocumentViewModel()
    {
        CanClose = false;
    }
}

/// <summary>
/// Tool view model for the GameObject panel
/// </summary>
public class GameObjectToolViewModel : Tool
{
    public GameObjectToolViewModel()
    {
        CanClose = false;
        CanPin = true;
    }
}

/// <summary>
/// Tool view model for the Inspector panel
/// </summary>
public class InspectorToolViewModel : Tool
{
    public InspectorToolViewModel()
    {
        CanClose = false;
        CanPin = true;
    }
}

/// <summary>
/// Tool view model for the Asset Browser panel
/// </summary>
public class AssetBrowserToolViewModel : Tool
{
    public AssetBrowserToolViewModel()
    {
        CanClose = false;
        CanPin = true;
    }
}

/// <summary>
/// Tool view model for the Toolbox panel
/// </summary>
public class ToolboxToolViewModel : Tool
{
    public ToolboxToolViewModel()
    {
        CanClose = false;
        CanPin = true;
    }
}