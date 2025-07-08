#pragma once
#include <vector>
#include <glm/glm.hpp>
#include <entt/entt.hpp>

namespace WanderSpire {

	struct PathResult {
		std::vector<glm::ivec2> fullPath;     ///< Complete step-by-step path
		std::vector<glm::ivec2> checkpoints;  ///< Key waypoints (direction changes)
	};

	class Pathfinder2D {
	public:
		/// Find a path from start to target within maxRange tiles.
		/// If tilemapLayer is entt::null, will auto-find the first available layer.
		static PathResult FindPath(
			const glm::ivec2& start,
			const glm::ivec2& target,
			int maxRange,
			entt::registry& registry,
			entt::entity tilemapLayer = entt::null
		);

		/// Check if movement is allowed between two adjacent tiles.
		/// If tilemapLayer is entt::null, will auto-find the first available layer.
		static bool CanMoveBetween(
			entt::registry& registry,
			entt::entity tilemapLayer,
			const glm::ivec2& from,
			const glm::ivec2& to
		);

		/// Check if a single tile is walkable.
		/// If tilemapLayer is entt::null, will auto-find the first available layer.
		static bool IsTileWalkable(
			entt::registry& registry,
			entt::entity tilemapLayer,
			const glm::ivec2& pos
		);

		/// Auto-discover the first available tilemap layer in the registry.
		/// Returns entt::null if no tilemap layer is found.
		static entt::entity FindFirstTilemapLayer(entt::registry& registry);
	};

	/// Legacy free function for compatibility
	bool CanMoveBetween(
		const glm::ivec2& from,
		const glm::ivec2& to,
		entt::registry& registry,
		entt::entity tilemapLayer = entt::null
	);

} // namespace WanderSpire