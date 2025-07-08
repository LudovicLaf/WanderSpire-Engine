using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.ReactiveUI;
using Dock.Model.ReactiveUI.Controls;
using SceneEditor.ViewModels;
using System;
using System.Collections.Generic;

namespace SceneEditor.Docking;

/// <summary>
/// Factory for creating and managing the docking layout for the Scene Editor
/// </summary>
public class DockFactory : Factory
{
    private readonly object _context;
    private IRootDock? _rootDock;
    private IDocumentDock? _documentDock;

    public DockFactory(object context)
    {
        _context = context;
    }

    public override IRootDock CreateLayout()
    {
        // Create the main document dock for viewport
        var viewportDocument = new ViewportDocumentViewModel
        {
            Id = "Viewport",
            Title = "Viewport"
        };

        // Create tool docks for panels
        var gameObjectTool = new GameObjectToolViewModel
        {
            Id = "GameObjects",
            Title = "GameObjects"
        };

        var inspectorTool = new InspectorToolViewModel
        {
            Id = "Inspector",
            Title = "Inspector"
        };

        var assetBrowserTool = new AssetBrowserToolViewModel
        {
            Id = "Assets",
            Title = "Assets"
        };

        var toolboxTool = new ToolboxToolViewModel
        {
            Id = "Toolbox",
            Title = "Toolbox"
        };

        // Create left panel (GameObjects)
        var leftDock = new ToolDock
        {
            Id = "LeftDock",
            Proportion = 0.2,
            ActiveDockable = gameObjectTool,
            VisibleDockables = CreateList<IDockable>(gameObjectTool),
            Alignment = Alignment.Left
        };

        // Create right panel (Inspector)
        var rightDock = new ToolDock
        {
            Id = "RightDock",
            Proportion = 0.25,
            ActiveDockable = inspectorTool,
            VisibleDockables = CreateList<IDockable>(inspectorTool),
            Alignment = Alignment.Right
        };

        // Create bottom left panel (Assets)
        var bottomLeftDock = new ToolDock
        {
            Id = "BottomLeftDock",
            ActiveDockable = assetBrowserTool,
            VisibleDockables = CreateList<IDockable>(assetBrowserTool),
            Alignment = Alignment.Bottom
        };

        // Create bottom right panel (Toolbox) 
        var bottomRightDock = new ToolDock
        {
            Id = "BottomRightDock",
            ActiveDockable = toolboxTool,
            VisibleDockables = CreateList<IDockable>(toolboxTool),
            Alignment = Alignment.Bottom
        };

        // Create document dock for viewport
        _documentDock = new DocumentDock
        {
            Id = "DocumentDock",
            IsCollapsable = false,
            ActiveDockable = viewportDocument,
            VisibleDockables = CreateList<IDockable>(viewportDocument),
            CanCreateDocument = false
        };

        // Create vertical layout for left side (GameObjects)
        var leftLayout = leftDock;

        // Create vertical layout for right side (Inspector)  
        var rightLayout = rightDock;

        // Create bottom layout with splitter
        var bottomLayout = new ProportionalDock
        {
            Id = "BottomLayout",
            Proportion = 0.3,
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>
            (
                bottomLeftDock,
                new ProportionalDockSplitter(),
                bottomRightDock
            )
        };

        // Create center layout with viewport and bottom panels
        var centerLayout = new ProportionalDock
        {
            Id = "CenterLayout",
            Orientation = Orientation.Vertical,
            VisibleDockables = CreateList<IDockable>
            (
                _documentDock,
                new ProportionalDockSplitter(),
                bottomLayout
            )
        };

        // Create main layout
        var mainLayout = new ProportionalDock
        {
            Id = "MainLayout",
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>
            (
                leftLayout,
                new ProportionalDockSplitter(),
                centerLayout,
                new ProportionalDockSplitter(),
                rightLayout
            )
        };

        // Create root dock
        _rootDock = CreateRootDock();
        _rootDock.Id = "Root";
        _rootDock.IsCollapsable = false;
        _rootDock.ActiveDockable = mainLayout;
        _rootDock.DefaultDockable = mainLayout;
        _rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        return _rootDock;
    }

    public override IDockWindow? CreateWindowFrom(IDockable dockable)
    {
        var window = base.CreateWindowFrom(dockable);
        if (window != null)
        {
            window.Title = "WanderSpire Scene Editor";
            window.Width = 800;
            window.Height = 600;
        }
        return window;
    }

    public override void InitLayout(IDockable layout)
    {
        ContextLocator = new Dictionary<string, Func<object?>>
        {
            ["Viewport"] = () => _context,
            ["GameObjects"] = () => _context,
            ["Inspector"] = () => _context,
            ["Assets"] = () => _context,
            ["Toolbox"] = () => _context,
            ["Root"] = () => _context
        };

        DockableLocator = new Dictionary<string, Func<IDockable?>>()
        {
            ["Root"] = () => _rootDock,
            ["Documents"] = () => _documentDock
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }
}