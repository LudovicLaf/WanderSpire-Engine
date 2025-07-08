// WanderSpire/Scene/JsonSceneLoader.h
#pragma once
#include "ISceneManager.h"
#include <nlohmann/json.hpp>
#include <unordered_map>

namespace WanderSpire::Scene {

	class JsonSceneLoader : public ISceneLoader {
	public:
		SceneLoadResult LoadScene(const std::string& filePath, entt::registry& registry) override;
		bool SupportsFormat(const std::string& extension) const override;
		void LoadEntityComponents(entt::entity entity, const nlohmann::json& components,
			entt::registry& registry);
	private:
		struct LoadContext {
			entt::registry* registry;
			std::unordered_map<uint32_t, entt::entity> idMapping;
			std::vector<entt::entity> loadedEntities;
			SceneLoadResult result;
		};

		bool LoadSceneFile(const std::string& filePath, nlohmann::json& outJson);
		void LoadMetadata(const nlohmann::json& json, LoadContext& context);
		void CreateEntities(const nlohmann::json& json, LoadContext& context);
		void LoadComponents(const nlohmann::json& json, LoadContext& context);
		void RestoreHierarchy(const nlohmann::json& json, LoadContext& context);
		void FindSpecialEntities(LoadContext& context);


		bool IsNativeComponent(const std::string& componentName);
		void LoadReflectedComponent(const std::string& name, const nlohmann::json& data,
			entt::entity entity, entt::registry& registry);
	};
} // namespace WanderSpire::Scene

