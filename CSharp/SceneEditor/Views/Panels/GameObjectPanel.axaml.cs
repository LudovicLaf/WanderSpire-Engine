using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using SceneEditor.Models;
using SceneEditor.ViewModels;
using System;
using System.Threading.Tasks;

namespace SceneEditor.Views.Panels;

public partial class GameObjectPanel : UserControl
{
    private GameObjectViewModel? _viewModel;

    public GameObjectPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SetupEventHandlers();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as GameObjectViewModel;
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        // Handle double-click on GameObjects to focus/edit
        if (this.FindControl<TreeView>("HierarchyTree") is TreeView hierarchyTree)
        {
            hierarchyTree.DoubleTapped -= OnGameObjectDoubleClick; // Remove existing handler
            hierarchyTree.DoubleTapped += OnGameObjectDoubleClick;
        }

        // Handle double-click on Prefabs to instantiate
        if (this.FindControl<ListBox>("PrefabList") is ListBox prefabList)
        {
            prefabList.DoubleTapped -= OnPrefabDoubleClick; // Remove existing handler
            prefabList.DoubleTapped += OnPrefabDoubleClick;
        }
    }

    private void OnGameObjectDoubleClick(object? sender, TappedEventArgs e)
    {
        if (_viewModel is null) return;

        // Find the data context from the tapped element
        var element = e.Source as Control;
        while (element is not null)
        {
            if (element.DataContext is SceneNode node)
            {
                _viewModel.FocusGameObjectCommand?.Execute(node);
                e.Handled = true;
                return;
            }
            element = element.Parent as Control;
        }
    }

    private void OnPrefabDoubleClick(object? sender, TappedEventArgs e)
    {
        if (_viewModel is null) return;

        // Find the data context from the tapped element
        var element = e.Source as Control;
        while (element is not null)
        {
            if (element.DataContext is PrefabDefinition prefab)
            {
                if (_viewModel.IsSceneMode)
                {
                    _viewModel.InstantiatePrefabCommand?.Execute(prefab.Name);
                }
                else
                {
                    _viewModel.SelectedPrefab = prefab;
                }
                e.Handled = true;
                return;
            }
            element = element.Parent as Control;
        }
    }

    /// <summary>
    /// Create a modern dialog for prefab selection
    /// </summary>
    public async Task ShowCreateFromPrefabDialog()
    {
        if (_viewModel is null) return;

        var availablePrefabs = _viewModel.GetAvailablePrefabs();
        if (availablePrefabs.Length == 0)
        {
            await ShowMessageDialog("No Prefabs Available", "No prefabs are available to instantiate. Create some prefabs first in Prefab mode.");
            return;
        }

        var dialog = CreatePrefabSelectionDialog(availablePrefabs);
        var parentWindow = this.GetTopLevelWindow();

        PrefabSelectionResult? result;
        if (parentWindow != null)
        {
            result = await dialog.ShowDialog<PrefabSelectionResult?>(parentWindow);
        }
        else
        {
            dialog.Show();
            result = null; // Handle this case as needed
        }

        if (result?.IsConfirmed == true && !string.IsNullOrEmpty(result.SelectedPrefab))
        {
            _viewModel.InstantiatePrefabCommand?.Execute(result.SelectedPrefab);
        }
    }

    /// <summary>
    /// Create a modern dialog for new prefab creation
    /// </summary>
    public async Task ShowCreatePrefabDialog()
    {
        if (_viewModel is null) return;

        var dialog = CreatePrefabNameDialog();
        var parentWindow = this.GetTopLevelWindow();

        PrefabNameResult? result;
        if (parentWindow != null)
        {
            result = await dialog.ShowDialog<PrefabNameResult?>(parentWindow);
        }
        else
        {
            dialog.Show();
            result = null; // Handle this case as needed
        }

        if (result?.IsConfirmed == true && !string.IsNullOrEmpty(result.PrefabName))
        {
            _viewModel.CreateNewPrefabCommand?.Execute(result.PrefabName);
        }
    }

    private Window CreatePrefabSelectionDialog(string[] availablePrefabs)
    {
        var dialog = new Window
        {
            Title = "Select Prefab",
            Width = 350,
            Height = 450,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = SystemDecorations.Full
        };

        var mainPanel = new DockPanel { Margin = new Avalonia.Thickness(20) };

        // Header
        var header = new TextBlock
        {
            Text = "Select a prefab to instantiate:",
            FontSize = 14,
            FontWeight = Avalonia.Media.FontWeight.Medium,
            Margin = new Avalonia.Thickness(0, 0, 0, 16)
        };
        DockPanel.SetDock(header, Avalonia.Controls.Dock.Top);

        // Button panel
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 12,
            Margin = new Avalonia.Thickness(0, 16, 0, 0)
        };
        DockPanel.SetDock(buttonPanel, Avalonia.Controls.Dock.Bottom);

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80,
            Padding = new Avalonia.Thickness(16, 8)
        };
        var createButton = new Button
        {
            Content = "Create",
            Width = 80,
            IsEnabled = false,
            Classes = { "accent" },
            Padding = new Avalonia.Thickness(16, 8)
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(createButton);

        // Prefab list
        var listBox = new ListBox
        {
            ItemsSource = availablePrefabs,
            SelectionMode = SelectionMode.Single,
            Margin = new Avalonia.Thickness(0, 0, 0, 0)
        };

        // Assemble dialog
        mainPanel.Children.Add(header);
        mainPanel.Children.Add(buttonPanel);
        mainPanel.Children.Add(listBox);
        dialog.Content = mainPanel;

        var result = new PrefabSelectionResult();

        // Event handlers
        listBox.SelectionChanged += (s, e) =>
        {
            result.SelectedPrefab = listBox.SelectedItem as string;
            createButton.IsEnabled = !string.IsNullOrEmpty(result.SelectedPrefab);
        };

        createButton.Click += (s, e) =>
        {
            result.IsConfirmed = true;
            dialog.Close(result);
        };

        cancelButton.Click += (s, e) => dialog.Close(result);

        listBox.DoubleTapped += (s, e) =>
        {
            if (!string.IsNullOrEmpty(result.SelectedPrefab))
            {
                result.IsConfirmed = true;
                dialog.Close(result);
            }
        };

        return dialog;
    }

    private Window CreatePrefabNameDialog()
    {
        var dialog = new Window
        {
            Title = "Create New Prefab",
            Width = 350,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = SystemDecorations.Full
        };

        var mainPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16
        };

        var label = new TextBlock
        {
            Text = "Prefab Name:",
            FontWeight = Avalonia.Media.FontWeight.Medium
        };

        var textBox = new TextBox
        {
            Text = $"NewPrefab_{DateTime.Now:HHmmss}",
            Watermark = "Enter prefab name..."
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 12,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80,
            Padding = new Avalonia.Thickness(16, 8)
        };
        var createButton = new Button
        {
            Content = "Create",
            Width = 80,
            Classes = { "accent" },
            Padding = new Avalonia.Thickness(16, 8)
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(createButton);

        mainPanel.Children.Add(label);
        mainPanel.Children.Add(textBox);
        mainPanel.Children.Add(buttonPanel);

        dialog.Content = mainPanel;

        var result = new PrefabNameResult();

        // Event handlers
        createButton.Click += (s, e) =>
        {
            var name = textBox.Text?.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                result.PrefabName = name;
                result.IsConfirmed = true;
                dialog.Close(result);
            }
        };

        cancelButton.Click += (s, e) => dialog.Close(result);

        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                var name = textBox.Text?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    result.PrefabName = name;
                    result.IsConfirmed = true;
                    dialog.Close(result);
                }
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close(result);
            }
        };

        // Focus and select all text
        Dispatcher.UIThread.Post(() =>
        {
            textBox.Focus();
            textBox.SelectAll();
        }, DispatcherPriority.Normal);

        return dialog;
    }

    private async Task ShowMessageDialog(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = SystemDecorations.Full
        };

        var mainPanel = new DockPanel { Margin = new Avalonia.Thickness(20) };

        var messageText = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            FontSize = 14,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        };
        DockPanel.SetDock(messageText, Avalonia.Controls.Dock.Top);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        DockPanel.SetDock(buttonPanel, Avalonia.Controls.Dock.Bottom);

        var okButton = new Button
        {
            Content = "OK",
            Width = 80,
            Classes = { "accent" },
            Padding = new Avalonia.Thickness(16, 8)
        };
        buttonPanel.Children.Add(okButton);

        mainPanel.Children.Add(messageText);
        mainPanel.Children.Add(buttonPanel);
        dialog.Content = mainPanel;

        okButton.Click += (s, e) => dialog.Close();

        var parentWindow = this.GetTopLevelWindow();
        if (parentWindow != null)
        {
            await dialog.ShowDialog(parentWindow);
        }
        else
        {
            dialog.Show();
        }
    }

    /// <summary>
    /// Handle search text changes with debouncing
    /// </summary>
    public void OnSearchTextChanged(string searchText)
    {
        // Use the safe extension method to apply search filter
        _viewModel.TryApplySearchFilter(searchText);
    }

    /// <summary>
    /// Show context-sensitive help for the current mode
    /// </summary>
    public async Task ShowModeHelp()
    {
        if (_viewModel is null) return;

        var helpText = _viewModel.IsSceneMode
            ? "Scene Mode allows you to:\n\n• View and edit GameObjects in the scene hierarchy\n• Create prefabs from selected GameObjects\n• Instantiate prefabs into the scene\n• Organize objects using parent-child relationships"
            : "Prefab Mode allows you to:\n\n• Create and edit prefab definitions\n• Save prefabs as reusable templates\n• Manage your prefab library\n• Define default components and properties";

        await ShowMessageDialog(_viewModel.IsSceneMode ? "Scene Mode Help" : "Prefab Mode Help", helpText);
    }
}

/// <summary>
/// Result class for prefab selection dialog
/// </summary>
public class PrefabSelectionResult
{
    public bool IsConfirmed { get; set; }
    public string? SelectedPrefab { get; set; }
}

/// <summary>
/// Result class for prefab name dialog
/// </summary>
public class PrefabNameResult
{
    public bool IsConfirmed { get; set; }
    public string? PrefabName { get; set; }
}

/// <summary>
/// Extension methods for better code organization
/// </summary>
public static class GameObjectPanelExtensions
{
    /// <summary>
    /// Safely get the top-level window from any control
    /// </summary>
    public static Window? GetTopLevelWindow(this Control control)
    {
        // Try to get the TopLevel first (modern approach)
        var topLevel = TopLevel.GetTopLevel(control);
        if (topLevel is Window window)
        {
            return window;
        }

        // Fallback: traverse the visual tree to find a Window
        var parent = control.Parent;
        while (parent != null)
        {
            if (parent is Window parentWindow)
            {
                return parentWindow;
            }
            parent = parent.Parent;
        }

        return null;
    }

    /// <summary>
    /// Find a control by name with null safety
    /// </summary>
    public static T? FindControlSafe<T>(this Control control, string name) where T : Control
    {
        try
        {
            return control.FindControl<T>(name);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Safe method to apply search filter if the ViewModel supports it
    /// </summary>
    public static void TryApplySearchFilter(this GameObjectViewModel? viewModel, string? searchText)
    {
        // TODO: Implement when GameObjectViewModel has search support
        // This is a placeholder for future search functionality
        if (viewModel != null && !string.IsNullOrEmpty(searchText))
        {
            // viewModel.SearchText = searchText; // Uncomment when property exists
        }
    }
}