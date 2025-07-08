using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SceneEditor.Services;
using SceneEditor.Tools;
using SceneEditor.ViewModels;
using SceneEditor.Views;
using System;
using System.Reactive.Concurrency;

namespace SceneEditor;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
        RxApp.TaskpoolScheduler = TaskPoolScheduler.Default;

        ConfigureServices();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var startupWindow = new StartupWindow();
            desktop.MainWindow = startupWindow;
            startupWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Theme service - register early so it can be used by other services
        services.AddSingleton<ThemeService>();

        // Core services - order matters!
        services.AddSingleton<EditorEngine>();
        services.AddSingleton<CommandService>();
        services.AddSingleton<AssetService>();
        services.AddSingleton<GameObjectService>();

        // Tool services
        services.AddSingleton<ToolService>();

        // Tilemap services
        services.AddSingleton<TilemapService>();
        services.AddSingleton<TilePaletteService>();
        services.AddSingleton<TilePaintingService>();

        // ViewModels - these depend on services
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<GameObjectViewModel>();
        services.AddTransient<InspectorViewModel>();
        services.AddTransient<AssetBrowserViewModel>();
        services.AddTransient<ToolboxViewModel>();
        services.AddTransient<ViewportViewModel>();
        services.AddTransient<ProjectManagerViewModel>();

        ServiceProvider = services.BuildServiceProvider();

        // Initialize theme service after service provider is built
        var themeService = GetService<ThemeService>();
        themeService.InitializeTheme();

        // Register additional tools
        RegisterAdditionalTools();

        Console.WriteLine("[App] Services configured successfully with unified GameObject system and theme support");
    }

    private void RegisterAdditionalTools()
    {
        try
        {
            var toolService = GetService<ToolService>();

            // Register the prefab placement tool (now works with unified system)
            toolService.RegisterTool(new PrefabPlacementTool(
                GetService<EditorEngine>(),
                GetService<GameObjectService>(), // Updated to use GameObjectService
                GetService<CommandService>()));

            Console.WriteLine("[App] Additional tools registered");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[App] Failed to register additional tools: {ex}");
        }
    }

    public static T GetService<T>() where T : notnull
    {
        if (ServiceProvider == null)
            throw new InvalidOperationException("ServiceProvider not initialized");

        return ServiceProvider.GetRequiredService<T>();
    }

    public static T? GetOptionalService<T>() where T : class
    {
        return ServiceProvider?.GetService<T>();
    }
}