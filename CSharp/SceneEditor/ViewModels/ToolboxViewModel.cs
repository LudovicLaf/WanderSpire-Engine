// File: CSharp/SceneEditor/ViewModels/ToolboxViewModel.cs
using ReactiveUI;
using SceneEditor.Services;
using System;
using System.Collections.ObjectModel;
using ICommand = System.Windows.Input.ICommand;

namespace SceneEditor.ViewModels;

/// <summary>
/// View model for the toolbox panel that shows available tools
/// </summary>
public class ToolboxViewModel : ReactiveObject
{
    private readonly ToolService _toolService;
    private IEditorTool? _selectedTool;

    public ObservableCollection<IEditorTool> Tools { get; } = new();
    /// <summary>
    /// Get tools by category
    /// </summary>
    public ObservableCollection<IEditorTool> SelectionTools { get; } = new();
    public ObservableCollection<IEditorTool> TileTools { get; } = new();
    public ObservableCollection<IEditorTool> OtherTools { get; } = new();

    public IEditorTool? SelectedTool
    {
        get => _selectedTool;
        set => this.RaiseAndSetIfChanged(ref _selectedTool, value);
    }

    // Commands
    public ICommand SelectToolCommand { get; }

    public ToolboxViewModel(ToolService toolService)
    {
        _toolService = toolService;

        // Initialize commands
        SelectToolCommand = ReactiveCommand.Create<IEditorTool>(SelectTool);

        // Subscribe to tool service events
        _toolService.ToolChanged += OnToolChanged;

        // Monitor selected tool changes
        this.WhenAnyValue(x => x.SelectedTool)
            .Subscribe(OnSelectedToolChanged);

        // Load available tools
        LoadTools();
    }

    private void LoadTools()
    {
        Tools.Clear();
        SelectionTools.Clear();
        TileTools.Clear();
        OtherTools.Clear();

        foreach (var tool in _toolService.AvailableTools)
        {
            Tools.Add(tool);

            // Categorize tools
            if (IsSelectionTool(tool))
                SelectionTools.Add(tool);
            else if (IsTileTool(tool))
                TileTools.Add(tool);
            else
                OtherTools.Add(tool);
        }

        // Set initial selection
        SelectedTool = _toolService.CurrentTool;
    }

    private bool IsSelectionTool(IEditorTool tool)
    {
        return tool.Name is "Select" or "Move" or "Rotate" or "Scale";
    }

    private bool IsTileTool(IEditorTool tool)
    {
        return tool.Name.StartsWith("Tile");
    }

    private void OnToolChanged(object? sender, IEditorTool tool)
    {
        SelectedTool = tool;
    }

    private void OnSelectedToolChanged(IEditorTool? tool)
    {
        if (tool != null && tool != _toolService.CurrentTool)
        {
            _toolService.SetActiveTool(tool.Name);
        }
    }

    private void SelectTool(IEditorTool tool)
    {
        SelectedTool = tool;
    }
}