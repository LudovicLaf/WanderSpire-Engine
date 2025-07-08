#pragma once

#include "WanderSpire/Core/Application.h"
#include "WanderSpire/Core/ConfigManager.h"
#include "WanderSpire/Core/AssetManager.h"
#include "WanderSpire/Core/SDLContext.h"
#include "WanderSpire/Core/GLObjects.h"
#include "WanderSpire/Core/AssetLoader.h"
#include "WanderSpire/Core/FileWatcher.h"
#include "WanderSpire/Core/EventBus.h"
#include "WanderSpire/Core/Events.h"

#include "WanderSpire/Graphics/Camera2D.h"
#include "WanderSpire/Graphics/RenderResourceManager.h"
#include "WanderSpire/Graphics/SpriteRenderer.h"
#include "WanderSpire/Graphics/RenderManager.h"
#include "WanderSpire/Graphics/OpenGLDebug.h"

#include "WanderSpire/Editor/EditorSystems.h"
#include "WanderSpire/Editor/TilePaint/TilePaintSystems.h"
#include "WanderSpire/World/TilemapSystem.h"
#include "WanderSpire/Systems/RenderSystem.h"
#include "WanderSpire/Systems/TickSystem.h"

#include "WanderSpire/Input/InputManager.h"

#include "WanderSpire/ECS/PrefabManager.h"
#include "WanderSpire/Scene/SceneManager.h"

#include "WanderSpire/World/Pathfinder2D.h"
#include "WanderSpire/Components/AllComponents.h"

#include <SDL3/SDL.h>
#include <glm/gtc/matrix_transform.hpp>
#include <spdlog/spdlog.h>
#include <nlohmann/json.hpp>
#include <cstdint>
#include <fstream>
#include <filesystem>

namespace {
	struct PerformanceTracker {
		std::chrono::high_resolution_clock::time_point frameStart;
		std::chrono::high_resolution_clock::time_point renderStart;
		std::chrono::high_resolution_clock::time_point updateStart;
		float lastFrameTime = 0.0f;
		float lastRenderTime = 0.0f;
		float lastUpdateTime = 0.0f;
		int frameDrawCalls = 0;
	};

	static PerformanceTracker g_perfTracker;
}

using namespace entt;
using json = nlohmann::json;
extern uint64_t g_nextUuid = 1;

// Forward‐declare the overlay flush, which is called once per frame
extern "C" void Engine_OverlayPresent(void);

namespace WanderSpire {
	// Static members for OpenGL state tracking
	bool Application::s_immediateModeEnabled = false;
	bool Application::s_headlessMode = false;

	// Global flag to track if we're in editor mode
	static bool g_editorMode = false;

	// Performance accessor functions (already exist, just ensure they're accessible)
	float Application::GetLastFrameTime() { return g_perfTracker.lastFrameTime; }
	float Application::GetLastRenderTime() { return g_perfTracker.lastRenderTime; }
	float Application::GetLastUpdateTime() { return g_perfTracker.lastUpdateTime; }
	int Application::GetLastFrameDrawCalls() { return g_perfTracker.frameDrawCalls; }

	// Add this function to set editor mode
	void Application::SetEditorMode(bool enabled) {
		g_editorMode = enabled;
		spdlog::info("[Application] Editor mode: {}", enabled ? "enabled" : "disabled");
	}

	bool Application::IsEditorMode() {
		return g_editorMode;
	}

	SDL_GLContext Application::GetCurrentGLContext() {
		return SDL_GL_GetCurrentContext();
	}

	bool Application::IsHeadlessMode() {
		return s_headlessMode;
	}

	void Application::SetImmediateModeRendering(bool enabled) {
		s_immediateModeEnabled = enabled;
		if (enabled) {
			spdlog::info("[Application] Enabled immediate mode rendering (fallback)");
		}
		else {
			spdlog::info("[Application] Disabled immediate mode rendering (using command system)");
		}
		// Could potentially modify RenderManager behavior here
	}

	AppState* Application::GetState(void* raw) {
		return reinterpret_cast<AppState*>(raw);
	}

	SDL_AppResult Application::AppInit(void** appstate, int argc, char* argv[])
	{
		spdlog::info("=== Application::AppInit ===");

		auto* state = new AppState();
		*appstate = state;

		// Check for editor mode in arguments
		for (int i = 0; i < argc; ++i) {
			if (std::string(argv[i]) == "--editor" || std::string(argv[i]) == "--headless") {
				SetEditorMode(true);
				break;
			}
		}

		spdlog::set_pattern("[%T] [%^%l%$] %v");
		spdlog::set_level(spdlog::level::debug);

		// 1) Config
		spdlog::debug("[AppInit] Loading config/engine.json …");
		ConfigManager::Load("config/engine.json");
		state->ctx.settings = ConfigManager::Get();

		// 2) Sub-systems
		spdlog::debug("[AppInit] Initializing AssetManager …");
		state->ctx.assets.Initialize(state->ctx.settings.assetsRoot);

		// Skip SDL initialization in editor mode
		if (!g_editorMode) {
			spdlog::debug("[AppInit] Initializing Input …");
			InitializeInput();

			spdlog::debug("[AppInit] Initializing Rendering …");
			InitializeRendering(state);

			spdlog::debug("[AppInit] Initializing Textures …");
			InitializeTextures(state);
		}
		else {
			spdlog::info("[AppInit] Skipping SDL/OpenGL initialization (editor mode)");
		}

		spdlog::debug("[AppInit] Initializing World …");
		state->world.Initialize(state->ctx);

		// Set headless mode flag if in editor
		if (g_editorMode) {
			s_headlessMode = true;
		}

		// Load prefabs
		state->ctx.prefabs.LoadPrefabsFromFolder(
			std::filesystem::path(state->ctx.settings.assetsRoot) / "prefabs");

		spdlog::info("[AppInit] Reflection: {} types registered",
			Reflect::TypeRegistry::Get().GetNameMap().size());
		spdlog::info("=== Application initialized (editor mode: {}) ===", g_editorMode);

		return SDL_APP_CONTINUE;
	}

	/*──────────────────────── event pump ────────────────────*/
	SDL_AppResult Application::AppEvent(void* raw, SDL_Event* e)
	{
		auto* state = GetState(raw);
		InputManager::HandleEvent(*e);

		switch (e->type) {
		case SDL_EVENT_QUIT:
			return SDL_APP_SUCCESS;
		case SDL_EVENT_WINDOW_RESIZED:
			OnWindowResized(e->window.data1, e->window.data2);
			return SDL_APP_CONTINUE;
		case SDL_EVENT_KEY_DOWN:
			HandleKeyboardInput(state);
			return SDL_APP_CONTINUE;
		default:
			return SDL_APP_CONTINUE;
		}
	}

	// ─────────────────────────────────────────────────────────────────────────────
	//  Application::AppIterate
	// ─────────────────────────────────────────────────────────────────────────────
	SDL_AppResult Application::AppIterate(void* raw)
	{
		if (g_editorMode) {
			spdlog::warn("[AppIterate] Called in editor mode - use EngineIterateEditor instead");
			return SDL_APP_CONTINUE;
		}

		static size_t frame = 0; ++frame;

		// Start frame timing
		g_perfTracker.frameStart = std::chrono::high_resolution_clock::now();

		static Uint64 last = SDL_GetPerformanceCounter();
		Uint64 now = SDL_GetPerformanceCounter();
		float dt = float(now - last) / float(SDL_GetPerformanceFrequency());
		last = now;

		auto* state = GetState(raw);

		// Mouse-wheel zoom --------------------------------------------------
		const int scroll = std::exchange(InputManager::gScrollDelta, 0);
		if (scroll != 0)
		{
			constexpr float kZoomStep = 0.15f;
			camera.AddZoom(scroll * kZoomStep);
		}
		camera.Update(dt);

		// Publish camera movement for chunk streaming
		const float halfW = camera.GetWidth() * 0.5f / camera.GetZoom();
		const float halfH = camera.GetHeight() * 0.5f / camera.GetZoom();
		const glm::vec2 minB = camera.GetPosition() - glm::vec2(halfW, halfH);
		const glm::vec2 maxB = camera.GetPosition() + glm::vec2(halfW, halfH);

		EventBus::Get().Publish(CameraMovedEvent{ minB, maxB });

		// Game logic -------------------------------------------------------
		g_perfTracker.updateStart = std::chrono::high_resolution_clock::now();

		AssetLoader::Get().UpdateMainThread();
		FileWatcher::Get().Update();

		state->world.Tick(dt, state->ctx);
		state->world.Update(dt, state->ctx);

		auto updateEnd = std::chrono::high_resolution_clock::now();
		g_perfTracker.lastUpdateTime = std::chrono::duration<float, std::milli>(
			updateEnd - g_perfTracker.updateStart).count();

		// ═══════════════════════════════════════════════════════════════════
		// COMMAND-BASED RENDERING PIPELINE
		// ═══════════════════════════════════════════════════════════════════

		g_perfTracker.renderStart = std::chrono::high_resolution_clock::now();

		auto& renderMgr = RenderManager::Get();

		// Begin frame with camera matrix - this clears previous commands and sets up frame
		renderMgr.BeginFrame(camera.GetViewProjectionMatrix());

		// Publish FrameRenderEvent - all systems will submit their render commands
		EventBus::Get().Publish<FrameRenderEvent>({ state });

		// Track draw calls
		g_perfTracker.frameDrawCalls = static_cast<int>(renderMgr.GetCommandCount());

		// Execute all queued commands in the correct layer order
		renderMgr.ExecuteFrame();

		auto renderEnd = std::chrono::high_resolution_clock::now();
		g_perfTracker.lastRenderTime = std::chrono::duration<float, std::milli>(
			renderEnd - g_perfTracker.renderStart).count();

		// Swap buffers to display the rendered frame
		SDL_GL_SwapWindow(state->sdl.GetWindow());

		auto frameEnd = std::chrono::high_resolution_clock::now();
		g_perfTracker.lastFrameTime = std::chrono::duration<float, std::milli>(
			frameEnd - g_perfTracker.frameStart).count();

		InputManager::Update();
		return SDL_APP_CONTINUE;
	}

	void Application::AppQuit(void* raw, SDL_AppResult)
	{
		delete GetState(raw);
	}

	void Application::OnWindowResized(int width, int height)
	{
		glViewport(0, 0, width, height);
		camera.SetScreenSize((float)width, (float)height);
	}

	// — private helpers —

	void Application::InitializeTextures(AppState* state)
	{
		auto& rm = state->ctx.renderer;
		auto assetsRoot = std::filesystem::path(state->ctx.settings.assetsRoot);

		// Generate atlases for static sprites (unchanged)
		rm.GenerateAtlases("textures");

		// NEW: Auto-register all spritesheets from Assets/SpriteSheets/
		rm.RegisterSpritesheets("SpriteSheets");

		// Register debug textures (unchanged)
		rm.RegisterTexture("tileDebug", "textures/debug_tile.png");

		// Set up hot-reload watchers for texture directory (unchanged)
		FileWatcher::Get().WatchDirectory(
			assetsRoot / "textures",
			{ ".png", ".jpg", ".jpeg" },
			[assetsRoot, &rm](const std::filesystem::path& changed) {
				auto rel = std::filesystem::relative(changed, assetsRoot).generic_string();
				if (rel.find("_atlas.png") != std::string::npos ||
					rel.find("_atlas.json") != std::string::npos)
					return;
				rm.RegisterTexture(rel, rel);
				spdlog::info("[HotReload] Scheduled texture reload: {}", rel);
			}
		);

		// NEW: Set up hot-reload watchers for SpriteSheets directory
		FileWatcher::Get().WatchDirectory(
			assetsRoot / "SpriteSheets",
			{ ".png", ".jpg", ".jpeg" },
			[assetsRoot, &rm](const std::filesystem::path& changed) {
				auto rel = std::filesystem::relative(changed, assetsRoot / "SpriteSheets").generic_string();
				std::string fullRelativePath = "SpriteSheets/" + rel;
				rm.RegisterTexture(rel, fullRelativePath);
				spdlog::info("[HotReload] Reloaded spritesheet: {} -> {}", rel, fullRelativePath);
			}
		);

		// Set up atlas hot-reload watchers (unchanged)
		for (auto const& kv : rm.GetAtlasMap()) {
			const auto& atlasName = kv.first;
			const auto pngRel = "textures/" + atlasName + "_atlas.png";
			const auto jsonRel = "textures/" + atlasName + "_atlas.json";
			FileWatcher::Get().WatchFile(
				std::filesystem::path(state->ctx.settings.assetsRoot) / pngRel,
				[atlasName, pngRel, jsonRel]() {
					RenderResourceManager::Get().RegisterAtlas(atlasName, pngRel, jsonRel);
					spdlog::info("[HotReload] Reloaded atlas image '{}'", atlasName);
				}
			);
			FileWatcher::Get().WatchFile(
				std::filesystem::path(state->ctx.settings.assetsRoot) / jsonRel,
				[atlasName, pngRel, jsonRel]() {
					RenderResourceManager::Get().RegisterAtlas(atlasName, pngRel, jsonRel);
					spdlog::info("[HotReload] Reloaded atlas JSON '{}'", atlasName);
				}
			);
		}

		spdlog::info("[Init] Watching textures/ and SpriteSheets/ directories for hot-reload");
		spdlog::info("[Init] Found {} atlases for static sprites", rm.GetAtlasCount());
	}

	void Application::InitializeInput()
	{
		InputManager::Initialize();
		InputManager::BindAction("DebugGrid", SDLK_F10);
		InputManager::BindAction("DebugEntities", SDLK_F11);
		InputManager::SaveBindingsToFile("config/input_bindings.json");
	}

	void Application::HandleKeyboardInput(AppState* state)
	{
		auto& reg = state->world.GetRegistry();

		if (InputManager::IsActionPressed("DebugGrid"))
		{
			// Toggle debug grid rendering (now using tilemap system)
			spdlog::info("[Debug] Grid borders toggled (now using ECS tilemap system).");
		}
		if (InputManager::IsActionPressed("DebugEntities"))
		{
			state->debugEntityTiles = !state->debugEntityTiles;
			spdlog::info("[Debug] Entity tile outlines toggled.");
		}
	}

	void Application::InitializeRendering(AppState* state)
	{
#ifdef _DEBUG
		OpenGLDebug::EnableDebugContext();
#endif
		/* -----------------------------------------------------------------
		   1)  Quad geometry (shared by sprites *and* terrain instances)
		------------------------------------------------------------------*/
		glBindVertexArray(state->gl.VAO);

		static const float verts[] = {
			// pos               // uv
			 0.5f,  0.5f, 0.0f,   1.0f, 1.0f,
			 0.5f, -0.5f, 0.0f,   1.0f, 0.0f,
			-0.5f, -0.5f, 0.0f,   0.0f, 0.0f,
			-0.5f,  0.5f, 0.0f,   0.0f, 1.0f
		};
		static const unsigned idx[] = { 0, 1, 3, 1, 2, 3 };

		glBindBuffer(GL_ARRAY_BUFFER, state->gl.VBO);
		glBufferData(GL_ARRAY_BUFFER, sizeof(verts), verts, GL_STATIC_DRAW);

		glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, state->gl.EBO);
		glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(idx), idx, GL_STATIC_DRAW);

		/* per-vertex (sprite) attributes ─ locations 0-1 */
		glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)0);
		glEnableVertexAttribArray(0);
		glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 5 * sizeof(float),
			(void*)(3 * sizeof(float)));
		glEnableVertexAttribArray(1);

		/* -----------------------------------------------------------------
		   2)  Per-instance attributes for the terrain instanced path
			   (position + UV rectangle)  – locations 2-4
		------------------------------------------------------------------*/
		const GLsizei instStride = 6 * sizeof(float);   // vec2 pos, vec2 uvOff, vec2 uvSize

		// a_InstancePos  (vec2)
		glVertexAttribPointer(2, 2, GL_FLOAT, GL_FALSE, instStride, (void*)0);
		glEnableVertexAttribArray(2);
		glVertexAttribDivisor(2, 1);                    // one per *instance*

		// a_InstanceUVOffset (vec2)
		glVertexAttribPointer(3, 2, GL_FLOAT, GL_FALSE,
			instStride, (void*)(2 * sizeof(float)));
		glEnableVertexAttribArray(3);
		glVertexAttribDivisor(3, 1);

		// a_InstanceUVSize (vec2)
		glVertexAttribPointer(4, 2, GL_FLOAT, GL_FALSE,
			instStride, (void*)(4 * sizeof(float)));
		glEnableVertexAttribArray(4);
		glVertexAttribDivisor(4, 1);

		glBindVertexArray(0);

		/* -----------------------------------------------------------------
		   3)  Hand off VAO + EBO to RenderResourceManager so the renderer
			   can re-bind them during draws
		------------------------------------------------------------------*/
		state->ctx.renderer.Init(state->gl.VAO, state->gl.EBO);

		/* -----------------------------------------------------------------
		   4)  Shader registration + hot-reload watches
		------------------------------------------------------------------*/
		auto& rm = state->ctx.renderer;
		rm.RegisterShader("sprite", "shaders/vertex.glsl", "shaders/fragment.glsl");

		auto watchShader = [&](const std::string& name,
			const std::string& vsPath,
			const std::string& fsPath)
			{
				auto fullVs = std::filesystem::path(state->ctx.settings.assetsRoot) / vsPath;
				auto fullFs = std::filesystem::path(state->ctx.settings.assetsRoot) / fsPath;

				FileWatcher::Get().WatchFile(fullVs,
					[=]() { RenderResourceManager::Get().RegisterShader(name, vsPath, fsPath); });
				FileWatcher::Get().WatchFile(fullFs,
					[=]() { RenderResourceManager::Get().RegisterShader(name, vsPath, fsPath); });
			};
		watchShader("sprite", "shaders/vertex.glsl", "shaders/fragment.glsl");

		/* -----------------------------------------------------------------
		   5)  Sync the GL viewport to the actual SDL window size once
		------------------------------------------------------------------*/
		int w = 0, h = 0;
		SDL_GetWindowSizeInPixels(state->sdl.GetWindow(), &w, &h);
		OnWindowResized(w, h);
	}

}

