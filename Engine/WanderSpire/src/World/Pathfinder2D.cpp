#include "WanderSpire/World/Pathfinder2D.h"
#include "WanderSpire/World/TilemapSystem.h"
#include "WanderSpire/Components/ObstacleComponent.h"
#include "WanderSpire/Components/GridPositionComponent.h"
#include "WanderSpire/Components/TileComponent.h"
#include "WanderSpire/Components/TilemapLayerComponent.h"
#include "WanderSpire/Components/SceneNodeComponent.h"

#include <queue>
#include <unordered_map>
#include <unordered_set>
#include <limits>
#define GLM_ENABLE_EXPERIMENTAL
#include <glm/gtx/norm.hpp>

namespace WanderSpire {

	// ─────────────────────────────────────────────────────────────────────────────
	// Helpers
	// ─────────────────────────────────────────────────────────────────────────────

	/// Packs a grid‐coordinate into a 64-bit key for hash tables.
	static inline uint64_t HashKey(const glm::ivec2& p) {
		return (uint64_t(p.x) << 32) | uint32_t(p.y);
	}

	// 8‐way neighbor offsets.
	static constexpr glm::ivec2 DIRS[8] = {
		{ 1,  0}, {-1,  0},
		{ 0,  1}, { 0, -1},
		{ 1,  1}, { 1, -1},
		{-1,  1}, {-1, -1}
	};

	// ─────────────────────────────────────────────────────────────────────────────
	// Dynamic tilemap layer finding
	// ─────────────────────────────────────────────────────────────────────────────

	entt::entity Pathfinder2D::FindFirstTilemapLayer(entt::registry& registry) {
		// Look for any entity with TilemapLayerComponent
		auto layerView = registry.view<TilemapLayerComponent>();
		if (!layerView.empty()) {
			// Return the first layer we find
			return *layerView.begin();
		}

		// Fallback: look for nodes named "Layer" that are children of "Tilemap"
		auto nodeView = registry.view<SceneNodeComponent>();
		for (auto entity : nodeView) {
			const auto& node = nodeView.get<SceneNodeComponent>(entity);

			if (node.name.find("Layer") != std::string::npos &&
				node.parent != entt::null) {

				// Check if parent is a tilemap
				if (auto* parentNode = registry.try_get<SceneNodeComponent>(node.parent)) {
					if (parentNode->name.find("Tilemap") != std::string::npos) {
						return entity;
					}
				}
			}
		}

		return entt::null;
	}

	// ─────────────────────────────────────────────────────────────────────────────
	// Tile walkability checking with ECS tilemap system
	// ─────────────────────────────────────────────────────────────────────────────

	bool Pathfinder2D::IsTileWalkable(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& pos) {
		// If we don't have a valid tilemap layer, try to find one
		if (!registry.valid(tilemapLayer) || tilemapLayer == entt::null) {
			tilemapLayer = FindFirstTilemapLayer(registry);
			if (tilemapLayer == entt::null) {
				// No tilemap found, assume walkable
				return true;
			}
		}

		auto& tilemapSystem = TilemapSystem::GetInstance();

		// Get base tile ID from the tilemap chunk system
		int tileId = tilemapSystem.GetTile(registry, tilemapLayer, pos);

		// Empty tiles (ID -1) are walkable
		if (tileId == -1) return true;

		// Check for TileComponent override at this position
		auto tileView = registry.view<TileComponent>();
		for (auto entity : tileView) {
			const auto& tile = tileView.get<TileComponent>(entity);
			if (tile.gridPosition == pos) {
				return tile.walkable;
			}
		}

		// Check for dynamic obstacles (entities with ObstacleComponent)
		auto obstacleView = registry.view<ObstacleComponent, GridPositionComponent>();
		for (auto entity : obstacleView) {
			const auto& obstacle = obstacleView.get<ObstacleComponent>(entity);
			const auto& gridPos = obstacleView.get<GridPositionComponent>(entity);
			if (obstacle.blocksMovement && gridPos.tile == pos) {
				return false;
			}
		}

		// Default: tiles with valid IDs are walkable unless explicitly marked otherwise
		return true;
	}

	bool Pathfinder2D::CanMoveBetween(entt::registry& registry, entt::entity tilemapLayer,
		const glm::ivec2& from, const glm::ivec2& to) {
		if (from == to) return true;

		// Auto-find tilemap layer if needed
		if (!registry.valid(tilemapLayer) || tilemapLayer == entt::null) {
			tilemapLayer = FindFirstTilemapLayer(registry);
			if (tilemapLayer == entt::null) {
				// No tilemap found, allow movement
				return true;
			}
		}

		glm::ivec2 delta = to - from;
		int dx = delta.x, dy = delta.y;

		// Only allow movement to direct neighbors (8-way)
		if (std::abs(dx) > 1 || std::abs(dy) > 1) return false;

		// Both tiles must be walkable
		if (!IsTileWalkable(registry, tilemapLayer, from) ||
			!IsTileWalkable(registry, tilemapLayer, to)) {
			return false;
		}

		// For diagonal movement, prevent corner cutting by checking orthogonal paths
		if (dx != 0 && dy != 0) {
			glm::ivec2 intermediate1 = from + glm::ivec2{ dx, 0 };
			glm::ivec2 intermediate2 = from + glm::ivec2{ 0, dy };

			if (!IsTileWalkable(registry, tilemapLayer, intermediate1) ||
				!IsTileWalkable(registry, tilemapLayer, intermediate2)) {
				return false;
			}
		}

		return true;
	}

	// ─────────────────────────────────────────────────────────────────────────────
	// Main pathfinding algorithm
	// ─────────────────────────────────────────────────────────────────────────────

	PathResult Pathfinder2D::FindPath(
		const glm::ivec2& start,
		const glm::ivec2& target,
		int               maxRange,
		entt::registry& registry,
		entt::entity      tilemapLayer
	) {
		PathResult out;

		// Auto-find tilemap layer if not provided or invalid
		if (!registry.valid(tilemapLayer) || tilemapLayer == entt::null) {
			tilemapLayer = FindFirstTilemapLayer(registry);
			if (tilemapLayer == entt::null) {
				// No tilemap found - create a simple direct path
				out.fullPath.push_back(start);
				if (start != target) {
					out.fullPath.push_back(target);
				}
				out.checkpoints = out.fullPath;
				return out;
			}
		}

		// Early exit if start or target is not walkable
		if (!IsTileWalkable(registry, tilemapLayer, start) ||
			!IsTileWalkable(registry, tilemapLayer, target)) {
			return out;
		}

		// Range limit
		int r = std::max(1, maxRange);
		int r2 = r * r;

		auto withinRange = [&](const glm::ivec2& p) {
			return glm::distance2(glm::vec2(p), glm::vec2(start)) <= float(r2);
			};

		// ─── BFS phase ────────────────────────────────────────────────────────────
		std::queue<glm::ivec2> q;
		std::unordered_map<uint64_t, glm::ivec2> parent;
		std::unordered_set<uint64_t> visited;

		uint64_t startKey = HashKey(start);
		q.push(start);
		visited.insert(startKey);
		bool found = false;
		glm::ivec2 reached{ -1, -1 };

		while (!q.empty()) {
			glm::ivec2 cur = q.front();
			q.pop();

			if (cur == target) {
				reached = cur;
				found = true;
				break;
			}

			for (auto d : DIRS) {
				glm::ivec2 nxt = cur + d;
				uint64_t key = HashKey(nxt);

				if (!withinRange(nxt)) continue;
				if (visited.count(key)) continue;
				if (!CanMoveBetween(registry, tilemapLayer, cur, nxt)) continue;

				visited.insert(key);
				parent[key] = cur;
				q.push(nxt);
			}
		}

		if (found) {
			// Reconstruct BFS path
			for (glm::ivec2 p = reached; ; p = parent[HashKey(p)]) {
				out.fullPath.push_back(p);
				if (p == start) break;
			}
			std::reverse(out.fullPath.begin(), out.fullPath.end());
		}

		// ─── Greedy fallback if BFS failed ─────────────────────────────────────────
		if (out.fullPath.empty()) {
			glm::ivec2 cur = start;
			out.fullPath.push_back(cur);

			// Keep stepping greedily toward target until stuck or reached
			while (cur != target && int(out.fullPath.size()) <= maxRange) {
				float bestDist = std::numeric_limits<float>::infinity();
				glm::ivec2 bestNbr = cur;

				for (auto d : DIRS) {
					glm::ivec2 cand = cur + d;
					if (!withinRange(cand)) continue;
					if (!CanMoveBetween(registry, tilemapLayer, cur, cand)) continue;

					float dist2 = glm::distance2(glm::vec2(cand), glm::vec2(target));
					if (dist2 < bestDist) {
						bestDist = dist2;
						bestNbr = cand;
					}
				}

				if (bestNbr == cur) {
					// No neighbor improved—stop
					break;
				}

				cur = bestNbr;
				out.fullPath.push_back(cur);
				if (cur == target) break;
			}
		}

		// ─── Extract turn‐point "checkpoints" ────────────────────────────────────
		if (!out.fullPath.empty()) {
			glm::ivec2 prevDir{ 0, 0 };
			for (size_t i = 1; i < out.fullPath.size(); ++i) {
				glm::ivec2 dir = out.fullPath[i] - out.fullPath[i - 1];
				// Whenever direction changes, record the previous tile
				if (dir.x != prevDir.x || dir.y != prevDir.y) {
					out.checkpoints.push_back(out.fullPath[i - 1]);
					prevDir = dir;
				}
			}
			// Always append the final destination
			out.checkpoints.push_back(out.fullPath.back());
		}

		return out;
	}

	// ─────────────────────────────────────────────────────────────────────────────
	// Free function for compatibility (formerly PathUtils)
	// ─────────────────────────────────────────────────────────────────────────────

	bool CanMoveBetween(const glm::ivec2& from,
		const glm::ivec2& to,
		entt::registry& registry,
		entt::entity tilemapLayer)
	{
		return Pathfinder2D::CanMoveBetween(registry, tilemapLayer, from, to);
	}

} // namespace WanderSpire