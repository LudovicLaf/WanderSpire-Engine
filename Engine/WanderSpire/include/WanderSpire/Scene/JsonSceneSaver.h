// WanderSpire/Scene/JsonSceneLoader.h
#pragma once
#include "ISceneManager.h"
#include <nlohmann/json.hpp>
#include <unordered_map>

namespace WanderSpire::Scene {
	class JsonSceneSaver : public ISceneSaver {
	public:
		SceneSaveResult SaveScene(const std::string& filePath,
			const entt::registry& registry,
			const SceneMetadata& metadata) override;
		bool SupportsFormat(const std::string& extension) const override;
		nlohmann::json SerializeEntity(entt::entity entity, const entt::registry& registry);


	private:
		struct SaveContext {
			const entt::registry* registry;
			nlohmann::json sceneJson;
			std::vector<entt::entity> entitiesToSave;
			size_t entitiesSaved = 0;
		};

		void GatherEntities(SaveContext& context);
		void SaveMetadata(const SceneMetadata& metadata, SaveContext& context);
		void SaveEntities(SaveContext& context);
		void SaveHierarchy(SaveContext& context);

		void SaveReflectedComponents(entt::entity entity, const entt::registry& registry,
			nlohmann::json& entityJson);
	};

} // namespace WanderSpire::Scene

