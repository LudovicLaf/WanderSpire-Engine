using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using SceneEditor.ViewModels;

namespace SceneEditor.Views;

public partial class ProjectManagerWindow : Window
{
    public ProjectManagerWindow()
    {
        InitializeComponent();
    }

    private void OpenSceneEditor_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProjectManagerViewModel viewModel && viewModel.IsCreatingProject)
        {
            // Create and show the main editor window
            var mainWindow = new MainWindow();

            // Initialize the main window with a properly configured service provider
            if (App.ServiceProvider != null)
            {
                var mainViewModel = App.ServiceProvider.GetRequiredService<MainWindowViewModel>();
                mainWindow.DataContext = mainViewModel;
            }

            mainWindow.Show();

            // Close the project manager
            this.Close();
        }
    }
}