#pragma once
#include "WanderSpire/Editor/ICommand.h"
#include <entt/entt.hpp>
#include <glm/glm.hpp>
#include <vector>
#include <chrono>

namespace WanderSpire {

	class SetTileCommand : public ICommand {
	public:
		SetTileCommand(entt::registry& registry, entt::entity tilemapLayer,
			const glm::ivec2& position, int newTileId);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
		bool CanMerge(const ICommand* other) const override;
		void MergeWith(const ICommand* other) override;
	private:
		entt::registry* registry;
		entt::entity tilemapLayer;
		glm::ivec2 tilePosition;
		int oldTileId, newTileId;
		std::chrono::steady_clock::time_point commandTime;
	};

	class PaintTilesCommand : public ICommand {
	public:
		struct TileChange {
			glm::ivec2 position;
			int oldTileId, newTileId;
		};
		PaintTilesCommand(entt::registry& registry, entt::entity tilemapLayer,
			const std::vector<TileChange>& changes);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		entt::entity tilemapLayer;
		std::vector<TileChange> tileChanges;
	};

	class FloodFillCommand : public ICommand {
	public:
		FloodFillCommand(entt::registry& registry, entt::entity tilemapLayer,
			const glm::ivec2& startPos, int newTileId);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		entt::entity tilemapLayer;
		glm::ivec2 startPosition;
		int newTileId;
		std::vector<PaintTilesCommand::TileChange> affectedTiles;
		void CalculateAffectedTiles();
	};

} // namespace WanderSpire
