#pragma once
#include "WanderSpire/Core/Exports.h"
#include <SDL3/SDL.h>
#include <glm/glm.hpp>
#include "WanderSpire/Core/AppState.h"
#include "WanderSpire/Graphics/Camera2D.h"

namespace WanderSpire {
	class Application {
	public:
		// ==== SDL application lifecycle ====
		static SDL_AppResult AppInit(void** appstate, int argc, char* argv[]);
		static SDL_AppResult AppEvent(void* appstate, SDL_Event* event);
		static SDL_AppResult AppIterate(void* appstate);
		static void          AppQuit(void* appstate, SDL_AppResult result);

		// ==== Window/Rendering ====
		static void          OnWindowResized(int width, int height);

		// ==== Camera Access ====
		static Camera2D& GetCamera() { return camera; }

		// ==== Performance Access for C API ====
		static float GetLastFrameTime();
		static float GetLastRenderTime();
		static float GetLastUpdateTime();
		static int   GetLastFrameDrawCalls();

		// ==== OpenGL Context Management ====
		static SDL_GLContext GetCurrentGLContext();
		static bool IsHeadlessMode();
		static void SetImmediateModeRendering(bool enabled);

		// ==== Editor Mode Management ====
		static void SetEditorMode(bool enabled);
		static bool IsEditorMode();

	private:
		// ==== AppState ====
		static AppState* GetState(void* appstate);

		// ==== System Initialization ====
		static void          InitializeRendering(AppState* state);
		static void          InitializeAssets(AppState* state);
		static void          InitializeTerrain(AppState* state);    // Updated to use ECS tilemap system
		static void          InitializeInput();
		static void          InitializeTextures(AppState* state);

		// ==== Scene Editor Bootstrap ====
		static void          InitializeSceneEditor(AppState* state);
		static void          UpdateSceneEditor(AppState* state, float deltaTime);
		static void          CleanupSceneEditor(AppState* state);

		// ==== Input ====
		static void          HandleKeyboardInput(AppState* state);

		// ==== Camera ====
		inline static Camera2D camera{ 800.0f, 600.0f };

		// ==== OpenGL State Tracking ====
		static bool s_immediateModeEnabled;
		static bool s_headlessMode;
	};
} // namespace WanderSpire