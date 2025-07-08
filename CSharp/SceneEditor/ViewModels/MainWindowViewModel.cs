using Avalonia.Data.Converters;
using Dock.Model.Controls;
using Dock.Model.Core;
using ReactiveUI;
using SceneEditor.Docking;
using SceneEditor.Services;
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ICommand = System.Windows.Input.ICommand;

namespace SceneEditor.ViewModels;

/// <summary>
/// Updated main window view model with docking support and theme integration
/// </summary>
public class MainWindowViewModel : ReactiveObject
{
    private readonly EditorEngine _engine;
    private readonly GameObjectService _gameObjectService;
    private readonly CommandService _commandService;
    private readonly ThemeService _themeService;
    private readonly DockFactory _dockFactory;
    private string _title = "WanderSpire Scene Editor";
    private string _statusText = "Ready";
    private bool _isInitialized = false;
    private IRootDock? _layout;

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public bool IsInitialized
    {
        get => _isInitialized;
        set => this.RaiseAndSetIfChanged(ref _isInitialized, value);
    }

    public IRootDock? Layout
    {
        get => _layout;
        set => this.RaiseAndSetIfChanged(ref _layout, value);
    }

    // Theme properties
    public bool IsDarkTheme => _themeService.IsDarkTheme;
    public string CurrentThemeName => _themeService.CurrentThemeName;
    public string ThemeIcon => _themeService.GetThemeIcon();
    public string ThemeTooltip => _themeService.GetThemeTooltip();
    public string ThemeMenuHeader => _themeService.GetThemeMenuHeader();

    // Static Converters for XAML
    public static IValueConverter EngineStatusConverter { get; } = new FuncValueConverter<bool, string>(
        isInitialized => isInitialized ? "Ready" : "Loading");

    // Panel ViewModels
    public GameObjectViewModel GameObjectViewModel { get; }
    public InspectorViewModel InspectorViewModel { get; }
    public AssetBrowserViewModel AssetBrowserViewModel { get; }
    public ToolboxViewModel ToolboxViewModel { get; }
    public ViewportViewModel ViewportViewModel { get; }
    public ProjectManagerViewModel ProjectManagerViewModel { get; }

    // Commands
    public ICommand NewSceneCommand { get; }
    public ICommand OpenSceneCommand { get; }
    public ICommand SaveSceneCommand { get; }
    public ICommand SaveSceneAsCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand PlaySceneCommand { get; }
    public ICommand PauseSceneCommand { get; }
    public ICommand StopSceneCommand { get; }
    public ICommand ResetLayoutCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    public MainWindowViewModel(
        EditorEngine engine,
        GameObjectService gameObjectService,
        CommandService commandService,
        ThemeService themeService,
        GameObjectViewModel gameObjectViewModel,
        InspectorViewModel inspectorViewModel,
        AssetBrowserViewModel assetBrowserViewModel,
        ToolboxViewModel toolboxViewModel,
        ViewportViewModel viewportViewModel,
        ProjectManagerViewModel projectManagerViewModel)
    {
        _engine = engine;
        _gameObjectService = gameObjectService;
        _commandService = commandService;
        _themeService = themeService;

        GameObjectViewModel = gameObjectViewModel;
        InspectorViewModel = inspectorViewModel;
        AssetBrowserViewModel = assetBrowserViewModel;
        ToolboxViewModel = toolboxViewModel;
        ViewportViewModel = viewportViewModel;
        ProjectManagerViewModel = projectManagerViewModel;

        // Initialize docking
        _dockFactory = new DockFactory(this);
        DebugFactoryEvents(_dockFactory);

        // Initialize commands
        NewSceneCommand = ReactiveCommand.CreateFromTask(NewScene);
        OpenSceneCommand = ReactiveCommand.CreateFromTask(OpenScene);
        SaveSceneCommand = ReactiveCommand.CreateFromTask(SaveScene);
        SaveSceneAsCommand = ReactiveCommand.CreateFromTask(SaveSceneAs);
        UndoCommand = ReactiveCommand.Create(Undo, this.WhenAnyValue(x => x._commandService.CanUndo));
        RedoCommand = ReactiveCommand.Create(Redo, this.WhenAnyValue(x => x._commandService.CanRedo));
        ExitCommand = ReactiveCommand.Create(Exit);
        PlaySceneCommand = ReactiveCommand.Create(PlayScene);
        PauseSceneCommand = ReactiveCommand.Create(PauseScene);
        StopSceneCommand = ReactiveCommand.Create(StopScene);
        ResetLayoutCommand = ReactiveCommand.Create(ResetLayout);
        ToggleThemeCommand = ReactiveCommand.Create(ToggleTheme);

        // Subscribe to engine events
        _engine.EngineInitialized += OnEngineInitialized;
        _engine.EngineShutdown += OnEngineShutdown;

        // Subscribe to scene events
        _gameObjectService.SceneChanged += OnSceneChanged;

        // Subscribe to theme events
        _themeService.ThemeChanged += OnThemeChanged;

        // Update title when scene changes
        this.WhenAnyValue(x => x._gameObjectService.CurrentScenePath, x => x._gameObjectService.HasUnsavedChanges)
            .Subscribe(UpdateTitle);

        // Initialize docking layout
        InitializeDockingLayout();

        // Initialize engine
        Task.Run(InitializeEngine);
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        // Notify UI of theme-related property changes
        this.RaisePropertyChanged(nameof(IsDarkTheme));
        this.RaisePropertyChanged(nameof(CurrentThemeName));
        this.RaisePropertyChanged(nameof(ThemeIcon));
        this.RaisePropertyChanged(nameof(ThemeTooltip));
        this.RaisePropertyChanged(nameof(ThemeMenuHeader));

        StatusText = $"Theme changed to {_themeService.CurrentThemeName}";

        Console.WriteLine($"[MainWindowViewModel] Theme changed to: {_themeService.CurrentThemeName}");
    }

    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }

    private void InitializeDockingLayout()
    {
        try
        {
            Layout = _dockFactory.CreateLayout();
            if (Layout is { })
            {
                _dockFactory.InitLayout(Layout);
                StatusText = "Docking layout initialized";
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MainWindowViewModel] Failed to initialize docking layout: {ex}");
            StatusText = "Failed to initialize docking layout";
        }
    }

    private void DebugFactoryEvents(IFactory factory)
    {
        factory.ActiveDockableChanged += (_, args) =>
        {
            Debug.WriteLine($"[ActiveDockableChanged] Title='{args.Dockable?.Title}'");
        };

        factory.FocusedDockableChanged += (_, args) =>
        {
            Debug.WriteLine($"[FocusedDockableChanged] Title='{args.Dockable?.Title}'");
        };

        factory.DockableAdded += (_, args) =>
        {
            Debug.WriteLine($"[DockableAdded] Title='{args.Dockable?.Title}'");
        };

        factory.DockableRemoved += (_, args) =>
        {
            Debug.WriteLine($"[DockableRemoved] Title='{args.Dockable?.Title}'");
        };

        factory.DockableClosed += (_, args) =>
        {
            Debug.WriteLine($"[DockableClosed] Title='{args.Dockable?.Title}'");
        };

        factory.DockableMoved += (_, args) =>
        {
            Debug.WriteLine($"[DockableMoved] Title='{args.Dockable?.Title}'");
        };

        factory.DockableSwapped += (_, args) =>
        {
            Debug.WriteLine($"[DockableSwapped] Title='{args.Dockable?.Title}'");
        };

        factory.DockablePinned += (_, args) =>
        {
            Debug.WriteLine($"[DockablePinned] Title='{args.Dockable?.Title}'");
        };

        factory.DockableUnpinned += (_, args) =>
        {
            Debug.WriteLine($"[DockableUnpinned] Title='{args.Dockable?.Title}'");
        };

        factory.WindowOpened += (_, args) =>
        {
            Debug.WriteLine($"[WindowOpened] Title='{args.Window?.Title}'");
        };

        factory.WindowClosed += (_, args) =>
        {
            Debug.WriteLine($"[WindowClosed] Title='{args.Window?.Title}'");
        };

        factory.WindowClosing += (_, args) =>
        {
            Debug.WriteLine($"[WindowClosing] Title='{args.Window?.Title}', Cancel={args.Cancel}");
        };
    }

    private async Task InitializeEngine()
    {
        StatusText = "Initializing engine...";

        await Task.Run(() =>
        {
            bool success = _engine.Initialize();
            if (success)
            {
                IsInitialized = true;
                StatusText = "Engine initialized successfully";
            }
            else
            {
                StatusText = "Failed to initialize engine";
            }
        });
    }

    private void OnEngineInitialized(object? sender, EventArgs e)
    {
        StatusText = "Engine ready";

        // Create default scene
        _gameObjectService.NewScene();

        // Load prefab library
        _gameObjectService.LoadPrefabLibrary();

        // Auto-place player prefab for testing
        var playerNode = _gameObjectService.InstantiatePrefab("player", 0, 0);
        if (playerNode != null)
        {
            StatusText = "Scene ready with player GameObject";
        }

        // Initialize panels
        GameObjectViewModel.Initialize();
        AssetBrowserViewModel.Initialize();
        ViewportViewModel.Initialize();
    }

    private void OnEngineShutdown(object? sender, EventArgs e)
    {
        StatusText = "Engine shutdown";
        IsInitialized = false;
    }

    private void OnSceneChanged(object? sender, EventArgs e)
    {
        StatusText = "Scene changed";
    }

    private void UpdateTitle((string? scenePath, bool hasUnsavedChanges) values)
    {
        var (scenePath, hasUnsavedChanges) = values;

        string sceneName = string.IsNullOrEmpty(scenePath)
            ? "Untitled Scene"
            : System.IO.Path.GetFileNameWithoutExtension(scenePath);

        string unsavedMarker = hasUnsavedChanges ? "*" : "";
        string themeIndicator = IsDarkTheme ? " [Dark]" : " [Light]";

        Title = $"WanderSpire Scene Editor - {sceneName}{unsavedMarker}{themeIndicator}";
    }

    // Command implementations
    private async Task NewScene()
    {
        if (_gameObjectService.HasUnsavedChanges)
        {
            // TODO: Show save dialog
        }

        _gameObjectService.NewScene();

        // Auto-place player GameObject for testing
        var playerNode = _gameObjectService.InstantiatePrefab("player", 0, 0);
        if (playerNode != null)
        {
            StatusText = "New scene created with player GameObject";
        }
        else
        {
            StatusText = "New scene created";
        }
    }

    private async Task OpenScene()
    {
        if (_gameObjectService.HasUnsavedChanges)
        {
            // TODO: Show save dialog
        }

        // TODO: Show open file dialog
        // For now, try to open a default scene
        string defaultScenePath = System.IO.Path.Combine("Assets", "scenes", "default.json");
        if (System.IO.File.Exists(defaultScenePath))
        {
            bool success = _gameObjectService.LoadScene(defaultScenePath);
            StatusText = success ? "Scene opened" : "Failed to open scene";
        }
        else
        {
            StatusText = "No scene file found";
        }
    }

    private async Task SaveScene()
    {
        if (string.IsNullOrEmpty(_gameObjectService.CurrentScenePath))
        {
            await SaveSceneAs();
            return;
        }

        bool success = _gameObjectService.SaveScene();
        StatusText = success ? "Scene saved" : "Failed to save scene";
    }

    private async Task SaveSceneAs()
    {
        // TODO: Show save file dialog
        // For now, save to a default location
        string defaultPath = System.IO.Path.Combine("Assets", "scenes", "editor_scene.json");

        // Ensure directory exists
        var directory = System.IO.Path.GetDirectoryName(defaultPath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        bool success = _gameObjectService.SaveScene(defaultPath);
        StatusText = success ? "Scene saved as " + defaultPath : "Failed to save scene";
    }

    private void Undo()
    {
        bool success = _commandService.Undo();
        StatusText = success ? "Undone" : "Nothing to undo";
    }

    private void Redo()
    {
        bool success = _commandService.Redo();
        StatusText = success ? "Redone" : "Nothing to redo";
    }

    private void Exit()
    {
        if (_gameObjectService.HasUnsavedChanges)
        {
            // TODO: Show save dialog
        }

        CloseLayout();
        // This would close the application
        Environment.Exit(0);
    }

    private void PlayScene()
    {
        StatusText = "Playing scene...";
        // TODO: Implement scene play mode
    }

    private void PauseScene()
    {
        StatusText = "Scene paused";
        // TODO: Implement scene pause
    }

    private void StopScene()
    {
        StatusText = "Scene stopped";
        // TODO: Implement scene stop
    }

    private void ResetLayout()
    {
        try
        {
            CloseLayout();
            InitializeDockingLayout();
            StatusText = "Layout reset";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MainWindowViewModel] Failed to reset layout: {ex}");
            StatusText = "Failed to reset layout";
        }
    }

    public void CloseLayout()
    {
        if (Layout is IDock dock)
        {
            if (dock.Close.CanExecute(null))
            {
                dock.Close.Execute(null);
            }
        }
    }

    public void Dispose()
    {
        _engine.EngineInitialized -= OnEngineInitialized;
        _engine.EngineShutdown -= OnEngineShutdown;
        _gameObjectService.SceneChanged -= OnSceneChanged;
        _themeService.ThemeChanged -= OnThemeChanged;
        CloseLayout();
    }
}