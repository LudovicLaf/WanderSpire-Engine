#pragma once
#include <entt/entt.hpp>
#include "WanderSpire/Graphics/RenderCommand.h"

namespace WanderSpire {
	struct AppState;

	/// Submits entity rendering commands to the RenderManager instead of immediate rendering
	class RenderSystem {
	public:
		/// Installs the event‑bus subscription (call once during bootstrap).
		static void Initialize();

		/// Main render entry – submits commands to RenderManager
		static void SubmitEntityCommands(const entt::registry& registry,
			const AppState* state);

		/// Submit terrain/chunk rendering commands using new ECS tilemap system
		static void SubmitTerrainCommands(const AppState* state,
			const glm::vec2& minBound,
			const glm::vec2& maxBound);

		/// Submit debug overlay commands
		static void SubmitDebugCommands(const entt::registry& registry,
			const AppState* state,
			const glm::vec2& minBound,
			const glm::vec2& maxBound);

		/// Render a single tilemap layer using the new ECS system
		static void RenderTilemapLayer(const entt::registry& registry,
			entt::entity tilemapLayer,
			const glm::vec2& minBound,
			const glm::vec2& maxBound,
			float tileSize);

	private:
		/// Legacy immediate render method - kept for backwards compatibility
		static void Render(const entt::registry& registry, const AppState* state);
	};
} // namespace WanderSpire