# WanderSpire Render Pipeline Refactor

## Overview

The rendering system has been completely refactored to provide developers with full control over render order and operations. The new system is inspired by Unity and Godot's rendering architectures and uses a command-based approach for maximum flexibility.

## Key Features

- **Command-Based Rendering**: All rendering operations are queued as commands and executed in a specific order
- **Layered Rendering**: Standard render layers (Background, Terrain, Entities, UI, etc.) with support for custom layers
- **Fine-Grained Control**: Sub-ordering within layers using order values
- **Thread-Safe**: Commands can be submitted from any thread
- **Future-Proof**: Easy to extend with new command types and rendering techniques

## Render Layers

```cpp
enum class RenderLayer : int {
    Background  = -1000,    // Clear operations, skybox
    Terrain     = 0,        // Ground tiles, background elements  
    Entities    = 100,      // Game objects, sprites, characters
    Effects     = 200,      // Particles, visual effects
    UI          = 1000,     // User interface elements
    Debug       = 2000,     // Debug overlays, gizmos
    PostProcess = 3000      // Screen effects, filters
};
```

## Usage Examples

### C++ (Native)

```cpp
#include "WanderSpire/Graphics/RenderManager.h"

// Submit a sprite
auto& renderMgr = RenderManager::Get();
renderMgr.SubmitSprite(
    textureID, 
    {100.0f, 200.0f},     // position
    {64.0f, 64.0f},       // size
    0.0f,                 // rotation
    {1.0f, 1.0f, 1.0f},   // color
    {0.0f, 0.0f},         // uv offset
    {1.0f, 1.0f},         // uv size
    RenderLayer::Entities,
    0                     // order
);

// Submit custom rendering
renderMgr.SubmitCustom([]() {
    // Your custom OpenGL calls here
    glDrawArrays(GL_TRIANGLES, 0, 3);
}, RenderLayer::Effects, 10);

// Frame rendering (typically done by engine)
{
    FrameScope frame(camera.GetViewProjectionMatrix());
    
    // Submit all your commands here
    // Commands are automatically executed when scope ends
}
```

### C# (Managed)

```csharp
using static WanderSpire.Scripting.EngineInterop;

// Submit a sprite
Render_SubmitSprite(
    engineContext,
    textureID,
    100f, 200f,          // position
    64f, 64f,            // size  
    0f,                  // rotation
    1f, 1f, 1f,          // color
    0f, 0f,              // uv offset
    1f, 1f,              // uv size
    (int)RenderLayer.Entities,
    0                    // order
);

// Submit custom rendering
RenderCallback myCallback = (userData) => {
    // Your custom rendering logic
    ImGui.Begin("My UI");
    ImGui.Text("Hello World!");
    ImGui.End();
};
Render_SubmitCustom(engineContext, myCallback, IntPtr.Zero, 
                   (int)RenderLayer.UI, 0);

// Helper methods
SubmitSprite(engineContext, textureID, x, y, width, height, RenderLayer.Entities);
SubmitColoredRect(engineContext, x, y, width, height, 1f, 0f, 0f, RenderLayer.UI);
```

## Migration Guide

### Before (Immediate Rendering)
```cpp
// Old way - immediate rendering
glClear(GL_COLOR_BUFFER_BIT);
SpriteRenderer::Get().BeginFrame(viewProjection);
SpriteRenderer::Get().DrawSprite(/* params */);
SpriteRenderer::Get().EndFrame();
```

### After (Command-Based)
```cpp
// New way - command-based
auto& renderMgr = RenderManager::Get();
renderMgr.BeginFrame(viewProjection);
renderMgr.SubmitClear();
renderMgr.SubmitSprite(/* params */, RenderLayer::Entities);
renderMgr.ExecuteFrame(); // Called automatically by engine
```

## Advanced Usage

### Custom Render Layers
```cpp
// Define custom layers between standard ones
const RenderLayer LAYER_BACKGROUND_PARALLAX = RenderLayer(-500);
const RenderLayer LAYER_FOREGROUND_EFFECTS = RenderLayer(150);
```

### Batched Rendering
```cpp
// Submit multiple sprites for batching
for (auto& sprite : sprites) {
    renderMgr.SubmitSprite(/* sprite data */, RenderLayer::Entities, sprite.zOrder);
}
```

### Conditional Rendering
```cpp
// Only submit UI commands when UI is visible
if (showUI) {
    renderMgr.SubmitCustom([this]() {
        RenderUI();
    }, RenderLayer::UI);
}
```

## Performance Considerations

- Commands are lightweight and fast to submit
- Sorting happens once per frame after all commands are queued
- OpenGL state changes are minimized through intelligent batching
- Memory allocation is minimized with object pooling

## Extension Points

The system is designed to be easily extended:

- Add new `RenderCommand` types for specialized operations
- Implement custom batching strategies  
- Add render passes (shadow maps, post-processing, etc.)
- Integrate with modern graphics APIs (Vulkan, DirectX 12)

## Thread Safety

The `RenderManager` is thread-safe for command submission, allowing background threads to queue rendering work for the main render thread.