#include "WanderSpire/Editor/Commands/HierarchyCommands.h"
#include "WanderSpire/Editor/SceneHierarchyManager.h"
#include "WanderSpire/Components/TransformComponent.h"

namespace WanderSpire {

	CreateGameObjectCommand::CreateGameObjectCommand(entt::registry& registry, const std::string& name,
		entt::entity parent, const glm::vec2& position)
		: registry(&registry), objectName(name), parentEntity(parent), initialPosition(position) {
	}

	std::string CreateGameObjectCommand::GetDescription() const {
		return "Create " + objectName;
	}

	void CreateGameObjectCommand::Execute() {
		if (createdEntity == entt::null) {
			createdEntity = SceneHierarchyManager::GetInstance().CreateGameObject(*registry, objectName);
			if (parentEntity != entt::null)
				SceneHierarchyManager::GetInstance().SetParent(*registry, createdEntity, parentEntity);
			auto* transform = registry->try_get<TransformComponent>(createdEntity);
			if (transform)
				transform->localPosition = initialPosition;
		}
	}

	void CreateGameObjectCommand::Undo() {
		if (createdEntity != entt::null && registry->valid(createdEntity)) {
			SceneHierarchyManager::GetInstance().DestroyGameObject(*registry, createdEntity);
			createdEntity = entt::null;
		}
	}

} // namespace WanderSpire
