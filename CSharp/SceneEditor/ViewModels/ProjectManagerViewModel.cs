// SceneEditor/ViewModels/ProjectManagerViewModel.cs
using ReactiveUI;
using SceneEditor.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ICommand = System.Windows.Input.ICommand;

namespace SceneEditor.ViewModels;

/// <summary>
/// View model for project management and startup
/// </summary>
public class ProjectManagerViewModel : ReactiveObject
{
    private readonly AssetService _assetService;
    private string _selectedProjectPath = string.Empty;
    private string _newProjectName = string.Empty;
    private string _newProjectPath = string.Empty;
    private bool _isCreatingProject = false;

    public ObservableCollection<RecentProject> RecentProjects { get; } = new();

    public string SelectedProjectPath
    {
        get => _selectedProjectPath;
        set => this.RaiseAndSetIfChanged(ref _selectedProjectPath, value);
    }

    public string NewProjectName
    {
        get => _newProjectName;
        set => this.RaiseAndSetIfChanged(ref _newProjectName, value);
    }

    public string NewProjectPath
    {
        get => _newProjectPath;
        set => this.RaiseAndSetIfChanged(ref _newProjectPath, value);
    }

    public bool IsCreatingProject
    {
        get => _isCreatingProject;
        set => this.RaiseAndSetIfChanged(ref _isCreatingProject, value);
    }

    public bool CanCreateProject => !string.IsNullOrWhiteSpace(NewProjectName) && !string.IsNullOrWhiteSpace(NewProjectPath);
    public bool CanOpenProject => !string.IsNullOrWhiteSpace(SelectedProjectPath);

    // Commands
    public ICommand CreateProjectCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand BrowseProjectCommand { get; }
    public ICommand BrowseNewProjectPathCommand { get; }
    public ICommand OpenRecentProjectCommand { get; }

    public event EventHandler<string>? ProjectOpened;

    public ProjectManagerViewModel(AssetService assetService)
    {
        _assetService = assetService;

        // Initialize commands
        CreateProjectCommand = ReactiveCommand.Create(CreateProject, this.WhenAnyValue(x => x.CanCreateProject));
        OpenProjectCommand = ReactiveCommand.Create(OpenProject, this.WhenAnyValue(x => x.CanOpenProject));
        BrowseProjectCommand = ReactiveCommand.Create(BrowseProject);
        BrowseNewProjectPathCommand = ReactiveCommand.Create(BrowseNewProjectPath);
        OpenRecentProjectCommand = ReactiveCommand.Create<RecentProject>(OpenRecentProject);

        // Monitor property changes
        this.WhenAnyValue(x => x.NewProjectName, x => x.NewProjectPath)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(CanCreateProject)));

        this.WhenAnyValue(x => x.SelectedProjectPath)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(CanOpenProject)));

        // Set default new project path
        NewProjectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WanderSpire Projects");

        LoadRecentProjects();
    }

    private void CreateProject()
    {
        try
        {
            IsCreatingProject = true;

            var projectPath = Path.Combine(NewProjectPath, NewProjectName);

            if (Directory.Exists(projectPath))
            {
                // TODO: Show error dialog
                Console.Error.WriteLine($"[ProjectManager] Project directory already exists: {projectPath}");
                return;
            }

            // Create project directory structure
            CreateProjectStructure(projectPath);

            // Add to recent projects
            AddRecentProject(projectPath, NewProjectName);

            // Open the new project
            ProjectOpened?.Invoke(this, projectPath);

            Console.WriteLine($"[ProjectManager] Created new project: {NewProjectName} at {projectPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProjectManager] CreateProject error: {ex}");
        }
        finally
        {
            IsCreatingProject = false;
        }
    }

    private void OpenProject()
    {
        try
        {
            if (!Directory.Exists(SelectedProjectPath))
            {
                Console.Error.WriteLine($"[ProjectManager] Project directory does not exist: {SelectedProjectPath}");
                return;
            }

            // Validate project structure
            if (!IsValidProject(SelectedProjectPath))
            {
                Console.Error.WriteLine($"[ProjectManager] Invalid project structure: {SelectedProjectPath}");
                return;
            }

            // Add to recent projects
            var projectName = Path.GetFileName(SelectedProjectPath);
            AddRecentProject(SelectedProjectPath, projectName);

            // Open the project
            ProjectOpened?.Invoke(this, SelectedProjectPath);

            Console.WriteLine($"[ProjectManager] Opened project: {SelectedProjectPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProjectManager] OpenProject error: {ex}");
        }
    }

    private void BrowseProject()
    {
        try
        {
            // TODO: Show folder picker dialog
            // For now, use a hardcoded path for testing
            var testPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "WanderSpire Projects", "TestProject");

            if (Directory.Exists(testPath))
            {
                SelectedProjectPath = testPath;
            }
            else
            {
                Console.WriteLine("[ProjectManager] No test project found, please create one first");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProjectManager] BrowseProject error: {ex}");
        }
    }

    private void BrowseNewProjectPath()
    {
        try
        {
            // TODO: Show folder picker dialog
            // For now, keep the current path
            Console.WriteLine("[ProjectManager] BrowseNewProjectPath - dialog not implemented");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProjectManager] BrowseNewProjectPath error: {ex}");
        }
    }

    private void OpenRecentProject(RecentProject? project)
    {
        if (project == null)
            return;

        try
        {
            SelectedProjectPath = project.Path;
            OpenProject();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProjectManager] OpenRecentProject error: {ex}");
        }
    }

    private void CreateProjectStructure(string projectPath)
    {
        try
        {
            // Create main directories
            Directory.CreateDirectory(projectPath);
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets"));
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets", "scenes"));
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets", "textures"));
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets", "audio"));
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets", "scripts"));
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets", "prefabs"));
            Directory.CreateDirectory(Path.Combine(projectPath, "ProjectSettings"));

            // Create project file
            var projectFile = Path.Combine(projectPath, "project.json");
            var projectData = System.Text.Json.JsonSerializer.Serialize(new
            {
                name = NewProjectName,
                version = "1.0.0",
                engineVersion = "1.0.0",
                created = DateTime.Now,
                settings = new
                {
                    defaultScene = "Assets/scenes/main.json",
                    tileSize = 32,
                    targetResolution = new { width = 1920, height = 1080 }
                }
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(projectFile, projectData);

            // Create default scene
            var defaultScenePath = Path.Combine(projectPath, "Assets", "scenes", "main.json");
            var defaultSceneData = System.Text.Json.JsonSerializer.Serialize(new
            {
                name = "Main Scene",
                entities = new object[0],
                camera = new
                {
                    position = new[] { 0.0, 0.0 },
                    zoom = 1.0
                }
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(defaultScenePath, defaultSceneData);

            // Create sample script
            var sampleScriptPath = Path.Combine(projectPath, "Assets", "scripts", "SampleScript.csx");
            var sampleScriptContent = @"// Sample Script
using System;
using WanderSpire.Scripting;

public class SampleScript
{
    public void OnStart()
    {
        Console.WriteLine(""Sample script started!"");
    }
    
    public void OnUpdate(float deltaTime)
    {
        // Update logic here
    }
}

return new SampleScript();
";
            File.WriteAllText(sampleScriptPath, sampleScriptContent);

            Console.WriteLine($"[ProjectManager] Created project structure at: {projectPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProjectManager] CreateProjectStructure error: {ex}");
            throw;
        }
    }

    private bool IsValidProject(string projectPath)
    {
        try
        {
            // Check for required files and directories
            var requiredFiles = new[]
            {
                "project.json"
            };

            var requiredDirectories = new[]
            {
                "Assets"
            };

            foreach (var file in requiredFiles)
            {
                if (!File.Exists(Path.Combine(projectPath, file)))
                {
                    Console.WriteLine($"[ProjectManager] Missing required file: {file}");
                    return false;
                }
            }

            foreach (var directory in requiredDirectories)
            {
                if (!Directory.Exists(Path.Combine(projectPath, directory)))
                {
                    Console.WriteLine($"[ProjectManager] Missing required directory: {directory}");
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProjectManager] IsValidProject error: {ex}");
            return false;
        }
    }

    private void LoadRecentProjects()
    {
        try
        {
            // TODO: Load from settings/registry
            // For now, add some sample recent projects
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var projectsPath = Path.Combine(documentsPath, "WanderSpire Projects");

            if (Directory.Exists(projectsPath))
            {
                var directories = Directory.GetDirectories(projectsPath);
                foreach (var dir in directories.Take(5)) // Limit to 5 recent projects
                {
                    if (IsValidProject(dir))
                    {
                        var projectName = Path.GetFileName(dir);
                        var lastModified = Directory.GetLastWriteTime(dir);

                        RecentProjects.Add(new RecentProject
                        {
                            Name = projectName,
                            Path = dir,
                            LastOpened = lastModified
                        });
                    }
                }
            }

            Console.WriteLine($"[ProjectManager] Loaded {RecentProjects.Count} recent projects");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProjectManager] LoadRecentProjects error: {ex}");
        }
    }

    private void AddRecentProject(string path, string name)
    {
        try
        {
            // Remove if already exists
            var existing = RecentProjects.FirstOrDefault(p => p.Path == path);
            if (existing != null)
            {
                RecentProjects.Remove(existing);
            }

            // Add to beginning
            RecentProjects.Insert(0, new RecentProject
            {
                Name = name,
                Path = path,
                LastOpened = DateTime.Now
            });

            // Keep only last 10 projects
            while (RecentProjects.Count > 10)
            {
                RecentProjects.RemoveAt(RecentProjects.Count - 1);
            }

            // TODO: Save to settings/registry
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProjectManager] AddRecentProject error: {ex}");
        }
    }

    /// <summary>
    /// Open a project and initialize the asset service
    /// </summary>
    public void InitializeProject(string projectPath)
    {
        try
        {
            // Set the assets root path
            var assetsPath = Path.Combine(projectPath, "Assets");
            _assetService.SetAssetsRoot(assetsPath);

            Console.WriteLine($"[ProjectManager] Initialized project: {projectPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ProjectManager] InitializeProject error: {ex}");
        }
    }
}

/// <summary>
/// Represents a recent project
/// </summary>
public class RecentProject : ReactiveObject
{
    private string _name = string.Empty;
    private string _path = string.Empty;
    private DateTime _lastOpened;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string Path
    {
        get => _path;
        set => this.RaiseAndSetIfChanged(ref _path, value);
    }

    public DateTime LastOpened
    {
        get => _lastOpened;
        set => this.RaiseAndSetIfChanged(ref _lastOpened, value);
    }

    public string LastOpenedText => LastOpened.ToString("yyyy-MM-dd HH:mm");
}