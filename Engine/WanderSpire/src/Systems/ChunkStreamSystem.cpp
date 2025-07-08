#include "WanderSpire/Systems/ChunkStreamSystem.h"

#include "WanderSpire/Core/EventBus.h"
#include "WanderSpire/Core/Events.h"
#include "WanderSpire/World/TilemapSystem.h"
#include "WanderSpire/Components/TilemapLayerComponent.h"
#include <spdlog/spdlog.h>

namespace WanderSpire {

	// Static reference to registry for event callbacks
	static entt::registry* s_Registry = nullptr;

	void ChunkStreamSystem::Initialize(EngineContext& ctx, entt::registry& registry)
	{
		// Store registry reference for event callbacks
		s_Registry = &registry;

		static EventBus::Subscription s_token =
			EventBus::Get().Subscribe<CameraMovedEvent>(
				[](const CameraMovedEvent& ev)
				{
					if (s_Registry) {
						OnCameraMove(*s_Registry, ev.minBound, ev.maxBound);
					}
				});
	}

	void ChunkStreamSystem::OnCameraMove(entt::registry& registry, const glm::vec2& minBound, const glm::vec2& maxBound)
	{
		// Calculate view center and radius for streaming
		glm::vec2 viewCenter = (minBound + maxBound) * 0.5f;
		float viewRadius = glm::length(maxBound - minBound) * 0.5f;

		// Add some padding for smooth streaming
		float streamingRadius = viewRadius * 1.5f;

		UpdateTilemapStreaming(registry, viewCenter, streamingRadius);
	}

	void ChunkStreamSystem::UpdateTilemapStreaming(entt::registry& registry, const glm::vec2& viewCenter, float viewRadius)
	{
		auto& tilemapSystem = TilemapSystem::GetInstance();

		// spdlog::debug("[ChunkStreamSystem] Updating tilemap streaming - view center: ({:.1f}, {:.1f}), radius: {:.1f}",
		// 	viewCenter.x, viewCenter.y, viewRadius);

		// Use the modern ECS tilemap system for chunk streaming
		tilemapSystem.UpdateTilemapStreaming(registry, viewCenter, viewRadius);
	}

} // namespace WanderSpire