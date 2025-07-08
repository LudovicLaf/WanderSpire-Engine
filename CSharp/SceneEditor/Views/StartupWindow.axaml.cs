using Avalonia.Controls;
using Avalonia.Interactivity;
using SceneEditor.Services;
using SceneEditor.ViewModels;
using System;
using System.Threading.Tasks;

namespace SceneEditor.Views;

public partial class StartupWindow : Window
{
    private readonly ThemeService _themeService;

    public StartupWindow()
    {
        InitializeComponent();

        // Get theme service if available
        _themeService = App.GetOptionalService<ThemeService>() ?? new ThemeService();

        SetupEventHandlers();
        ApplyTheme();
    }

    private void SetupEventHandlers()
    {
        this.Loaded += (s, e) =>
        {
            // Wire up button click events
            if (this.FindControl<Button>("ManageProjectsButton") is Button manageProjectsBtn)
            {
                manageProjectsBtn.Click += ManageProjects_Click;
            }

            if (this.FindControl<Button>("OpenSceneEditorButton") is Button openEditorBtn)
            {
                openEditorBtn.Click += OpenSceneEditor_Click;
            }

            if (this.FindControl<Button>("ExitButton") is Button exitBtn)
            {
                exitBtn.Click += Exit_Click;
            }
        };

        // Subscribe to theme changes
        _themeService.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        try
        {
            // The theme is automatically applied through the application-wide theme system
            // Any startup window specific theme customizations can go here
            Console.WriteLine($"[StartupWindow] Applied {_themeService.CurrentThemeName} theme");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StartupWindow] Failed to apply theme: {ex.Message}");
        }
    }

    private async void OpenSceneEditor_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var engine = App.GetService<EditorEngine>();
            if (!engine.IsInitialized)
            {
                bool ok = engine.Initialize();
                if (!ok)
                    throw new Exception("Engine initialization failed");
            }

            var mainViewModel = App.GetService<MainWindowViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            mainWindow.Show();
            Close(); // Only close after main is shown
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Failed to open Scene Editor", ex.Message);
        }
    }

    private void ManageProjects_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var projectManagerViewModel = App.GetService<ProjectManagerViewModel>();
            var projectManagerWindow = new ProjectManagerWindow
            {
                DataContext = projectManagerViewModel
            };

            projectManagerWindow.Show();
            this.Close();
        }
        catch (Exception ex)
        {
            _ = ShowErrorDialog("Failed to open Project Manager", ex.Message);
        }
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16
        };

        // Error icon and title
        var headerPanel = new StackPanel
        {
            Spacing = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        headerPanel.Children.Add(new TextBlock
        {
            Text = "⚠️",
            FontSize = 32,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        headerPanel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = App.Current?.FindResource("PrimaryTextBrush") as Avalonia.Media.IBrush
        });

        content.Children.Add(headerPanel);

        // Error message
        content.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextAlignment = Avalonia.Media.TextAlignment.Center,
            Foreground = App.Current?.FindResource("SecondaryTextBrush") as Avalonia.Media.IBrush
        });

        // OK button
        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            MinWidth = 80,
            Padding = new Avalonia.Thickness(16, 8),
            Classes = { "Primary" }
        };

        okButton.Click += (s, e) => dialog.Close();
        content.Children.Add(okButton);

        dialog.Content = content;

        try
        {
            await dialog.ShowDialog(this);
        }
        catch
        {
            // Fallback if ShowDialog fails
            dialog.Show();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from theme changes
        if (_themeService != null)
        {
            _themeService.ThemeChanged -= OnThemeChanged;
        }

        base.OnClosed(e);
    }
}