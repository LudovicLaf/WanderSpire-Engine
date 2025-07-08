using ReactiveUI;
using SceneEditor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WanderSpire.Scripting;

namespace SceneEditor.Services;

/// <summary>
/// Modern asset service with enhanced hot reload and async operations
/// </summary>
public class AssetService : ReactiveObject, IDisposable
{
    private readonly EditorEngine _engine;
    private readonly ObservableCollection<AssetItem> _rootAssets = new();
    private readonly Dictionary<string, AssetItem> _assetCache = new();
    private readonly FileSystemWatcher? _fileWatcher;
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
    private string _assetsRootPath = string.Empty;
    private bool _isWatching = false;
    private bool _disposed = false;

    public ReadOnlyObservableCollection<AssetItem> RootAssets { get; }
    public string AssetsRootPath => _assetsRootPath;

    // Reactive properties
    private bool _isLoading = false;
    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private int _assetCount = 0;
    public int AssetCount
    {
        get => _assetCount;
        private set => this.RaiseAndSetIfChanged(ref _assetCount, value);
    }

    // Events
    public event EventHandler? AssetsRefreshed;
    public event EventHandler<AssetItem>? AssetSelected;
    public event EventHandler<string>? PrefabAdded;
    public event EventHandler<string>? PrefabChanged;
    public event EventHandler<string>? PrefabRemoved;

    public AssetService(EditorEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        RootAssets = new ReadOnlyObservableCollection<AssetItem>(_rootAssets);

        // Initialize asset root path
        InitializeAssetPath();
    }

    private void InitializeAssetPath()
    {
        // Try the exe's directory first: <exeDir>/Assets
        var exeDir = AppContext.BaseDirectory;
        var defaultDir = Path.Combine(exeDir, "Assets");
        if (Directory.Exists(defaultDir))
        {
            _ = SetAssetsRootAsync(defaultDir);
            return;
        }

        // Fall back to ContentPaths.Root (+ "Assets" if needed)
        if (!string.IsNullOrWhiteSpace(ContentPaths.Root))
        {
            var cpRoot = ContentPaths.Root!;
            var cpAssets = cpRoot.EndsWith("Assets", StringComparison.OrdinalIgnoreCase)
                         ? cpRoot
                         : Path.Combine(cpRoot, "Assets");

            if (Directory.Exists(cpAssets))
            {
                SetAssetsRoot(cpAssets);
                return;
            }
        }

        Console.WriteLine("[AssetService] No valid asset directory found");
    }

    /// <summary>
    /// Set the root directory for assets asynchronously
    /// </summary>
    public async Task<bool> SetAssetsRootAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            Console.Error.WriteLine($"[AssetService] Assets directory does not exist: {path}");
            return false;
        }

        _assetsRootPath = path;
        await SetupFileWatcherAsync();
        await RefreshAssetsAsync();
        return true;
    }

    /// <summary>
    /// Set the root directory for assets synchronously (for backward compatibility)
    /// </summary>
    public void SetAssetsRoot(string path)
    {
        try
        {
            SetAssetsRootAsync(path).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetService] Failed to set assets root: {ex.Message}");
        }
    }

    private async Task SetupFileWatcherAsync()
    {
        await Task.Run(() =>
        {
            _fileWatcher?.Dispose();
            _isWatching = false;

            try
            {
                var watcher = new FileSystemWatcher(_assetsRootPath)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                    Filter = "*.*",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
                };

                watcher.Created += OnFileSystemChanged;
                watcher.Deleted += OnFileSystemChanged;
                watcher.Renamed += OnFileSystemChanged;
                watcher.Changed += OnFileSystemChanged;

                _isWatching = true;
                Console.WriteLine($"[AssetService] File watcher setup for: {_assetsRootPath}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AssetService] Failed to setup file watcher: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Refresh the asset tree from the file system asynchronously
    /// </summary>
    public async Task RefreshAssetsAsync()
    {
        if (string.IsNullOrEmpty(_assetsRootPath) || !Directory.Exists(_assetsRootPath))
            return;

        if (!await _refreshSemaphore.WaitAsync(100))
            return; // Skip if already refreshing

        try
        {
            IsLoading = true;

            await Task.Run(() =>
            {
                _assetCache.Clear();

                // Clear on UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() => _rootAssets.Clear());

                var rootInfo = new DirectoryInfo(_assetsRootPath);
                LoadDirectory(rootInfo, null);

                // Update asset count on UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    AssetCount = _assetCache.Count;
                });
            });

            // Notify on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                AssetsRefreshed?.Invoke(this, EventArgs.Empty);
            });

            Console.WriteLine($"[AssetService] Refreshed {AssetCount} assets");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetService] Failed to refresh assets: {ex}");
        }
        finally
        {
            IsLoading = false;
            _refreshSemaphore.Release();
        }
    }

    /// <summary>
    /// Refresh the asset tree synchronously (for backward compatibility)
    /// </summary>
    public void RefreshAssets()
    {
        try
        {
            RefreshAssetsAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetService] Failed to refresh assets: {ex.Message}");
        }
    }

    private void LoadDirectory(DirectoryInfo dirInfo, AssetItem? parent)
    {
        var dirAsset = new AssetItem
        {
            Name = dirInfo.Name,
            FullPath = dirInfo.FullName,
            RelativePath = Path.GetRelativePath(_assetsRootPath, dirInfo.FullName),
            Type = AssetType.Folder,
            Parent = parent,
            LastModified = dirInfo.LastWriteTime
        };

        _assetCache[dirAsset.RelativePath] = dirAsset;

        // Add to UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (parent != null)
            {
                parent.Children.Add(dirAsset);
            }
            else
            {
                _rootAssets.Add(dirAsset);
            }
        });

        // Load subdirectories
        try
        {
            foreach (var subDir in dirInfo.GetDirectories()
                .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden) &&
                           !d.Attributes.HasFlag(FileAttributes.System)))
            {
                LoadDirectory(subDir, dirAsset);
            }

            // Load files
            foreach (var file in dirInfo.GetFiles()
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) &&
                           !f.Attributes.HasFlag(FileAttributes.System)))
            {
                var fileAsset = new AssetItem
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    RelativePath = Path.GetRelativePath(_assetsRootPath, file.FullName),
                    Type = GetAssetTypeFromExtension(file.Extension),
                    Size = file.Length,
                    LastModified = file.LastWriteTime,
                    Parent = dirAsset
                };

                _assetCache[fileAsset.RelativePath] = fileAsset;

                // Add to UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    dirAsset.Children.Add(fileAsset);
                });
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetService] Error loading directory {dirInfo.FullName}: {ex.Message}");
        }
    }

    private static AssetType GetAssetTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" or ".webp" => AssetType.Texture,
            ".json" => AssetType.Prefab,
            ".glsl" or ".hlsl" or ".vert" or ".frag" or ".compute" => AssetType.Shader,
            ".wav" or ".mp3" or ".ogg" or ".flac" => AssetType.Audio,
            ".ttf" or ".otf" or ".woff" or ".woff2" => AssetType.Font,
            ".csx" or ".cs" => AssetType.Script,
            ".txt" or ".md" or ".yml" or ".yaml" => AssetType.Text,
            ".scene" => AssetType.Scene,
            _ => AssetType.Unknown
        };
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        if (_disposed) return;

        try
        {
            // Handle prefab hot reload
            if (e.FullPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = Path.GetRelativePath(_assetsRootPath, e.FullPath);

                if (relativePath.StartsWith("prefabs", StringComparison.OrdinalIgnoreCase))
                {
                    var prefabName = Path.GetFileNameWithoutExtension(e.Name);

                    switch (e.ChangeType)
                    {
                        case WatcherChangeTypes.Created:
                            Console.WriteLine($"[AssetService] Prefab added: {prefabName}");
                            PrefabAdded?.Invoke(this, prefabName);
                            break;

                        case WatcherChangeTypes.Changed:
                            Console.WriteLine($"[AssetService] Prefab changed: {prefabName}");
                            PrefabChanged?.Invoke(this, prefabName);
                            break;

                        case WatcherChangeTypes.Deleted:
                            Console.WriteLine($"[AssetService] Prefab removed: {prefabName}");
                            PrefabRemoved?.Invoke(this, prefabName);
                            break;
                    }

                    TriggerPrefabRegistryReload();
                }
            }

            // Debounced refresh
            Task.Delay(500).ContinueWith(_ =>
            {
                Task.Run(async () => await RefreshAssetsAsync());
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetService] File system change error: {ex.Message}");
        }
    }

    private void TriggerPrefabRegistryReload()
    {
        try
        {
            Console.WriteLine("[AssetService] Triggering prefab registry reload");
            // The existing PrefabRegistry.EnsureInitialized() will pick up new files automatically
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetService] Failed to trigger prefab registry reload: {ex}");
        }
    }

    /// <summary>
    /// Create a new asset from a template asynchronously
    /// </summary>
    public async Task<AssetItem?> CreateAssetAsync(string name, AssetType type, AssetItem? parent = null)
    {
        try
        {
            var parentPath = parent?.FullPath ?? _assetsRootPath;
            var fullPath = Path.Combine(parentPath, name);

            await Task.Run(() =>
            {
                switch (type)
                {
                    case AssetType.Scene:
                        if (!name.EndsWith(".json"))
                            fullPath += ".json";
                        CreateSceneAsset(fullPath);
                        break;

                    case AssetType.Prefab:
                        if (!name.EndsWith(".json"))
                            fullPath += ".json";
                        CreatePrefabAsset(fullPath);
                        break;

                    case AssetType.Script:
                        if (!name.EndsWith(".csx"))
                            fullPath += ".csx";
                        CreateScriptAsset(fullPath);
                        break;

                    case AssetType.Folder:
                        Directory.CreateDirectory(fullPath);
                        break;

                    default:
                        File.WriteAllText(fullPath, string.Empty);
                        break;
                }
            });

            // Wait a moment for file watcher to pick up changes
            await Task.Delay(100);

            return GetAsset(Path.GetRelativePath(_assetsRootPath, fullPath));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetService] Failed to create asset: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Create a new asset synchronously (for backward compatibility)
    /// </summary>
    public AssetItem? CreateAsset(string name, AssetType type, AssetItem? parent = null)
    {
        try
        {
            return CreateAssetAsync(name, type, parent).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    private void CreateSceneAsset(string path)
    {
        var sceneTemplate = """
        {
          "name": "New Scene",
          "entities": [],
          "camera": {
            "position": [0.0, 0.0],
            "zoom": 1.0
          },
          "metadata": {
            "created": "{{NOW}}",
            "version": "1.0"
          }
        }
        """;
        File.WriteAllText(path, sceneTemplate.Replace("{{NOW}}", DateTime.UtcNow.ToString("O")));
    }

    private void CreatePrefabAsset(string path)
    {
        var prefabTemplate = """
        {
          "name": "New Prefab",
          "components": {
            "PrefabIdComponent": {
              "prefabId": 1000,
              "prefabName": "New Prefab"
            },
            "TagComponent": {
              "tag": "New Prefab"
            },
            "GridPositionComponent": {
              "tile": [0, 0]
            },
            "TransformComponent": {
              "localPosition": [0.0, 0.0],
              "localRotation": 0.0,
              "localScale": [1.0, 1.0],
              "worldPosition": [0.0, 0.0],
              "worldRotation": 0.0,
              "worldScale": [1.0, 1.0],
              "isDirty": true,
              "freezeTransform": false,
              "pivot": [0.5, 0.5],
              "lockX": false,
              "lockY": false,
              "lockRotation": false,
              "lockScaleX": false,
              "lockScaleY": false
            }
          },
          "metadata": {
            "created": "{{NOW}}",
            "version": "1.0"
          }
        }
        """;
        File.WriteAllText(path, prefabTemplate.Replace("{{NOW}}", DateTime.UtcNow.ToString("O")));
    }

    private void CreateScriptAsset(string path)
    {
        var scriptTemplate = """
        // {{NAME}} - Created {{NOW}}
        using System;
        using WanderSpire.Scripting;
        using WanderSpire.Scripting.Content;

        public class {{CLASS_NAME}} : IQuestDefinition
        {
            public string Id => "{{ID}}";
            public string Title => "{{NAME}}";

            public void OnStart()
            {
                Console.WriteLine("{{NAME}} started!");
            }

            public void OnTick(float dt)
            {
                // Update logic here
            }
        }

        return new {{CLASS_NAME}}();
        """;

        var className = Path.GetFileNameWithoutExtension(path).Replace(" ", "");
        var id = className.ToLowerInvariant();
        var content = scriptTemplate
            .Replace("{{NAME}}", Path.GetFileNameWithoutExtension(path))
            .Replace("{{NOW}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{{CLASS_NAME}}", className)
            .Replace("{{ID}}", id);

        File.WriteAllText(path, content);
    }

    /// <summary>
    /// Get an asset by its relative path
    /// </summary>
    public AssetItem? GetAsset(string relativePath)
    {
        return _assetCache.TryGetValue(relativePath, out var asset) ? asset : null;
    }

    /// <summary>
    /// Find assets by search criteria
    /// </summary>
    public async Task<List<AssetItem>> FindAssetsAsync(string searchTerm, AssetType? typeFilter = null)
    {
        return await Task.Run(() =>
        {
            var results = new List<AssetItem>();

            foreach (var asset in _assetCache.Values)
            {
                if (asset.Type == AssetType.Folder)
                    continue;

                if (typeFilter.HasValue && asset.Type != typeFilter.Value)
                    continue;

                if (string.IsNullOrEmpty(searchTerm) ||
                    asset.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(asset);
                }
            }

            return results.OrderBy(a => a.Name).ToList();
        });
    }

    /// <summary>
    /// Find assets synchronously (for backward compatibility)
    /// </summary>
    public List<AssetItem> FindAssets(string searchTerm, AssetType? typeFilter = null)
    {
        try
        {
            return FindAssetsAsync(searchTerm, typeFilter).GetAwaiter().GetResult();
        }
        catch
        {
            return new List<AssetItem>();
        }
    }

    /// <summary>
    /// Get all assets of a specific type
    /// </summary>
    public List<AssetItem> GetAssetsByType(AssetType assetType)
    {
        return _assetCache.Values
            .Where(a => a.Type == assetType)
            .OrderBy(a => a.Name)
            .ToList();
    }

    /// <summary>
    /// Get all prefab assets
    /// </summary>
    public List<AssetItem> GetPrefabAssets()
    {
        return GetAssetsByType(AssetType.Prefab)
            .Where(a => a.RelativePath.StartsWith("prefabs", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Delete an asset asynchronously
    /// </summary>
    public async Task<bool> DeleteAssetAsync(AssetItem asset)
    {
        try
        {
            await Task.Run(() =>
            {
                if (asset.Type == AssetType.Folder)
                {
                    if (Directory.Exists(asset.FullPath))
                    {
                        Directory.Delete(asset.FullPath, true);
                    }
                }
                else
                {
                    if (File.Exists(asset.FullPath))
                    {
                        File.Delete(asset.FullPath);
                    }
                }
            });
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetService] Failed to delete asset {asset.Name}: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Delete an asset synchronously (for backward compatibility)
    /// </summary>
    public bool DeleteAsset(AssetItem asset)
    {
        return DeleteAssetAsync(asset).Result;
    }

    /// <summary>
    /// Rename an asset asynchronously
    /// </summary>
    public async Task<bool> RenameAssetAsync(AssetItem asset, string newName)
    {
        try
        {
            await Task.Run(() =>
            {
                var directory = Path.GetDirectoryName(asset.FullPath);
                if (string.IsNullOrEmpty(directory))
                    throw new InvalidOperationException("Cannot determine directory");

                var newPath = Path.Combine(directory, newName);

                if (asset.Type == AssetType.Folder)
                {
                    Directory.Move(asset.FullPath, newPath);
                }
                else
                {
                    File.Move(asset.FullPath, newPath);
                }
            });
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetService] Failed to rename asset {asset.Name}: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Rename an asset synchronously (for backward compatibility)
    /// </summary>
    public bool RenameAsset(AssetItem asset, string newName)
    {
        return RenameAssetAsync(asset, newName).Result;
    }

    /// <summary>
    /// Get the icon for an asset type
    /// </summary>
    public static string GetAssetIcon(AssetType type)
    {
        return type switch
        {
            AssetType.Folder => "\uf07b",      // folder
            AssetType.Scene => "\uf1b2",       // cube
            AssetType.Prefab => "\uf1b3",      // cubes
            AssetType.Texture => "\uf03e",     // image
            AssetType.Audio => "\uf001",       // music
            AssetType.Font => "\uf031",        // font
            AssetType.Script => "\uf121",      // code
            AssetType.Shader => "\uf06d",      // fire
            AssetType.Text => "\uf15c",        // file-text
            _ => "\uf15b"                      // file
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _fileWatcher?.Dispose();
        _refreshSemaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}