using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using SceneEditor.ViewModels;
using System;
using System.IO;
using System.Text.Json;

namespace SceneEditor.Views;

public partial class MainWindow : Window
{
    private bool _isDarkTheme = false;
    private readonly string _settingsPath;

    public MainWindow()
    {
        InitializeComponent();

        // Setup settings path
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "WanderSpire");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");

        InitializeTheme();
    }

    private void InitializeTheme()
    {
        try
        {
            // Load saved theme preference first
            _isDarkTheme = LoadThemePreference();

            // If no saved preference, check system theme
            if (!File.Exists(_settingsPath))
            {
                var app = Application.Current;
                if (app != null)
                {
                    _isDarkTheme = app.ActualThemeVariant == ThemeVariant.Dark ||
                                  (app.ActualThemeVariant == ThemeVariant.Default && IsSystemDarkTheme());
                }
            }

            ApplyTheme();
            UpdateThemeIcons();
            UpdateThemeStatusText();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize theme: {ex.Message}");
            // Default to light theme on error
            _isDarkTheme = false;
            ApplyTheme();
            UpdateThemeIcons();
            UpdateThemeStatusText();
        }
    }

    private bool IsSystemDarkTheme()
    {
        try
        {
            // Try to detect system dark theme preference
            if (OperatingSystem.IsWindows())
            {
                return IsWindowsDarkTheme();
            }
            else if (OperatingSystem.IsMacOS())
            {
                return IsMacOSDarkTheme();
            }
            else if (OperatingSystem.IsLinux())
            {
                return IsLinuxDarkTheme();
            }
        }
        catch
        {
            // Ignore errors in system detection
        }

        // Fallback: simple time-based detection
        var hour = DateTime.Now.Hour;
        return hour < 7 || hour > 19;
    }

    private bool IsWindowsDarkTheme()
    {
        try
        {
            // Check Windows registry for dark theme preference
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            if (key?.GetValue("AppsUseLightTheme") is int value)
            {
                return value == 0; // 0 = dark theme, 1 = light theme
            }
        }
        catch
        {
            // Ignore registry access errors
        }
        return false;
    }

    private bool IsMacOSDarkTheme()
    {
        try
        {
            // Use defaults command to check macOS dark mode
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "defaults",
                Arguments = "read -g AppleInterfaceStyle",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            if (process != null)
            {
                process.WaitForExit(1000); // 1 second timeout
                var output = process.StandardOutput.ReadToEnd().Trim();
                return output.Equals("Dark", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // Ignore process execution errors
        }
        return false;
    }

    private bool IsLinuxDarkTheme()
    {
        try
        {
            // Check common Linux dark theme environment variables
            var gtkTheme = Environment.GetEnvironmentVariable("GTK_THEME");
            if (!string.IsNullOrEmpty(gtkTheme) && gtkTheme.Contains("dark", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check GNOME settings
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "gsettings",
                Arguments = "get org.gnome.desktop.interface gtk-theme",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            if (process != null)
            {
                process.WaitForExit(1000);
                var output = process.StandardOutput.ReadToEnd().Trim();
                return output.Contains("dark", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // Ignore errors in Linux theme detection
        }
        return false;
    }

    private void ToggleTheme_Click(object? sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        ApplyTheme();
        UpdateThemeIcons();
        UpdateThemeStatusText();
        SaveThemePreference(_isDarkTheme);
    }

    private void ApplyTheme()
    {
        try
        {
            var app = Application.Current;
            if (app != null)
            {
                app.RequestedThemeVariant = _isDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to apply theme: {ex.Message}");
        }
    }

    private void UpdateThemeIcons()
    {
        try
        {
            // Update toolbar theme icon
            if (this.FindControl<TextBlock>("ThemeIcon") is TextBlock themeIcon)
            {
                themeIcon.Text = _isDarkTheme ? "☀️" : "🌙";
            }

            // Update menu theme icon
            if (this.FindControl<TextBlock>("MenuThemeIcon") is TextBlock menuThemeIcon)
            {
                menuThemeIcon.Text = _isDarkTheme ? "☀️" : "🌙";
            }

            // Update menu item text
            if (this.FindControl<MenuItem>("ToggleThemeMenuItem") is MenuItem menuItem)
            {
                menuItem.Header = _isDarkTheme ? "Toggle _Light Theme" : "Toggle _Dark Theme";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update theme icons: {ex.Message}");
        }
    }

    private void UpdateThemeStatusText()
    {
        try
        {
            if (this.FindControl<TextBlock>("ThemeStatusText") is TextBlock statusText)
            {
                statusText.Text = _isDarkTheme ? "Dark Theme" : "Light Theme";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update theme status text: {ex.Message}");
        }
    }

    private void SaveThemePreference(bool isDark)
    {
        try
        {
            var settings = new ThemeSettings
            {
                IsDarkTheme = isDark,
                LastUpdated = DateTime.Now
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save theme preference: {ex.Message}");
        }
    }

    private bool LoadThemePreference()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
                return settings?.IsDarkTheme ?? false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load theme preference: {ex.Message}");
        }
        return false; // Default to light theme
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Ensure theme is properly applied after window is opened
        ApplyTheme();
        UpdateThemeIcons();
        UpdateThemeStatusText();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Set up key bindings only when DataContext is available
        if (DataContext is MainWindowViewModel viewModel)
        {
            SetupKeyBindings(viewModel);
        }
    }

    private void SetupKeyBindings(MainWindowViewModel viewModel)
    {
        // Clear any existing key bindings first
        KeyBindings.Clear();

        // File operations
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.N, KeyModifiers.Control),
            Command = viewModel.NewSceneCommand
        });

        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.O, KeyModifiers.Control),
            Command = viewModel.OpenSceneCommand
        });

        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.S, KeyModifiers.Control),
            Command = viewModel.SaveSceneCommand
        });

        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift),
            Command = viewModel.SaveSceneAsCommand
        });

        // Edit operations
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.Z, KeyModifiers.Control),
            Command = viewModel.UndoCommand
        });

        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.Y, KeyModifiers.Control),
            Command = viewModel.RedoCommand
        });

        // Scene operations
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.F5),
            Command = viewModel.PlaySceneCommand
        });

        // Theme toggle
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.T, KeyModifiers.Control | KeyModifiers.Shift),
            Command = ReactiveUI.ReactiveCommand.Create(ToggleThemeFromKeyboard)
        });
    }

    private void ToggleThemeFromKeyboard()
    {
        ToggleTheme_Click(null, new RoutedEventArgs());
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Save theme preference before closing
        SaveThemePreference(_isDarkTheme);

        // Handle unsaved changes and cleanup docking
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Dispose();
        }

        base.OnClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Handle additional global shortcuts
        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.OemComma: // Ctrl+, for settings (common shortcut)
                    // Open settings window
                    e.Handled = true;
                    break;
            }
        }

        if (!e.Handled)
        {
            base.OnKeyDown(e);
        }
    }

    /// <summary>
    /// Programmatically set the theme (useful for settings or startup)
    /// </summary>
    public void SetTheme(bool isDark)
    {
        if (_isDarkTheme != isDark)
        {
            _isDarkTheme = isDark;
            ApplyTheme();
            UpdateThemeIcons();
            UpdateThemeStatusText();
            SaveThemePreference(_isDarkTheme);
        }
    }

    /// <summary>
    /// Get current theme state
    /// </summary>
    public bool IsDarkTheme => _isDarkTheme;

    /// <summary>
    /// Toggle theme programmatically
    /// </summary>
    public void ToggleTheme()
    {
        ToggleTheme_Click(null, new RoutedEventArgs());
    }
}

/// <summary>
/// Settings model for theme persistence
/// </summary>
public class ThemeSettings
{
    public bool IsDarkTheme { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Version { get; set; } = "1.0.0";
}