#include "WanderSpire/Editor/Commands/TilemapCommands.h"
#include "WanderSpire/World/TilemapSystem.h"
#include <chrono>

namespace WanderSpire {

	SetTileCommand::SetTileCommand(entt::registry& registry, entt::entity tilemapLayer,
		const glm::ivec2& position, int newTileId)
		: registry(&registry), tilemapLayer(tilemapLayer), tilePosition(position),
		newTileId(newTileId), commandTime(std::chrono::steady_clock::now())
	{
		oldTileId = TilemapSystem::GetInstance().GetTile(registry, tilemapLayer, position);
	}

	std::string SetTileCommand::GetDescription() const {
		return "Set tile at (" + std::to_string(tilePosition.x) + ", " +
			std::to_string(tilePosition.y) + ") to " + std::to_string(newTileId);
	}

	void SetTileCommand::Execute() {
		TilemapSystem::GetInstance().SetTile(*registry, tilemapLayer, tilePosition, newTileId);
	}

	void SetTileCommand::Undo() {
		TilemapSystem::GetInstance().SetTile(*registry, tilemapLayer, tilePosition, oldTileId);
	}

	bool SetTileCommand::CanMerge(const ICommand* other) const {
		auto* otherTile = dynamic_cast<const SetTileCommand*>(other);
		if (!otherTile || otherTile->tilemapLayer != tilemapLayer)
			return false;
		auto timeDiff = std::chrono::duration_cast<std::chrono::milliseconds>(
			otherTile->commandTime - commandTime).count();
		return timeDiff < 500;
	}

	void SetTileCommand::MergeWith(const ICommand* other) {
		auto* otherTile = static_cast<const SetTileCommand*>(other);
		newTileId = otherTile->newTileId;
		commandTime = otherTile->commandTime;
	}

} // namespace WanderSpire
