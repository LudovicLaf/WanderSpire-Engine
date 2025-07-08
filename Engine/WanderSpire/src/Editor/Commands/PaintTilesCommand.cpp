#include "WanderSpire/Editor/Commands/TilemapCommands.h"
#include "WanderSpire/World/TilemapSystem.h"

namespace WanderSpire {

	PaintTilesCommand::PaintTilesCommand(entt::registry& registry, entt::entity tilemapLayer,
		const std::vector<TileChange>& changes)
		: registry(&registry), tilemapLayer(tilemapLayer), tileChanges(changes) {
	}

	void PaintTilesCommand::Execute() {
		for (const auto& change : tileChanges)
			TilemapSystem::GetInstance().SetTile(*registry, tilemapLayer, change.position, change.newTileId);
	}

	void PaintTilesCommand::Undo() {
		for (const auto& change : tileChanges)
			TilemapSystem::GetInstance().SetTile(*registry, tilemapLayer, change.position, change.oldTileId);
	}

	std::string PaintTilesCommand::GetDescription() const {
		return "Paint " + std::to_string(tileChanges.size()) + " tiles";
	}

} // namespace WanderSpire
