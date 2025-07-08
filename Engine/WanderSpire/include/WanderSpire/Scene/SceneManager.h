// WanderSpire/Scene/SceneManager.h
#pragma once
#include "ISceneManager.h"
#include <memory>
#include <unordered_map>
#include <vector>

namespace WanderSpire::Scene {

	class SceneManager {
	public:
		SceneManager() = default;
		~SceneManager() = default;

		// Non-copyable, movable
		SceneManager(const SceneManager&) = delete;
		SceneManager& operator=(const SceneManager&) = delete;
		SceneManager(SceneManager&&) = default;
		SceneManager& operator=(SceneManager&&) = default;

		// Register loaders and savers
		void RegisterLoader(std::unique_ptr<ISceneLoader> loader);
		void RegisterSaver(std::unique_ptr<ISceneSaver> saver);
		void RegisterPostProcessor(std::unique_ptr<IEntityPostProcessor> processor);

		// Main API
		SceneLoadResult LoadScene(const std::string& filePath, entt::registry& registry);
		SceneSaveResult SaveScene(const std::string& filePath,
			const entt::registry& registry,
			const SceneMetadata& metadata = {});

		// Query capabilities
		std::vector<std::string> GetSupportedLoadFormats() const;
		std::vector<std::string> GetSupportedSaveFormats() const;

		// Specialized operations
		SceneLoadResult LoadTilemap(const std::string& filePath,
			entt::registry& registry,
			const glm::vec2& position = { 0, 0 });
		SceneSaveResult SaveTilemap(const std::string& filePath,
			const entt::registry& registry,
			entt::entity tilemapEntity);

	private:
		std::vector<std::unique_ptr<ISceneLoader>> m_loaders;
		std::vector<std::unique_ptr<ISceneSaver>> m_savers;
		std::vector<std::unique_ptr<IEntityPostProcessor>> m_postProcessors;

		ISceneLoader* FindLoader(const std::string& filePath) const;
		ISceneSaver* FindSaver(const std::string& filePath) const;
		std::string GetFileExtension(const std::string& filePath) const;
		void RunPostProcessors(entt::registry& registry,
			const std::vector<entt::entity>& entities);
	};

} // namespace WanderSpire::Scene

