# WanderSpire Engine

A modern, high-performance 2D game engine built in C++ with Entity-Component-System (ECS) architecture, advanced tilemap systems, and comprehensive editor tooling.

## 📑 Table of Contents

### [🚀 Native Engine (C++)](#native-engine-core)
- [Features](#🚀-features)
  - [Core Engine](#core-engine)
  - [Rendering System](#rendering-system)
  - [Tilemap System](#tilemap-system)
  - [Editor Framework](#editor-framework)
  - [Asset Management](#asset-management)
- [Architecture](#🏗️-architecture)
  - [Project Structure](#project-structure)
  - [Core Systems](#core-systems)
  - [Entity-Component-System (ECS)](#entity-component-system-ecs)
  - [Component System](#component-system)
  - [Rendering Pipeline](#rendering-pipeline)
  - [Tilemap System](#tilemap-system-1)
- [Build Instructions](#🛠️-build-instructions)
  - [Prerequisites](#prerequisites)
  - [Dependencies](#dependencies)
  - [Building](#building)
  - [CMake Configuration](#cmake-configuration)
- [Usage Examples](#🎮-usage-examples)
  - [Basic Application Setup](#basic-application-setup)
  - [Creating a Simple Scene](#creating-a-simple-scene)
  - [Custom System Implementation](#custom-system-implementation)
  - [Asset Management](#asset-management-1)
  - [Editor Integration](#editor-integration)
- [Testing](#🧪-testing)
- [Asset Pipeline](#📁-asset-pipeline)
  - [Supported Formats](#supported-formats)
  - [Directory Structure](#directory-structure)
  - [Atlas Generation](#atlas-generation)
- [Configuration](#🔧-configuration)
  - [Engine Configuration](#engine-configuration-configenginejson)
  - [Prefab Definition Example](#prefab-definition-example)
- [Performance Features](#🚀-performance-features)
  - [Memory Management](#memory-management)
  - [Rendering Optimizations](#rendering-optimizations)
  - [Threading](#threading)
- [Integration Points](#🤝-integration-points)
  - [C API Layer (EngineCore)](#c-api-layer-enginecore)
  - [Event System](#event-system)
- [Key Classes Reference](#📚-key-classes-reference)
  - [Core Classes](#core-classes)
  - [Component Categories](#component-categories)
  - [System Categories](#system-categories)
- [Future Roadmap](#🔮-future-roadmap)

### [⚡ ScriptHost Layer (C#)](#scripthost-layer)
- [Key Features](#🎯-key-features)
  - [Managed Engine Wrapper](#managed-engine-wrapper)
  - [Hot-Reloadable Scripting System](#hot-reloadable-scripting-system)
  - [Advanced Gameplay Systems](#advanced-gameplay-systems)
  - [Developer Tools](#developer-tools)
- [Architecture Overview](#🏗️-architecture-overview-1)
  - [Project Structure](#project-structure-1)
  - [Core Components](#core-components)
  - [Entity-Component-System Bridge](#entity-component-system-bridge)
  - [Behaviour System](#behaviour-system)
  - [Event-Driven Communication](#event-driven-communication)
  - [Script Data System](#script-data-system)
- [Example Game Implementation](#🎮-example-game-implementation)
  - [Core Systems](#core-systems-1)
  - [Game Features](#game-features)
- [Development Workflow](#🛠️-development-workflow)
  - [Setting Up a New Behaviour](#setting-up-a-new-behaviour)
  - [Hot-Reload Development](#hot-reload-development)
  - [Debug Tools Usage](#debug-tools-usage)
- [Configuration & Setup](#🔧-configuration--setup)
  - [Engine Integration](#engine-integration)
  - [Project Configuration](#project-configuration)
- [Performance Characteristics](#📊-performance-characteristics)
  - [Memory Management](#memory-management-1)
  - [Threading Model](#threading-model)
  - [Typical Performance](#typical-performance)
- [Advanced Features](#🔮-advanced-features)
  - [Custom Component Types](#custom-component-types)
  - [Event System Extensions](#event-system-extensions)
  - [Dynamic Script Loading](#dynamic-script-loading)
- [Getting Started](#🚀-getting-started)

### [🎨 Scene Editor (Avalonia UI)](#scene-editor)
- [Core Features](#🎨-core-features)
  - [Visual Scene Editing](#visual-scene-editing)
  - [Asset Management Pipeline](#asset-management-pipeline)
  - [Advanced Tilemap Editor](#advanced-tilemap-editor)
  - [Professional Workflow Tools](#professional-workflow-tools)
- [Architecture Overview](#🏗️-architecture-overview-2)
  - [MVVM Architecture](#mvvm-architecture)
  - [Service-Oriented Design](#service-oriented-design)
- [GameObject & Prefab System](#🎮-gameobject--prefab-system)
  - [Unified Entity Management](#unified-entity-management)
  - [Component System Integration](#component-system-integration)
- [Editor Tools System](#🛠️-editor-tools-system)
  - [Extensible Tool Architecture](#extensible-tool-architecture)
  - [Viewport Integration](#viewport-integration)
- [Asset Management](#📁-asset-management-1)
  - [File System Integration](#file-system-integration)
  - [Asset Types & Operations](#asset-types--operations)
- [Professional UI Features](#🎯-professional-ui-features)
  - [Modern Docking System](#modern-docking-system)
  - [Theme System](#theme-system)
  - [Command System](#command-system)
- [Performance & Optimization](#🔧-performance--optimization)
  - [Efficient Rendering Pipeline](#efficient-rendering-pipeline)
  - [Memory Management](#memory-management-2)
- [Getting Started](#🚀-getting-started-1)
  - [Prerequisites](#prerequisites-1)
  - [Building the Scene Editor](#building-the-scene-editor)
  - [Project Structure](#project-structure-2)
- [Usage Examples](#🎲-usage-examples)
  - [Creating a New Scene](#creating-a-new-scene)
  - [Custom Component Editor](#custom-component-editor)
  - [Asset Pipeline Integration](#asset-pipeline-integration)
- [Integration with Native Engine](#🔄-integration-with-native-engine)
  - [P/Invoke Bridge](#pinvoke-bridge)
  - [Event System Bridge](#event-system-bridge)
- [Development Status](#🐛-development-status)
  - [Implemented Features](#✅-implemented-features)
  - [Work in Progress](#🚧-work-in-progress)
  - [Planned Features](#📋-planned-features)

---

# Native Engine Core

## 🚀 Features

### Core Engine
- **ECS Architecture**: Built on EnTT for high-performance entity management
- **Modern C++17**: Clean, maintainable codebase with RAII and smart pointers
- **Cross-Platform**: SDL3-based windowing and input handling
- **OpenGL Rendering**: Hardware-accelerated 2D graphics with instanced rendering
- **Hot-Reload**: Real-time asset reloading for rapid development

### Rendering System
- **Command-Based Rendering**: Layered rendering pipeline with automatic sorting
- **Sprite Batching**: Efficient instanced rendering for tilemaps and sprites
- **Texture Atlases**: Automatic atlas generation and management
- **Spritesheet Support**: Frame-based animation system
- **Debug Overlays**: Grid visualization and entity debugging

### Tilemap System
- **Chunked Loading**: Dynamic chunk streaming for infinite worlds
- **Multi-Layer Support**: Layered tilemaps with sorting and blending
- **Collision Detection**: Built-in pathfinding and obstacle systems
- **Auto-Tiling**: Rule-based tile placement and pattern matching
- **Paint Tools**: Comprehensive tile editing with brushes and patterns

### Editor Framework
- **Command Pattern**: Full undo/redo system for all editor operations
- **Scene Hierarchy**: Tree-based scene management with parent-child relationships
- **Selection System**: Multi-selection with spatial queries
- **Asset Pipeline**: Automatic asset discovery and dependency tracking
- **Spatial Partitioning**: Quadtree-based culling and selection

### Asset Management
- **Asynchronous Loading**: Non-blocking asset loading with worker threads
- **Hot-Reload**: Real-time texture and shader recompilation
- **Prefab System**: JSON-based prefab definitions with component serialization
- **Atlas Generation**: Automatic texture atlas creation from directories

## 🏗️ Architecture

### Project Structure

```
Engine/WanderSpire/          # Core engine library
├── include/WanderSpire/     # Public headers
│   ├── Components/          # ECS components
│   ├── Core/               # Engine core systems
│   ├── ECS/                # Entity-Component-System
│   ├── Editor/             # Editor framework
│   ├── Graphics/           # Rendering systems
│   ├── Systems/            # Game systems
│   └── World/              # World management
├── src/                    # Implementation files
└── CMakeLists.txt

EngineCore/                 # C API wrapper
├── include/               # C API headers
├── src/                   # C API implementation
└── CMakeLists.txt

tests/                     # Unit tests
├── test_*.cpp            # Test files
└── CMakeLists.txt
```

### Core Systems

#### Entity-Component-System (ECS)
Built on EnTT for maximum performance:

```cpp
// Create entity
auto entity = registry.create();

// Add components
registry.emplace<TransformComponent>(entity, glm::vec2{0, 0});
registry.emplace<SpriteComponent>(entity, "atlas", "frame");
registry.emplace<GridPositionComponent>(entity, glm::ivec2{5, 3});

// Query entities
auto view = registry.view<TransformComponent, SpriteComponent>();
for (auto entity : view) {
    auto& transform = view.get<TransformComponent>(entity);
    // Process entity...
}
```

#### Component System
Over 30 specialized components including:

- **Spatial**: `TransformComponent`, `GridPositionComponent`, `SceneNodeComponent`
- **Rendering**: `SpriteComponent`, `SpriteAnimationComponent`, `LayerComponent`
- **Tilemap**: `TilemapChunkComponent`, `TilemapLayerComponent`, `TileComponent`
- **Editor**: `SelectableComponent`, `GizmoComponent`, `EditorMetadataComponent`
- **Gameplay**: `ObstacleComponent`, `PlayerTagComponent`, `FacingComponent`

#### Rendering Pipeline
Command-based rendering with automatic layer sorting:

```cpp
// Submit render commands
renderManager.SubmitSprite(textureID, position, size, rotation, color, uv, layer);
renderManager.SubmitInstanced(textureID, positions, uvRects, tileSize);

// Execute frame
renderManager.ExecuteFrame(); // Automatically sorts by layer and executes
```

#### Tilemap System
Modern ECS-based tilemap with chunked loading:

```cpp
auto& tilemapSystem = TilemapSystem::GetInstance();

// Create tilemap structure
auto tilemap = tilemapSystem.CreateTilemap(registry, "MainWorld");
auto layer = tilemapSystem.CreateTilemapLayer(registry, tilemap, "Ground");

// Set tiles
tilemapSystem.SetTile(registry, layer, {10, 5}, grassTileId);

// Automatic chunk streaming based on camera position
tilemapSystem.UpdateTilemapStreaming(registry, viewCenter, viewRadius);
```

## 🛠️ Build Instructions

### Prerequisites
- **C++17** compatible compiler (GCC 9+, Clang 10+, MSVC 2019+)
- **CMake 3.20+**
- **vcpkg** for dependency management

### Dependencies
The engine uses the following libraries (managed via vcpkg):
- **SDL3**: Windowing and input
- **OpenGL**: Graphics rendering (via glad)
- **GLM**: Mathematics library
- **EnTT**: Entity-Component-System
- **nlohmann/json**: JSON parsing
- **spdlog**: Logging
- **ImGui**: Debug UI (optional)

### Building

1. **Clone the repository**:
```bash
git clone https://github.com/yourusername/wanderspire.git
cd wanderspire
```

2. **Setup vcpkg** (if not already installed):
```bash
git clone https://github.com/Microsoft/vcpkg.git
./vcpkg/bootstrap-vcpkg.sh
```

3. **Configure and build**:
```bash
cmake --preset=default
cmake --build build
```

### CMake Configuration

The project uses modern CMake with presets. Key targets:

- **WanderSpire**: Main engine static library
- **EngineCore**: C API wrapper (for managed integration)
- **tests**: Unit test suite

## 🎮 Usage Examples

### Basic Application Setup

```cpp
#include "WanderSpire/Core/Application.h"

int main(int argc, char* argv[]) {
    // SDL3 main loop - engine handles everything
    return SDL_AppInit(&Application::AppInit, 
                      &Application::AppEvent,
                      &Application::AppIterate, 
                      &Application::AppQuit);
}
```

### Creating a Simple Scene

```cpp
void InitializeScene(AppState* state) {
    auto& registry = state->world.GetRegistry();
    auto& prefabManager = state->ctx.prefabs;
    
    // Create tilemap
    auto& tilemapSystem = TilemapSystem::GetInstance();
    auto tilemap = tilemapSystem.CreateTilemap(registry, "MainWorld");
    auto groundLayer = tilemapSystem.CreateTilemapLayer(registry, tilemap, "Ground");
    
    // Set some tiles
    for (int x = 0; x < 20; ++x) {
        for (int y = 0; y < 20; ++y) {
            tilemapSystem.SetTile(registry, groundLayer, {x, y}, 1); // Grass tile
        }
    }
    
    // Spawn entities from prefabs
    auto player = prefabManager.Instantiate("player", registry, {10, 10});
    auto enemy = prefabManager.Instantiate("orc", registry, {15, 15});
    
    // Set camera target
    state->SetCameraTarget(player);
}
```

### Custom System Implementation

```cpp
class CustomSystem {
public:
    static void Update(entt::registry& registry, float deltaTime) {
        auto view = registry.view<TransformComponent, MyCustomComponent>();
        
        for (auto entity : view) {
            auto& transform = view.get<TransformComponent>(entity);
            auto& custom = view.get<MyCustomComponent>(entity);
            
            // Update logic here
            transform.localPosition += custom.velocity * deltaTime;
        }
    }
};

// Register with world update loop
void World::Update(float deltaTime, EngineContext& ctx) {
    CustomSystem::Update(m_Registry, deltaTime);
    // ... other systems
}
```

### Asset Management

```cpp
// Automatic texture loading
auto& resourceManager = RenderResourceManager::Get();
resourceManager.RegisterTexture("player_texture", "sprites/player.png");
resourceManager.RegisterAtlas("terrain", "terrain_atlas.png", "terrain_atlas.json");

// Hot-reload support via FileWatcher
FileWatcher::Get().WatchFile("assets/config.json", []() {
    spdlog::info("Config file changed, reloading...");
    // Reload logic
});
```

### Editor Integration

```cpp
// Command pattern for undo/redo
auto& commandHistory = CommandHistory::GetInstance();

// Move entities with undo support
auto moveCommand = std::make_unique<MoveCommand>(registry, entities, delta);
commandHistory.ExecuteCommand(std::move(moveCommand));

// Selection management
auto& selectionManager = SelectionManager::GetInstance();
selectionManager.SelectEntity(registry, entity);
selectionManager.SelectInBounds(registry, minBounds, maxBounds);
```

## 🧪 Testing

The engine includes comprehensive unit tests covering:

- **Reflection System**: Component serialization and metadata
- **Pathfinding**: A* algorithm and tilemap navigation
- **Prefab System**: JSON loading and entity instantiation
- **Serialization**: Save/load functionality

Run tests with:
```bash
cd build
ctest --verbose
```

## 📁 Asset Pipeline

### Supported Formats
- **Images**: PNG, JPG, JPEG
- **Scenes**: JSON with component serialization
- **Prefabs**: JSON with component definitions
- **Shaders**: GLSL vertex/fragment shaders
- **Configurations**: JSON config files

### Directory Structure
```
Assets/
├── textures/           # Individual sprites
│   ├── environment/    # Organized by category
│   ├── mobs/
│   └── terrain/
├── SpriteSheets/       # Animation spritesheets
├── prefabs/           # Entity prefabs
├── maps/              # Scene files
└── shaders/           # GLSL shaders
```

### Atlas Generation
The engine automatically generates texture atlases from directories:

```cpp
// Generates terrain_atlas.png and terrain_atlas.json
resourceManager.GenerateAtlases("textures");

// Individual spritesheets (for animations)
resourceManager.RegisterSpritesheets("SpriteSheets");
```

## 🔧 Configuration

### Engine Configuration (config/engine.json)
```json
{
    "tileSize": 64.0,
    "tickInterval": 0.016,
    "chunkSize": 32,
    "assetsRoot": "Assets/",
    "mapsRoot": "Assets/maps/"
}
```

### Prefab Definition Example
```json
{
    "name": "player",
    "components": {
        "TransformComponent": {
            "localPosition": [0, 0],
            "localScale": [1, 1]
        },
        "SpriteAnimationComponent": {
            "frameWidth": 64,
            "frameHeight": 64,
            "frameCount": 4,
            "frameDuration": 0.2
        },
        "GridPositionComponent": {
            "tile": [0, 0]
        },
        "PlayerTagComponent": {}
    }
}
```

## 🚀 Performance Features

### Memory Management
- **Smart Pointers**: RAII-based resource management
- **Object Pooling**: Efficient entity recycling
- **Chunk Streaming**: Load only visible world sections
- **Asset Caching**: Shared texture and resource references

### Rendering Optimizations
- **Instanced Rendering**: Single draw call for similar objects
- **Frustum Culling**: Only render visible entities
- **Layer Sorting**: Automatic depth sorting
- **Texture Atlasing**: Reduce texture binding overhead

### Threading
- **Async Asset Loading**: Non-blocking resource loading
- **Worker Thread Pool**: Background processing
- **Main Thread Safety**: Clean separation of concerns

## 🤝 Integration Points

The native engine is designed to integrate with managed languages:

### C API Layer (EngineCore)
```c
// C API for managed integration
WANDERSPIRE_API void Engine_Init(int width, int height);
WANDERSPIRE_API void Engine_Update(float deltaTime);
WANDERSPIRE_API void Engine_Render();
WANDERSPIRE_API EntityId Engine_CreateEntity();
WANDERSPIRE_API void Engine_AddComponent(EntityId id, const char* type, const char* data);
```

### Event System
```cpp
// Global event bus for loose coupling
EventBus::Get().Subscribe<MoveCompletedEvent>([](const auto& event) {
    // Handle movement completion
});

EventBus::Get().Publish<AttackEvent>({attacker, target, damage});
```

## 📚 Key Classes Reference

### Core Classes
- **`Application`**: Main application lifecycle and SDL integration
- **`World`**: ECS world container and system coordinator
- **`RenderManager`**: Command-based rendering pipeline
- **`TilemapSystem`**: Modern chunked tilemap management
- **`PrefabManager`**: JSON-based entity templates

### Component Categories
- **Transform**: Position, rotation, scale, hierarchy
- **Rendering**: Sprites, animations, layers, culling
- **Tilemap**: Chunks, layers, brushes, auto-tiling
- **Editor**: Selection, gizmos, metadata, commands
- **Gameplay**: Player tags, obstacles, AI behaviors

### System Categories
- **Core**: Animation, rendering, input, ticking
- **Editor**: Commands, selection, spatial partitioning
- **World**: Pathfinding, chunk streaming, tile management

## 🔮 Future Roadmap

- **Audio System**: OpenAL-based sound management
- **Physics Integration**: Box2D or custom 2D physics
- **Networking**: Multiplayer support with state synchronization
- **Scripting**: Lua or JavaScript integration for gameplay logic
- **Performance Profiler**: Built-in performance analysis tools

---

# ScriptHost Layer

## C# Managed Integration & Scripting System

The ScriptHost layer provides a comprehensive C# managed wrapper around the native WanderSpire engine, enabling hot-reloadable gameplay logic, dynamic scripting, and seamless interop between native C++ performance and managed C# productivity.

## 🎯 Key Features

### Managed Engine Wrapper
- **Complete P/Invoke API**: Full C# bindings for all native engine functionality
- **Type-Safe Components**: Strongly-typed DTOs mirroring native ECS components
- **Memory-Safe Operations**: Automatic marshaling and resource management
- **Cross-Platform**: Runs on Windows, Linux, and macOS through .NET 8

### Hot-Reloadable Scripting System
- **Dynamic Script Loading**: CSX scripts loaded at runtime with automatic recompilation
- **Behaviour System**: Unity-style MonoBehaviour pattern for entity logic
- **Live Code Updates**: Modify scripts while the game is running without restart
- **Script Data Persistence**: Entity script state survives hot-reloads

### Advanced Gameplay Systems
- **ECS Integration**: Direct access to EnTT registry through managed wrappers
- **Event-Driven Architecture**: Type-safe managed event bus with native event forwarding
- **Interpolation System**: Smooth 60fps visual movement over logical tick-based updates
- **AI Framework**: Modular AI behaviors with pathfinding and faction systems
- **Combat System**: Damage calculation, health management, and death/respawn logic

### Developer Tools
- **Comprehensive Debug UI**: 18+ ImGui debug windows for real-time engine inspection
- **Performance Profiling**: Frame timing, memory usage, and system performance monitoring
- **Entity Inspector**: Live component editing and scene graph visualization
- **Asset Hot-Reload**: Automatic asset reloading on file changes

## 🏗️ Architecture Overview

### Project Structure

```
CSharp/
├── ScriptHost/           # Core managed engine wrapper
├── Game/                 # Example game implementation
└── Player/              # Executable game player
```

### Core Components

#### ScriptHost Assembly
The foundational layer providing engine integration:

- **EngineInterop.cs**: Complete P/Invoke bindings for native API
- **Entity.cs**: Managed entity wrapper with component access
- **EventBus.cs**: Native event forwarding and managed event system
- **Input.cs**: SDL3-based input handling with Unity-style API
- **ScriptEngine.cs**: Dynamic script compilation and hot-reload system

#### Entity-Component-System Bridge

```csharp
// Access any ECS component through reflection
var position = entity.GetComponent<GridPositionComponent>("GridPositionComponent");

// Modify components with automatic serialization
entity.SetComponent("TransformComponent", new TransformComponent {
    LocalPosition = new[] { worldX, worldY },
    LocalScale = new[] { 1.0f, 1.0f }
});

// Type-safe field access
float x = entity.GetField<float>("TransformComponent", "localPosition.x");
entity.SetField("GridPositionComponent", "tile.x", newX);
```

#### Behaviour System

```csharp
public class PlayerController : Behaviour
{
    protected override void Start()
    {
        // One-time initialization
        GameEventBus.Event<TileClickEvent>.Subscribe(OnTileClick);
    }

    public override void Update(float dt)
    {
        // Per-frame update logic
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Handle input
        }
    }

    private void OnTileClick(TileClickEvent ev)
    {
        // Move player to clicked tile
        GameEventBus.Event<MovementIntentEvent>.Publish(
            new MovementIntentEvent((uint)Entity.Id, ev.X, ev.Y, running: false));
    }
}
```

### Event-Driven Communication

The system uses a hybrid event architecture:

#### Native Events (Engine → Managed)
- **LogicTickEvent**: Precise timing for game logic
- **MoveCompletedEvent**: Entity movement notifications  
- **AnimationFinishedEvent**: Animation state management
- **FrameRenderEvent**: Per-frame rendering callbacks

#### Managed Events (Game Logic)
- **AttackEvent**: Combat system integration
- **HurtEvent**: Damage and health management
- **DeathEvent**: Entity lifecycle management
- **TileClickEvent**: Player interaction handling

### Script Data System

Persistent entity data that survives script hot-reloads:

```csharp
// Store complex data structures
entity.SetScriptData("AIParams", new AIParams {
    wanderRadius = 5.0f,
    chaseRange = 10,
    state = (int)AIState.Idle
});

// Retrieve and modify
var aiData = entity.GetScriptData<AIParams>("AIParams");
aiData.state = (int)AIState.Chase;
entity.SetScriptData("AIParams", aiData);
```

## 🎮 Example Game Implementation

The included Game assembly demonstrates a complete 2D RPG implementation:

### Core Systems

#### Movement System
```csharp
public sealed class MovementSystem : ITickReceiver
{
    public void OnTick(float dt)
    {
        // Process movement requests on logic ticks
        // Generate smooth pathfinding
        // Update grid positions discretely
    }
}
```

#### Interpolation System  
```csharp
public sealed class InterpolationSystem : ITickReceiver
{
    private void OnFrameRender(FrameRenderEvent _)
    {
        // Smooth 60fps interpolation between logic ticks
        // Continuous path-based movement
        // Visual position separate from logical position
    }
}
```

#### AI Behaviour
```csharp
public class AIBehaviour : Behaviour
{
    private enum State { Idle, Wander, Chase, Attack, Return, Dead }
    
    public override void Update(float dt)
    {
        // Faction-based hostility detection
        // A* pathfinding integration
        // State machine AI logic
        // Combat and leashing systems
    }
}
```

### Game Features

- **Grid-Based Movement**: Discrete tile movement with smooth visual interpolation
- **Combat System**: Turn-based combat with damage calculation and animations  
- **AI Behaviors**: Faction-based NPCs with wandering, chasing, and combat
- **Scene Management**: JSON-based scene loading with entity persistence
- **Asset Pipeline**: Automatic prefab loading and hot-reload support

## 🛠️ Development Workflow

### Setting Up a New Behaviour

1. **Create the Script**:
```csharp
public class MyBehaviour : Behaviour
{
    protected override void Start() { /* Initialize */ }
    public override void Update(float dt) { /* Update logic */ }
}
```

2. **Register in Factory**:
```csharp
// Automatically discovered via reflection in ScriptRegistry
// No manual registration needed
```

3. **Attach to Prefab**:
```json
{
  "name": "my_entity",
  "components": {
    "ScriptsComponent": {
      "scripts": ["MyBehaviour"]
    }
  }
}
```

### Hot-Reload Development

```csharp
// Modify any Behaviour code and save
// ScriptEngine automatically:
// 1. Detects file changes
// 2. Recompiles scripts
// 3. Preserves entity script data
// 4. Rebinds new behaviours to entities
// 5. Continues execution seamlessly
```

### Debug Tools Usage

```csharp
// Toggle debug UI with tilde key
// Press ` to open comprehensive debug interface
// F12 for render statistics overlay

// Programmatic debug access
DebugUISystem.Instance?.WantsInput(); // Check if ImGui capturing input
```

## 🔧 Configuration & Setup

### Engine Integration

```csharp
// Initialize ScriptHost
ContentPaths.Initialize(assetsRoot);
Engine.Initialize(nativeContext);

// Create script engine with hot-reload
var scriptEngine = new ScriptEngine(scriptsDirectory, nativeContext);
scriptEngine.BehaviourFactory = BehaviourFactory.Resolve;

// Load scene with script binding
SceneManager.Load("Assets/maps/example.json");
```

### Project Configuration

**ScriptHost.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Scripting" Version="4.8.0" />
    <Content Include="../../Assets/Scripts/**/*.csx" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

## 📊 Performance Characteristics

### Memory Management
- **Zero-Copy Interop**: Direct memory access for component data
- **Managed Object Pooling**: Automatic entity and component recycling
- **Script Data Persistence**: Minimal allocation during hot-reloads
- **Native Resource Tracking**: Automatic cleanup of engine resources

### Threading Model
- **Main Thread**: All managed code and script execution
- **Logic Ticks**: Precise timing on dedicated thread with marshaling
- **Render Thread**: Native rendering with managed overlay submission
- **Asset Loading**: Background asset streaming with main thread sync

### Typical Performance
- **Logic Tick Rate**: 0.6s intervals (RuneScape-style)
- **Render Rate**: 60fps with smooth interpolation
- **Script Reload Time**: <100ms for typical behaviour changes
- **Memory Usage**: ~50MB managed heap for complex scenes

## 🔮 Advanced Features

### Custom Component Types

```csharp
[Serializable]
public class CustomComponent
{
    public string Data { get; set; }
    public int Value { get; set; }
}

// Automatic serialization/deserialization
entity.SetComponent("CustomComponent", customData);
var data = entity.GetComponent<CustomComponent>("CustomComponent");
```

### Event System Extensions

```csharp
// Create custom event types
public struct CustomEvent
{
    public uint EntityId;
    public string Message;
}

// Type-safe subscription
GameEventBus.Event<CustomEvent>.Subscribe(OnCustomEvent);
GameEventBus.Event<CustomEvent>.Publish(new CustomEvent { ... });
```

### Dynamic Script Loading

```csharp
// Load quest scripts at runtime
var quests = ContentLoader.LoadQuests();
foreach (var quest in quests)
{
    quest.OnStart(); // Execute dynamic quest logic
}
```

## 🚀 Getting Started

1. **Build the Engine**: Ensure native WanderSpire engine is compiled
2. **Open Solution**: Load `CSharp/WanderSpire.sln` in Visual Studio
3. **Set Startup Project**: Configure `Player` as the startup project
4. **Run**: Press F5 to launch with full hot-reload support
5. **Experiment**: Modify scripts in `Game/Behaviours/` and see live updates

The ScriptHost layer transforms the native WanderSpire engine into a powerful, accessible game development platform while maintaining the performance characteristics needed for complex 2D games and simulations.

---

# Scene Editor

## C# Avalonia-based Visual Editor & Asset Management System

The Scene Editor is a comprehensive, cross-platform visual editor built on Avalonia UI that provides a professional interface for creating and managing WanderSpire game content. It combines the power of the native engine with the productivity of modern UI frameworks.

## 🎨 Core Features

### Visual Scene Editing
- **Unified GameObject System**: Edit both scene instances and prefab definitions in a single interface
- **Real-time Viewport**: Hardware-accelerated OpenGL rendering with live scene preview
- **Multi-Tool Editor**: Professional tool system for selection, transformation, and tile painting
- **Component Inspector**: Visual editing of entity components with type-safe property editors
- **Scene Hierarchy**: Tree-based scene management with drag-and-drop organization

### Asset Management Pipeline
- **Modern Asset Browser**: Tree, grid, and list views with real-time search and filtering
- **Hot-Reload Support**: Automatic asset reloading when files change on disk
- **Asset Creation Wizards**: Built-in templates for scenes, prefabs, scripts, and more
- **Cross-Reference Tracking**: Dependency management and asset relationship visualization
- **Import Pipeline**: Drag-and-drop importing with automatic format detection

### Advanced Tilemap Editor
- **Visual Tile Painting**: Brush-based tile placement with multiple brush types and sizes
- **Layer Management**: Multi-layer tilemap support with visibility and lock controls
- **Tile Palette System**: Visual tile selection with atlas-based organization
- **Auto-Tiling Support**: Rule-based tile placement for efficient level design
- **Collision Editing**: Visual editing of tile collision properties

### Professional Workflow Tools
- **Command System**: Full undo/redo support for all editor operations
- **Docking Layout**: Customizable, resizable panel system with save/restore
- **Theme System**: Light/Dark theme support with system integration
- **Project Management**: Project creation, templates, and recent project tracking
- **Performance Monitoring**: Real-time performance metrics and debugging tools

## 🏗️ Architecture Overview

### MVVM Architecture
The Scene Editor follows the Model-View-ViewModel pattern with ReactiveUI for robust data binding:

```
Views/               # Avalonia XAML UI definitions
├── MainWindow       # Primary editor interface
├── StartupWindow    # Project selection and startup
├── Panels/          # Dockable editor panels
    ├── ViewportPanel      # 3D/2D scene rendering
    ├── GameObjectPanel    # Hierarchy & prefab management
    ├── InspectorPanel     # Component property editing
    ├── AssetBrowserPanel  # Asset management interface
    └── ToolboxPanel       # Tool selection interface

ViewModels/          # Business logic and data binding
├── MainWindowViewModel    # Application orchestration
├── ViewportViewModel      # Scene rendering and tools
├── GameObjectViewModel    # Entity/prefab management
├── InspectorViewModel     # Component editing
├── AssetBrowserViewModel  # Asset browser logic
└── ComponentEditors/      # Type-specific property editors

Services/            # Core business services
├── EditorEngine          # Native engine integration
├── GameObjectService     # Scene/prefab management
├── AssetService          # Asset pipeline
├── CommandService        # Undo/redo system
├── ToolService           # Editor tool management
├── ThemeService          # UI theming
└── TilemapService        # Tilemap editing
```

### Service-Oriented Design
Core functionality is organized into focused services with dependency injection:

- **EditorEngine**: Manages native engine lifecycle and P/Invoke bindings
- **GameObjectService**: Unified scene hierarchy and prefab management
- **AssetService**: File system monitoring, asset discovery, and hot-reload
- **CommandService**: Command pattern implementation for undo/redo
- **ThemeService**: Application-wide theme management with system detection

## 🎮 GameObject & Prefab System

### Unified Entity Management
The Scene Editor treats GameObjects and Prefabs as part of a unified system:

```csharp
// Scene Mode: Edit live entities in the scene
var player = gameObjectService.InstantiatePrefab("player", 10, 10);
gameObjectService.SelectGameObject(player);

// Prefab Mode: Edit prefab definitions
var prefab = gameObjectService.CreatePrefabFromGameObject(player, "PlayerVariant");
gameObjectService.SavePrefab(prefab);
```

### Component System Integration
Direct integration with the native ECS system:

```csharp
// Type-safe component editing
var transform = entity.GetComponent<TransformComponent>("TransformComponent");
transform.LocalPosition = new[] { newX, newY };
entity.SetComponent("TransformComponent", JsonSerializer.Serialize(transform));

// Visual property editors for each component type
public class TransformComponentEditor : ComponentEditorViewModel
{
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float RotationDegrees { get; set; }
    // Automatic UI generation and data binding
}
```

## 🛠️ Editor Tools System

### Extensible Tool Architecture
The editor provides a plugin-like tool system:

```csharp
public interface IEditorTool
{
    string Name { get; }
    string DisplayName { get; }
    string Icon { get; }
    
    void OnActivate();
    void OnMouseDown(float worldX, float worldY, ViewportInputModifiers modifiers);
    void OnDrag(float worldX, float worldY, ViewportInputModifiers modifiers);
}

// Built-in tools
- SelectTool: Entity selection and multi-selection
- MoveTool: Transform manipulation with gizmos
- RotateTool: Rotation handles and snap-to-angle
- ScaleTool: Non-uniform scaling with aspect lock
- TilePaintTool: Brush-based tile painting
- TileEraseTool: Tile removal and cleanup
- PrefabPlacementTool: Drag-and-drop prefab instantiation
```

### Viewport Integration
Tools receive viewport events and can render custom overlays:

```csharp
// Mouse input with world-space coordinates
tool.OnMouseDown(worldX, worldY, modifiers);

// Coordinate system helpers
var worldPos = viewport.ScreenToWorld(mouseX, mouseY);
var snappedPos = viewport.SnapToGrid(worldX, worldY);
var bounds = viewport.GetViewportBounds();
```

## 📁 Asset Management

### File System Integration
The Asset Browser provides comprehensive file management:

```csharp
// Automatic asset discovery
assetService.SetAssetsRoot("Assets/");
await assetService.RefreshAssetsAsync();

// Hot-reload monitoring
assetService.PrefabChanged += (sender, prefabName) => {
    // Automatically reload changed prefabs
    prefabRegistry.ReloadPrefab(prefabName);
};

// Asset creation with templates
var newScene = await assetService.CreateAssetAsync("Level1", AssetType.Scene);
var newPrefab = await assetService.CreateAssetAsync("Enemy", AssetType.Prefab);
```

### Asset Types & Operations
Comprehensive support for game asset types:

- **Scenes**: JSON-based scene definitions with entity hierarchies
- **Prefabs**: Reusable entity templates with component presets
- **Textures**: PNG, JPG image assets with automatic atlas detection
- **Scripts**: C# script files with hot-reload compilation
- **Audio**: Sound effects and music files
- **Fonts**: Typography assets for UI systems
- **Shaders**: GLSL shader programs for custom rendering

## 🎯 Professional UI Features

### Modern Docking System
Built on Dock.Avalonia for professional layout management:

```csharp
// Flexible panel arrangement
var layout = dockFactory.CreateLayout();
layout.ActiveDockable = viewportDocument;

// Save/restore user layouts
layoutManager.SaveUserLayout("Default");
layoutManager.RestoreUserLayout("Default");
```

### Theme System
Comprehensive theming with system integration:

```csharp
// Automatic system theme detection
themeService.InitializeTheme(); // Detects Windows/macOS/Linux preferences

// Manual theme switching
themeService.SetTheme(isDark: true);
themeService.ToggleTheme();

// Theme-aware resources
<SolidColorBrush x:Key="PrimaryTextBrush" Color="{DynamicResource PrimaryTextColor}" />
```

### Command System
Professional undo/redo with command pattern:

```csharp
// Undoable operations
var moveCommand = new EntityMoveCommand(engine, entity, deltaX, deltaY);
commandService.ExecuteCommand(moveCommand);

// Full history management
if (commandService.CanUndo)
    commandService.Undo();
```

## 🔧 Performance & Optimization

### Efficient Rendering Pipeline
- **Hardware Acceleration**: OpenGL-based viewport with 60fps target
- **Frustum Culling**: Only render visible entities and tiles
- **Instanced Rendering**: Batch similar objects for GPU efficiency
- **Progressive Loading**: Stream large scenes without blocking UI

### Memory Management
- **Smart Caching**: Asset caching with automatic cleanup
- **Object Pooling**: Reuse UI elements and temporary objects
- **Lazy Loading**: Load assets on-demand to reduce startup time
- **Hot-Reload Optimization**: Minimal recompilation during development

## 🚀 Getting Started

### Prerequisites
- **.NET 8.0** or later
- **Visual Studio 2022** or **JetBrains Rider**
- **Native WanderSpire Engine** (built separately)

### Building the Scene Editor

1. **Clone and build the native engine first**:
```bash
cd WanderSpire/
cmake --preset=default
cmake --build build --config Release
```

2. **Build the Scene Editor**:
```bash
cd CSharp/SceneEditor/
dotnet restore
dotnet build -c Release
```

3. **Run the editor**:
```bash
dotnet run --project SceneEditor
```

### Project Structure
```
SceneEditor/
├── Views/              # XAML UI definitions
├── ViewModels/         # MVVM business logic
├── Services/           # Core services and engine integration
├── Models/             # Data models and DTOs
├── Tools/              # Editor tool implementations
├── Controls/           # Custom UI controls
├── Resources/          # Assets, icons, and themes
└── Infrastructure/     # Utilities and helpers
```

## 🎲 Usage Examples

### Creating a New Scene

```csharp
// 1. Create a new scene
gameObjectService.NewScene();

// 2. Add some GameObjects
var player = gameObjectService.InstantiatePrefab("player", 0, 0);
var enemy = gameObjectService.InstantiatePrefab("orc", 5, 3);

// 3. Set up the tilemap
tilemapService.SetTile(0, 0, 1); // Place grass tile
tilemapService.SetTile(1, 0, 2); // Place dirt tile

// 4. Save the scene
gameObjectService.SaveScene("Assets/scenes/level1.json");
```

### Custom Component Editor

```csharp
public class HealthComponentEditor : ComponentEditorViewModel
{
    private int _maxHealth = 100;
    private int _currentHealth = 100;
    
    public int MaxHealth
    {
        get => _maxHealth;
        set => this.RaiseAndSetIfChanged(ref _maxHealth, value);
    }
    
    public int CurrentHealth
    {
        get => _currentHealth;
        set => this.RaiseAndSetIfChanged(ref _currentHealth, value);
    }
    
    protected override void LoadFromJson(string json)
    {
        var data = JsonSerializer.Deserialize<HealthData>(json);
        MaxHealth = data.MaxHealth;
        CurrentHealth = data.CurrentHealth;
    }
    
    protected override string SaveToJson()
    {
        return JsonSerializer.Serialize(new HealthData 
        { 
            MaxHealth = MaxHealth, 
            CurrentHealth = CurrentHealth 
        });
    }
}
```

### Asset Pipeline Integration

```csharp
// Monitor for asset changes
assetService.AssetsRefreshed += OnAssetsRefreshed;
assetService.PrefabChanged += OnPrefabChanged;

// Create assets programmatically
var questScript = await assetService.CreateAssetAsync(
    "MainQuest", AssetType.Script, scriptsFolder);

// Batch asset operations
var scenes = assetService.GetAssetsByType(AssetType.Scene);
foreach (var scene in scenes)
{
    await ValidateSceneIntegrity(scene);
}
```

## 🔄 Integration with Native Engine

### P/Invoke Bridge
Seamless integration with the native C++ engine:

```csharp
[DllImport("EngineCore")]
public static extern EntityId CreateEntity(IntPtr context);

[DllImport("EngineCore")]
public static extern int SetComponentJson(IntPtr context, EntityId entity, 
    [MarshalAs(UnmanagedType.LPStr)] string componentType,
    [MarshalAs(UnmanagedType.LPStr)] string jsonData);

// High-level wrapper
public class Entity
{
    public T GetComponent<T>(string componentType)
    {
        var json = GetComponentJson(engineContext, entityId, componentType);
        return JsonSerializer.Deserialize<T>(json);
    }
}
```

### Event System Bridge
Native events forwarded to managed code:

```csharp
// Native → Managed event forwarding
engine.EngineInitialized += OnEngineReady;
engine.EntityCreated += OnEntityCreated;
engine.ComponentChanged += OnComponentChanged;

// Managed event bus
EventBus.Subscribe<EntitySelectedEvent>(OnEntitySelected);
EventBus.Publish(new SceneChangedEvent(scenePath));
```

## 🐛 Development Status

### ✅ Implemented Features
- **Core Architecture**: MVVM, services, dependency injection
- **Basic Viewport**: OpenGL rendering, camera controls
- **GameObject System**: Scene hierarchy, prefab management
- **Asset Browser**: File management, hot-reload
- **Theme System**: Light/dark themes with system detection
- **Command System**: Undo/redo infrastructure
- **Tool Framework**: Extensible tool system
- **Docking Layout**: Professional panel management

### 🚧 Work in Progress
- **Advanced Component Editors**: Type-specific property editing
- **Tilemap Editor**: Visual tile painting and palette management
- **Animation Timeline**: Keyframe-based animation editing
- **Script Debugging**: Integrated C# script debugging
- **Performance Profiling**: Advanced performance analysis tools
- **Plugin System**: Third-party tool and extension support

### 📋 Planned Features
- **Live Rendering**: Real Time hot reload and scene building
- **Visual Scripting**: Node-based logic editing
- **Terrain Editor**: Height-based terrain sculpting
- **Particle System**: Visual effect creation and editing
- **Audio Tools**: Sound placement and ambient audio zones
- **Lighting Editor**: Dynamic lighting and shadow configuration
- **Build Pipeline**: Asset packaging and deployment tools

The WanderSpire Scene Editor bridges the gap between the high-performance native engine and the productivity needs of game developers, providing a professional, extensible platform for creating 2D game content.