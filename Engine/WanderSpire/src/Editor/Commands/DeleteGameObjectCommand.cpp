#include "WanderSpire/Editor/Commands/HierarchyCommands.h"
#include "WanderSpire/Editor/SceneHierarchyManager.h"
#include "WanderSpire/Components/SceneNodeComponent.h"
#include "WanderSpire/Scene/JsonSceneSaver.h"
#include "WanderSpire/Scene/JsonSceneLoader.h"

namespace WanderSpire {

	// Helper functions for static-like use
	static nlohmann::json SerializeEntity(entt::registry& registry, entt::entity entity) {
		return Scene::JsonSceneSaver().SerializeEntity(entity, registry);
	}
	static entt::entity DeserializeEntity(entt::registry& registry, const nlohmann::json& data) {
		// Loader does not expose DeserializeEntity directly; simulate by:
		// 1. create a new entity
		// 2. add components from JSON
		entt::entity entity = registry.create();
		if (data.contains("components")) {
			Scene::JsonSceneLoader loader;
			loader.LoadEntityComponents(entity, data["components"], registry);
		}
		// restore entity id mapping and parent separately if needed
		return entity;
	}

	DeleteGameObjectCommand::DeleteGameObjectCommand(entt::registry& registry, const std::vector<entt::entity>& entities)
		: registry(&registry), deletedEntities(entities) {
		serializedEntities.reserve(entities.size());
		parentEntities.reserve(entities.size());
		for (entt::entity entity : entities) {
			serializedEntities.push_back(SerializeEntity(registry, entity));
			parentEntities.push_back(SceneHierarchyManager::GetInstance().GetParent(registry, entity));
		}
	}

	void DeleteGameObjectCommand::Execute() {
		for (entt::entity entity : deletedEntities) {
			if (registry->valid(entity))
				SceneHierarchyManager::GetInstance().DestroyGameObject(*registry, entity);
		}
	}

	void DeleteGameObjectCommand::Undo() {
		deletedEntities.clear();
		for (size_t i = 0; i < serializedEntities.size(); ++i) {
			entt::entity entity = DeserializeEntity(*registry, serializedEntities[i]);
			deletedEntities.push_back(entity);
			if (parentEntities[i] != entt::null && registry->valid(parentEntities[i]))
				SceneHierarchyManager::GetInstance().SetParent(*registry, entity, parentEntities[i]);
		}
	}

	std::string DeleteGameObjectCommand::GetDescription() const {
		if (deletedEntities.size() == 1) return "Delete GameObject";
		return "Delete " + std::to_string(deletedEntities.size()) + " GameObjects";
	}
}
