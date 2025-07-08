#pragma once
#include <entt/entt.hpp>
#include "WanderSpire/Core/EngineContext.h"

namespace WanderSpire {

	/** Maintains loaded terrain chunks in response to camera movement using ECS TilemapSystem. */
	struct ChunkStreamSystem {
		static void Initialize(EngineContext& ctx, entt::registry& registry);

	private:
		static void OnCameraMove(entt::registry& registry, const glm::vec2& minBound, const glm::vec2& maxBound);
		static void UpdateTilemapStreaming(entt::registry& registry, const glm::vec2& viewCenter, float viewRadius);
	};

} // namespace WanderSpire