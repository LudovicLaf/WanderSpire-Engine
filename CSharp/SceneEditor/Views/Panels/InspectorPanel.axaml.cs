using Avalonia.Controls;
using Avalonia.Interactivity;
using SceneEditor.ViewModels;

namespace SceneEditor.Views.Panels;

public partial class InspectorPanel : UserControl
{
    public InspectorPanel()
    {
        InitializeComponent();
    }

    private async void AddComponent_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not InspectorViewModel viewModel)
            return;

        var availableComponents = viewModel.GetAvailableComponentTypes();
        if (availableComponents.Length == 0)
        {
            // Show message that all components are already added
            return;
        }

        // Create a simple selection dialog
        var dialog = new Window
        {
            Title = "Add Component",
            Width = 300,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var content = new DockPanel { Margin = new Avalonia.Thickness(16) };

        // Header
        var header = new TextBlock
        {
            Text = "Select a component to add:",
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };
        DockPanel.SetDock(header, Avalonia.Controls.Dock.Top);
        content.Children.Add(header);

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };
        DockPanel.SetDock(buttonPanel, Avalonia.Controls.Dock.Bottom);

        var cancelButton = new Button { Content = "Cancel", Width = 80 };
        var addButton = new Button { Content = "Add", Width = 80, IsEnabled = false };
        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(addButton);
        content.Children.Add(buttonPanel);

        // Component list
        var listBox = new ListBox
        {
            ItemsSource = availableComponents,
            SelectionMode = SelectionMode.Single
        };
        content.Children.Add(listBox);

        dialog.Content = content;

        string? selectedComponent = null;

        listBox.SelectionChanged += (s, e) =>
        {
            selectedComponent = listBox.SelectedItem as string;
            addButton.IsEnabled = selectedComponent != null;
        };

        addButton.Click += (s, e) =>
        {
            if (selectedComponent != null)
            {
                viewModel.AddComponentCommand.Execute(selectedComponent);
            }
            dialog.Close();
        };

        cancelButton.Click += (s, e) => dialog.Close();

        // Handle double-click to add component
        listBox.DoubleTapped += (s, e) =>
        {
            if (selectedComponent != null)
            {
                viewModel.AddComponentCommand.Execute(selectedComponent);
                dialog.Close();
            }
        };

        if (this.VisualRoot is Window parentWindow)
        {
            await dialog.ShowDialog(parentWindow);
        }
        else
        {
            dialog.Show();
        }
    }
}