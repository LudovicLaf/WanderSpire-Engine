#pragma once
#include "WanderSpire/Editor/ICommand.h"
#include <entt/entt.hpp>
#include <glm/glm.hpp>
#include <string>
#include <vector>
#include <nlohmann/json.hpp>

namespace WanderSpire {

	class CreatePrefabCommand : public ICommand {
	public:
		CreatePrefabCommand(entt::registry& registry, const std::vector<entt::entity>& entities,
			const std::string& prefabPath);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		std::vector<entt::entity> sourceEntities;
		std::string prefabPath;
		nlohmann::json serializedEntities;
	};

	class InstantiatePrefabCommand : public ICommand {
	public:
		InstantiatePrefabCommand(entt::registry& registry, const std::string& prefabPath,
			const glm::vec2& position, entt::entity parent = entt::null);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		std::string prefabPath;
		glm::vec2 instantiatePosition;
		entt::entity parentEntity;
		std::vector<entt::entity> instantiatedEntities;
	};

	class BreakPrefabInstanceCommand : public ICommand {
	public:
		BreakPrefabInstanceCommand(entt::registry& registry, entt::entity prefabInstance);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		entt::entity prefabInstance;
		nlohmann::json savedPrefabData;
	};

} // namespace WanderSpire
