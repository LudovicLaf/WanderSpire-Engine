#include "WanderSpire/Editor/Commands/TilemapCommands.h"
#include "WanderSpire/World/TilemapSystem.h"
#include <queue>
#include <unordered_set>

namespace WanderSpire {

	FloodFillCommand::FloodFillCommand(entt::registry& registry, entt::entity tilemapLayer,
		const glm::ivec2& startPos, int newTileId)
		: registry(&registry), tilemapLayer(tilemapLayer), startPosition(startPos), newTileId(newTileId) {
		CalculateAffectedTiles();
	}

	void FloodFillCommand::Execute() {
		for (const auto& change : affectedTiles) {
			TilemapSystem::GetInstance().SetTile(*registry, tilemapLayer, change.position, change.newTileId);
		}
	}

	void FloodFillCommand::Undo() {
		for (const auto& change : affectedTiles) {
			TilemapSystem::GetInstance().SetTile(*registry, tilemapLayer, change.position, change.oldTileId);
		}
	}

	std::string FloodFillCommand::GetDescription() const {
		return "Flood fill " + std::to_string(affectedTiles.size()) + " tiles";
	}

	void FloodFillCommand::CalculateAffectedTiles() {
		auto& tilemapSystem = TilemapSystem::GetInstance();
		int originalTileId = tilemapSystem.GetTile(*registry, tilemapLayer, startPosition);

		if (originalTileId == newTileId) return;

		std::queue<glm::ivec2> toProcess;
		std::unordered_set<uint64_t> visited;
		toProcess.push(startPosition);

		while (!toProcess.empty()) {
			glm::ivec2 pos = toProcess.front();
			toProcess.pop();

			uint64_t key = (uint64_t(pos.x) << 32) | uint32_t(pos.y);
			if (visited.find(key) != visited.end()) continue;
			visited.insert(key);

			int currentTileId = tilemapSystem.GetTile(*registry, tilemapLayer, pos);
			if (currentTileId == originalTileId) {
				PaintTilesCommand::TileChange change;
				change.position = pos;
				change.oldTileId = currentTileId;
				change.newTileId = newTileId;
				affectedTiles.push_back(change);

				toProcess.push(pos + glm::ivec2{ 1, 0 });
				toProcess.push(pos + glm::ivec2{ -1, 0 });
				toProcess.push(pos + glm::ivec2{ 0, 1 });
				toProcess.push(pos + glm::ivec2{ 0, -1 });
			}
		}
	}

} // namespace WanderSpire
