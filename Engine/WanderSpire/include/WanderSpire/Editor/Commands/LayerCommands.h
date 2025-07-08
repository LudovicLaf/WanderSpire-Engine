#pragma once
#include "WanderSpire/Editor/ICommand.h"
#include "Wanderspire/Editor/LayerManager.h"
#include <entt/entt.hpp>
#include <string>
#include <vector>

namespace WanderSpire {

	class CreateLayerCommand : public ICommand {
	public:
		CreateLayerCommand(const std::string& layerName);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		std::string layerName;
		int createdLayerId = -1;
	};

	class DeleteLayerCommand : public ICommand {
	public:
		DeleteLayerCommand(int layerId);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		int layerId;
		LayerManager::Layer savedLayer;
		std::vector<std::pair<entt::entity, int>> entitiesMovedToDefault;
	};

	class ChangeEntityLayerCommand : public ICommand {
	public:
		ChangeEntityLayerCommand(entt::registry& registry, const std::vector<entt::entity>& entities, int newLayerId);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		std::vector<entt::entity> entities;
		std::vector<int> oldLayerIds;
		int newLayerId;
	};

} // namespace WanderSpire
