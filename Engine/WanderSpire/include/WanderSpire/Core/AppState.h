//=============================================================================
// Updated AppState.h - Clean Scene Editor Integration
// Modernized for ECS tilemap system, removed legacy GridMap2D and ChunkManager
//=============================================================================

#pragma once

#include <memory>
#include <entt/entt.hpp>
#include <glm/glm.hpp>

#include "WanderSpire/Core/SDLContext.h"
#include "WanderSpire/Core/GLObjects.h"
#include "WanderSpire/Core/EngineContext.h"
#include "WanderSpire/Graphics/Texture.h"
#include "WanderSpire/ECS/World.h"

// Scene editor systems
#include "WanderSpire/Editor/CommandHistory.h"

namespace WanderSpire {

	struct AppState {
		AppState() = default;

		// Core engine context
		EngineContext ctx;

		// SDL and OpenGL setup
		SDLContext sdl{ 800, 600, "WanderSpire Engine", true };
		GLObjects gl;

		// Asset references (for backwards compatibility during transition)
		std::shared_ptr<Texture> bobTexture;
		std::shared_ptr<Texture> tileTexture;
		std::shared_ptr<Texture> goblinTexture;

		// ═════════════════════════════════════════════════════════════════════
		// SCENE EDITOR INTEGRATION
		// ═════════════════════════════════════════════════════════════════════

		// Command system for undo/redo
		std::unique_ptr<CommandHistory> commandHistory;

		// World and entity management
		World world;

		// Current scene state
		entt::entity selectedEntity = entt::null;
		entt::entity cameraTarget = entt::null;    // Camera follows this entity
		entt::entity player = entt::null;          // Player reference (set by managed layer)

		// Main tilemap (replaces hardcoded gridMap)
		entt::entity mainTilemap = entt::null;     // Primary tilemap for world

		// Editor state
		bool debugEntityTiles = false;

		// ═════════════════════════════════════════════════════════════════════
		// SCENE MANAGEMENT HELPERS
		// ═════════════════════════════════════════════════════════════════════

		/// Check if a main tilemap exists
		bool HasMainTilemap() const {
			return mainTilemap != entt::null && world.GetRegistry().valid(mainTilemap);
		}

		/// Get the main tilemap or create one if missing
		entt::entity GetOrCreateMainTilemap() {
			if (!HasMainTilemap()) {
				// Let the managed layer create tilemaps via the API
				// This is just a placeholder for the entity reference
				return entt::null;
			}
			return mainTilemap;
		}

		/// Set the main tilemap (called from managed layer)
		void SetMainTilemap(entt::entity tilemap) {
			mainTilemap = tilemap;
		}

		/// Set the player entity (called from managed layer)
		void SetPlayer(entt::entity playerEntity) {
			player = playerEntity;
		}

		/// Set camera to follow an entity
		void SetCameraTarget(entt::entity target) {
			cameraTarget = target;
		}

		/// Clear camera target (free camera mode)
		void ClearCameraTarget() {
			cameraTarget = entt::null;
		}
	};

} // namespace WanderSpire