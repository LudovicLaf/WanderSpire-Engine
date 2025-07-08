// File: CSharp/SceneEditor/Services/ThemeService.cs
using Avalonia;
using Avalonia.Styling;
using System;
using System.IO;
using System.Text.Json;

namespace SceneEditor.Services;

/// <summary>
/// Service for managing application-wide theme switching
/// </summary>
public class ThemeService
{
    private readonly string _settingsPath;
    private bool _isDarkTheme = false;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public bool IsDarkTheme => _isDarkTheme;
    public string CurrentThemeName => _isDarkTheme ? "Dark" : "Light";

    public ThemeService()
    {
        // Setup settings path
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "WanderSpire");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "theme_settings.json");

        InitializeTheme();
    }

    /// <summary>
    /// Initialize theme from saved settings or system preference
    /// </summary>
    public void InitializeTheme()
    {
        try
        {
            // Load saved theme preference first
            var savedTheme = LoadThemeSettings();
            if (savedTheme != null)
            {
                _isDarkTheme = savedTheme.IsDarkTheme;
            }
            else
            {
                // No saved preference, detect system theme
                _isDarkTheme = DetectSystemTheme();
            }

            ApplyTheme();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] Failed to initialize theme: {ex.Message}");
            // Default to light theme on error
            _isDarkTheme = false;
            ApplyTheme();
        }
    }

    /// <summary>
    /// Toggle between dark and light theme
    /// </summary>
    public void ToggleTheme()
    {
        SetTheme(!_isDarkTheme);
    }

    /// <summary>
    /// Set specific theme
    /// </summary>
    public void SetTheme(bool isDark)
    {
        if (_isDarkTheme != isDark)
        {
            var oldTheme = _isDarkTheme;
            _isDarkTheme = isDark;

            ApplyTheme();
            SaveThemeSettings();

            // Notify listeners
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, _isDarkTheme));

            Console.WriteLine($"[ThemeService] Theme changed to: {CurrentThemeName}");
        }
    }

    /// <summary>
    /// Apply the current theme to the application
    /// </summary>
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
            Console.WriteLine($"[ThemeService] Failed to apply theme: {ex.Message}");
        }
    }

    /// <summary>
    /// Detect system theme preference
    /// </summary>
    private bool DetectSystemTheme()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return DetectWindowsTheme();
            }
            else if (OperatingSystem.IsMacOS())
            {
                return DetectMacOSTheme();
            }
            else if (OperatingSystem.IsLinux())
            {
                return DetectLinuxTheme();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] Failed to detect system theme: {ex.Message}");
        }

        // Fallback: time-based detection
        var hour = DateTime.Now.Hour;
        return hour < 7 || hour > 19;
    }

    private bool DetectWindowsTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            if (key?.GetValue("AppsUseLightTheme") is int value)
            {
                return value == 0; // 0 = dark theme, 1 = light theme
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] Windows theme detection failed: {ex.Message}");
        }
        return false;
    }

    private bool DetectMacOSTheme()
    {
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "defaults",
                Arguments = "read -g AppleInterfaceStyle",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process != null)
            {
                process.WaitForExit(2000); // 2 second timeout
                var output = process.StandardOutput.ReadToEnd().Trim();
                return output.Equals("Dark", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] macOS theme detection failed: {ex.Message}");
        }
        return false;
    }

    private bool DetectLinuxTheme()
    {
        try
        {
            // Check environment variables first
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
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process != null)
            {
                process.WaitForExit(2000);
                var output = process.StandardOutput.ReadToEnd().Trim();
                return output.Contains("dark", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] Linux theme detection failed: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Save theme settings to file
    /// </summary>
    private void SaveThemeSettings()
    {
        try
        {
            var settings = new ThemeSettings
            {
                IsDarkTheme = _isDarkTheme,
                LastUpdated = DateTime.Now,
                Version = "1.0.0"
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] Failed to save theme settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Load theme settings from file
    /// </summary>
    private ThemeSettings? LoadThemeSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<ThemeSettings>(json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ThemeService] Failed to load theme settings: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Get theme icon based on current theme
    /// </summary>
    public string GetThemeIcon()
    {
        return _isDarkTheme ? "☀️" : "🌙";
    }

    /// <summary>
    /// Get theme toggle tooltip text
    /// </summary>
    public string GetThemeTooltip()
    {
        return _isDarkTheme ? "Switch to Light Theme" : "Switch to Dark Theme";
    }

    /// <summary>
    /// Get theme menu header text
    /// </summary>
    public string GetThemeMenuHeader()
    {
        return _isDarkTheme ? "Toggle _Light Theme" : "Toggle _Dark Theme";
    }
}

/// <summary>
/// Event args for theme change notifications
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public bool PreviousTheme { get; }
    public bool NewTheme { get; }
    public bool IsNowDark => NewTheme;
    public bool IsNowLight => !NewTheme;

    public ThemeChangedEventArgs(bool previousTheme, bool newTheme)
    {
        PreviousTheme = previousTheme;
        NewTheme = newTheme;
    }
}

/// <summary>
/// Theme settings model for persistence
/// </summary>
public class ThemeSettings
{
    public bool IsDarkTheme { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Version { get; set; } = "1.0.0";
    public bool AutoDetectSystemTheme { get; set; } = true;
}