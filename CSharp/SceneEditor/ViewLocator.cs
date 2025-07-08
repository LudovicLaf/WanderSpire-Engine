// File: CSharp/SceneEditor/DockViewLocator.cs
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Dock.Model.Core;
using ReactiveUI;
using SceneEditor.ViewModels;
using SceneEditor.Views.Panels;
using System;
using System.Collections.Generic;

namespace SceneEditor;

/// <summary>
/// View locator for mapping dock view models to their corresponding views
/// </summary>
public class DockViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control?>> ViewMap = new()
    {
        [typeof(ViewportDocumentViewModel)] = () => new ViewportPanel(),
        [typeof(GameObjectToolViewModel)] = () => new GameObjectPanel(),
        [typeof(InspectorToolViewModel)] = () => new InspectorPanel(),
        [typeof(AssetBrowserToolViewModel)] = () => new AssetBrowserPanel(),
        [typeof(ToolboxToolViewModel)] = () => new ToolboxPanel()
    };

    public Control? Build(object? data)
    {
        if (data is null)
        {
            return null;
        }

        var type = data.GetType();
        Console.WriteLine($"[DockViewLocator] Building view for: {type.Name}");

        if (ViewMap.TryGetValue(type, out var factory))
        {
            var view = factory.Invoke();
            if (view != null)
            {
                Console.WriteLine($"[DockViewLocator] Created view: {view.GetType().Name}");

                try
                {
                    // Get the main view model from the App service provider
                    var mainViewModel = App.GetService<MainWindowViewModel>();

                    // Map dock view models to the appropriate panel view models
                    view.DataContext = type.Name switch
                    {
                        nameof(ViewportDocumentViewModel) => mainViewModel.ViewportViewModel,
                        nameof(GameObjectToolViewModel) => mainViewModel.GameObjectViewModel,
                        nameof(InspectorToolViewModel) => mainViewModel.InspectorViewModel,
                        nameof(AssetBrowserToolViewModel) => mainViewModel.AssetBrowserViewModel,
                        nameof(ToolboxToolViewModel) => mainViewModel.ToolboxViewModel,
                        _ => data
                    };

                    Console.WriteLine($"[DockViewLocator] Set DataContext for {type.Name}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[DockViewLocator] Failed to set DataContext: {ex.Message}");
                    // Fallback to the dock view model itself
                    view.DataContext = data;
                }
            }
            return view;
        }

        Console.WriteLine($"[DockViewLocator] No view found for {type.Name}, creating fallback");
        return new TextBlock { Text = $"View not found for {type.Name}" };
    }

    public bool Match(object? data)
    {
        if (data == null) return false;

        var type = data.GetType();
        bool matches = ViewMap.ContainsKey(type) || data is ReactiveObject || data is IDockable;

        Console.WriteLine($"[DockViewLocator] Match check for {type.Name}: {matches}");
        return matches;
    }
}