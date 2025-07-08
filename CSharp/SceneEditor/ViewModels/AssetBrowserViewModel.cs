using ReactiveUI;
using SceneEditor.Models;
using SceneEditor.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SceneEditor.ViewModels;

/// <summary>
/// Modern asset browser view model with enhanced features and responsiveness
/// </summary>
public class AssetBrowserViewModel : ReactiveObject
{
    private readonly AssetService _assetService;
    private readonly GameObjectService _sceneService;
    private AssetItem? _selectedAsset;
    private string _searchText = string.Empty;
    private AssetType? _typeFilter;
    private AssetBrowserView _currentView = AssetBrowserView.Tree;
    private string _statusText = "Ready";
    private bool _isLoading = false;

    public ObservableCollection<AssetItem> FilteredAssets { get; } = new();

    // Reactive Properties
    public AssetItem? SelectedAsset
    {
        get => _selectedAsset;
        set => this.RaiseAndSetIfChanged(ref _selectedAsset, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public AssetType? TypeFilter
    {
        get => _typeFilter;
        set => this.RaiseAndSetIfChanged(ref _typeFilter, value);
    }

    public AssetBrowserView CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    // Computed Properties
    public ReadOnlyObservableCollection<AssetItem> RootAssets => _assetService.RootAssets;
    public bool IsTreeView => CurrentView == AssetBrowserView.Tree;
    public bool IsGridView => CurrentView == AssetBrowserView.Grid;
    public bool IsListView => CurrentView == AssetBrowserView.List;
    public bool IsEmpty => !RootAssets.Any() && !IsLoading;
    public int AssetCount => _assetService.AssetCount;

    // Commands
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateSceneCommand { get; }
    public ReactiveCommand<Unit, Unit> CreatePrefabCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateScriptCommand { get; }
    public ReactiveCommand<AssetItem?, Unit> DeleteAssetCommand { get; }
    public ReactiveCommand<AssetItem?, Unit> RenameAssetCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportAssetCommand { get; }
    public ReactiveCommand<AssetItem?, Unit> OpenAssetCommand { get; }
    public ReactiveCommand<AssetItem?, Unit> ShowInExplorerCommand { get; }
    public ReactiveCommand<string, Unit> SetViewModeCommand { get; }

    public AssetBrowserViewModel(AssetService assetService, GameObjectService sceneService)
    {
        _assetService = assetService;
        _sceneService = sceneService;

        // Initialize commands with proper observables
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAssetsAsync);
        CreateFolderCommand = ReactiveCommand.CreateFromTask(CreateFolderAsync);
        CreateSceneCommand = ReactiveCommand.CreateFromTask(CreateSceneAsync);
        CreatePrefabCommand = ReactiveCommand.CreateFromTask(CreatePrefabAsync);
        CreateScriptCommand = ReactiveCommand.CreateFromTask(CreateScriptAsync);
        DeleteAssetCommand = ReactiveCommand.CreateFromTask<AssetItem?>(DeleteAssetAsync);
        RenameAssetCommand = ReactiveCommand.CreateFromTask<AssetItem?>(RenameAssetAsync);
        ImportAssetCommand = ReactiveCommand.CreateFromTask(ImportAssetAsync);
        OpenAssetCommand = ReactiveCommand.CreateFromTask<AssetItem?>(OpenAssetAsync);
        ShowInExplorerCommand = ReactiveCommand.CreateFromTask<AssetItem?>(ShowInExplorerAsync);
        SetViewModeCommand = ReactiveCommand.Create<string>(SetViewMode);

        // Subscribe to asset service events
        _assetService.AssetsRefreshed += OnAssetsRefreshed;
        _assetService.AssetSelected += OnAssetSelected;

        // Bind loading state to asset service
        this.WhenAnyValue(x => x._assetService.IsLoading)
            .Subscribe(loading => IsLoading = loading);

        // Bind asset count to asset service
        this.WhenAnyValue(x => x._assetService.AssetCount)
            .Subscribe(count => this.RaisePropertyChanged(nameof(AssetCount)));

        // Monitor search and filter changes with debouncing
        this.WhenAnyValue(x => x.SearchText, x => x.TypeFilter)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ApplyFilter());

        // Monitor selection changes
        this.WhenAnyValue(x => x.SelectedAsset)
            .Subscribe(OnSelectedAssetChanged);

        // Set initial status
        UpdateStatus();
    }

    public async Task InitializeAsync()
    {
        StatusText = "Initializing assets...";
        await _assetService.RefreshAssetsAsync();
        ApplyFilter();
        UpdateStatus();
    }

    public void Initialize()
    {
        _ = InitializeAsync();
    }

    private void OnAssetsRefreshed(object? sender, EventArgs e)
    {
        ApplyFilter();
        UpdateStatus();
    }

    private void ApplyFilter()
    {
        _ = ApplyFilterAsync();
    }

    private void OnAssetSelected(object? sender, AssetItem asset)
    {
        SelectedAsset = asset;
    }

    private async Task ApplyFilterAsync()
    {
        try
        {
            var filteredResults = await _assetService.FindAssetsAsync(SearchText ?? string.Empty, TypeFilter);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                FilteredAssets.Clear();
                foreach (var asset in filteredResults)
                {
                    FilteredAssets.Add(asset);
                }
            });

            UpdateStatus();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to apply filter: {ex.Message}");
            StatusText = "Error filtering assets";
        }
    }

    private void OnSelectedAssetChanged(AssetItem? asset)
    {
        if (asset != null)
        {
            Console.WriteLine($"[AssetBrowser] Selected: {asset.Name} ({asset.Type})");
            StatusText = $"Selected: {asset.Name}";
        }
        else
        {
            UpdateStatus();
        }
    }

    private void UpdateStatus()
    {
        var assetCount = AssetCount;
        var filteredCount = FilteredAssets.Count;

        if (IsLoading)
        {
            StatusText = "Loading assets...";
        }
        else if (!string.IsNullOrEmpty(SearchText) || TypeFilter.HasValue)
        {
            StatusText = $"Showing {filteredCount} of {assetCount} assets";
        }
        else
        {
            StatusText = $"{assetCount} assets";
        }
    }

    private void SetViewMode(string mode)
    {
        CurrentView = mode switch
        {
            "Tree" => AssetBrowserView.Tree,
            "Grid" => AssetBrowserView.Grid,
            "List" => AssetBrowserView.List,
            _ => AssetBrowserView.Tree
        };

        StatusText = $"View mode: {CurrentView}";
        Console.WriteLine($"[AssetBrowser] View mode changed to: {CurrentView}");
    }

    // Command implementations
    private async Task RefreshAssetsAsync()
    {
        try
        {
            StatusText = "Refreshing assets...";
            await _assetService.RefreshAssetsAsync();
            await ApplyFilterAsync();
            StatusText = "Assets refreshed";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to refresh assets: {ex.Message}");
            StatusText = "Failed to refresh assets";
        }
    }

    private async Task CreateFolderAsync()
    {
        try
        {
            var parent = SelectedAsset?.Type == AssetType.Folder ? SelectedAsset : SelectedAsset?.Parent;
            var newAsset = await _assetService.CreateAssetAsync("New Folder", AssetType.Folder, parent);

            if (newAsset != null)
            {
                SelectedAsset = newAsset;
                StatusText = "Folder created";
                await RenameAssetAsync(newAsset);
            }
            else
            {
                StatusText = "Failed to create folder";
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to create folder: {ex.Message}");
            StatusText = "Failed to create folder";
        }
    }

    private async Task CreateSceneAsync()
    {
        try
        {
            var parent = SelectedAsset?.Type == AssetType.Folder ? SelectedAsset : SelectedAsset?.Parent;
            var newAsset = await _assetService.CreateAssetAsync("New Scene", AssetType.Scene, parent);

            if (newAsset != null)
            {
                SelectedAsset = newAsset;
                StatusText = "Scene created";
                await RenameAssetAsync(newAsset);
            }
            else
            {
                StatusText = "Failed to create scene";
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to create scene: {ex.Message}");
            StatusText = "Failed to create scene";
        }
    }

    private async Task CreatePrefabAsync()
    {
        try
        {
            var parent = SelectedAsset?.Type == AssetType.Folder ? SelectedAsset : SelectedAsset?.Parent;
            var newAsset = await _assetService.CreateAssetAsync("New Prefab", AssetType.Prefab, parent);

            if (newAsset != null)
            {
                SelectedAsset = newAsset;
                StatusText = "Prefab created";
                await RenameAssetAsync(newAsset);
            }
            else
            {
                StatusText = "Failed to create prefab";
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to create prefab: {ex.Message}");
            StatusText = "Failed to create prefab";
        }
    }

    private async Task CreateScriptAsync()
    {
        try
        {
            var parent = SelectedAsset?.Type == AssetType.Folder ? SelectedAsset : SelectedAsset?.Parent;
            var newAsset = await _assetService.CreateAssetAsync("New Script", AssetType.Script, parent);

            if (newAsset != null)
            {
                SelectedAsset = newAsset;
                StatusText = "Script created";
                await RenameAssetAsync(newAsset);
            }
            else
            {
                StatusText = "Failed to create script";
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to create script: {ex.Message}");
            StatusText = "Failed to create script";
        }
    }

    private async Task DeleteAssetAsync(AssetItem? asset)
    {
        asset ??= SelectedAsset;
        if (asset == null)
            return;

        try
        {
            // TODO: Show confirmation dialog in production
            var confirmed = true; // Placeholder

            if (confirmed)
            {
                StatusText = $"Deleting {asset.Name}...";
                bool success = await _assetService.DeleteAssetAsync(asset);

                if (success)
                {
                    if (SelectedAsset == asset)
                        SelectedAsset = null;
                    StatusText = $"Deleted {asset.Name}";
                }
                else
                {
                    StatusText = $"Failed to delete {asset.Name}";
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to delete asset: {ex.Message}");
            StatusText = "Failed to delete asset";
        }
    }

    private async Task RenameAssetAsync(AssetItem? asset)
    {
        asset ??= SelectedAsset;
        if (asset == null)
            return;

        try
        {
            // TODO: Implement inline renaming or show rename dialog
            // For now, just log the action
            Console.WriteLine($"[AssetBrowser] Rename asset: {asset.Name}");
            StatusText = $"Rename {asset.Name}";

            // Placeholder for actual rename implementation
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to rename asset: {ex.Message}");
            StatusText = "Failed to rename asset";
        }
    }

    private async Task ImportAssetAsync()
    {
        try
        {
            // TODO: Show file picker dialog to import external assets
            Console.WriteLine("[AssetBrowser] Import asset");
            StatusText = "Import asset functionality coming soon";
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to import asset: {ex.Message}");
            StatusText = "Failed to import asset";
        }
    }

    private async Task OpenAssetAsync(AssetItem? asset)
    {
        asset ??= SelectedAsset;
        if (asset == null)
            return;

        try
        {
            StatusText = $"Opening {asset.Name}...";

            switch (asset.Type)
            {
                case AssetType.Scene:
                    await OpenSceneAsync(asset);
                    break;
                case AssetType.Prefab:
                    await OpenPrefabAsync(asset);
                    break;
                case AssetType.Script:
                    await OpenScriptAsync(asset);
                    break;
                case AssetType.Texture:
                    await OpenTextureAsync(asset);
                    break;
                default:
                    await OpenInSystemEditorAsync(asset);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to open asset: {ex.Message}");
            StatusText = $"Failed to open {asset.Name}";
        }
    }

    private async Task OpenSceneAsync(AssetItem asset)
    {
        await Task.Run(() =>
        {
            bool success = _sceneService.LoadScene(asset.FullPath);
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                StatusText = success ? $"Loaded scene: {asset.Name}" : $"Failed to load scene: {asset.Name}";
            });
        });
    }

    private async Task OpenPrefabAsync(AssetItem asset)
    {
        // TODO: Open prefab in prefab editor mode
        Console.WriteLine($"[AssetBrowser] Open prefab: {asset.Name}");
        StatusText = $"Opened prefab: {asset.Name}";
        await Task.Delay(100);
    }

    private async Task OpenScriptAsync(AssetItem asset)
    {
        // TODO: Open script in code editor
        await OpenInSystemEditorAsync(asset);
    }

    private async Task OpenTextureAsync(AssetItem asset)
    {
        // TODO: Open texture in texture viewer
        Console.WriteLine($"[AssetBrowser] Open texture: {asset.Name}");
        StatusText = $"Viewing texture: {asset.Name}";
        await Task.Delay(100);
    }

    private async Task OpenInSystemEditorAsync(AssetItem asset)
    {
        try
        {
            await Task.Run(() =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = asset.FullPath,
                    UseShellExecute = true
                });
            });
            StatusText = $"Opened {asset.Name} in system editor";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to open asset {asset.Name}: {ex.Message}");
            StatusText = $"Failed to open {asset.Name}";
        }
    }

    private async Task ShowInExplorerAsync(AssetItem? asset)
    {
        asset ??= SelectedAsset;
        if (asset == null)
            return;

        try
        {
            await Task.Run(() =>
            {
                if (OperatingSystem.IsWindows())
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{asset.FullPath}\"");
                }
                else if (OperatingSystem.IsMacOS())
                {
                    System.Diagnostics.Process.Start("open", $"-R \"{asset.FullPath}\"");
                }
                else if (OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start("xdg-open", $"\"{System.IO.Path.GetDirectoryName(asset.FullPath)}\"");
                }
            });
            StatusText = $"Showed {asset.Name} in explorer";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to show asset in explorer: {ex.Message}");
            StatusText = $"Failed to show {asset.Name} in explorer";
        }
    }

    /// <summary>
    /// Handle drag and drop of assets
    /// </summary>
    public async Task HandleAssetDropAsync(AssetItem draggedAsset, AssetItem? targetAsset)
    {
        if (draggedAsset == targetAsset)
            return;

        try
        {
            // TODO: Implement asset moving/reorganization
            Console.WriteLine($"[AssetBrowser] Move {draggedAsset.Name} to {targetAsset?.Name ?? "root"}");
            StatusText = $"Moved {draggedAsset.Name}";
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Failed to move asset: {ex.Message}");
            StatusText = "Failed to move asset";
        }
    }

    /// <summary>
    /// Get assets that can be instantiated in the scene
    /// </summary>
    public AssetItem[] GetInstantiableAssets()
    {
        return _assetService.GetAssetsByType(AssetType.Prefab).ToArray();
    }

    /// <summary>
    /// Get available asset types for filtering
    /// </summary>
    public AssetType[] GetAvailableAssetTypes()
    {
        return new[]
        {
            AssetType.Scene,
            AssetType.Prefab,
            AssetType.Texture,
            AssetType.Audio,
            AssetType.Font,
            AssetType.Script,
            AssetType.Shader,
            AssetType.Text
        };
    }
}

public enum AssetBrowserView
{
    Tree,
    Grid,
    List
}