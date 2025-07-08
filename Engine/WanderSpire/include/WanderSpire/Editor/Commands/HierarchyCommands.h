#pragma once
#include "WanderSpire/Editor/ICommand.h"
#include <entt/entt.hpp>
#include <string>
#include <glm/glm.hpp>
#include <vector>
#include <nlohmann/json.hpp>

namespace WanderSpire {

	class CreateGameObjectCommand : public ICommand {
	public:
		CreateGameObjectCommand(entt::registry& registry, const std::string& name,
			entt::entity parent = entt::null, const glm::vec2& position = { 0,0 });
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		std::string objectName;
		entt::entity parentEntity;
		glm::vec2 initialPosition;
		entt::entity createdEntity = entt::null;
	};

	class DeleteGameObjectCommand : public ICommand {
	public:
		DeleteGameObjectCommand(entt::registry& registry, const std::vector<entt::entity>& entities);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		std::vector<nlohmann::json> serializedEntities;
		std::vector<entt::entity> deletedEntities;
		std::vector<entt::entity> parentEntities;
	};

	class ReparentCommand : public ICommand {
	public:
		ReparentCommand(entt::registry& registry, entt::entity child, entt::entity newParent);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		entt::entity childEntity;
		entt::entity oldParent, newParent;
		std::string childName;
	};

} // namespace WanderSpire
