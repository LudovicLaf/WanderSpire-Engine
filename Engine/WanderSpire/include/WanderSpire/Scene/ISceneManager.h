#pragma once
#include <entt/entt.hpp>
#include <string>
#include <glm/glm.hpp>

namespace WanderSpire::Scene {

	struct SceneLoadResult {
		bool success = false;
		std::string error;
		entt::entity playerEntity = entt::null;
		entt::entity mainTilemap = entt::null;
		glm::vec2 playerPosition{ 0.0f, 0.0f };
		std::vector<entt::entity> loadedEntities;
	};

	struct SceneSaveResult {
		bool success = false;
		std::string error;
		size_t entitiesSaved = 0;
	};

	struct SceneMetadata {
		std::string name;
		std::string version = "2.0";
		std::string author;
		std::string description;
		std::vector<std::string> tags;
		uint64_t lastModified = 0;
		glm::vec2 worldMin{ -1000.0f, -1000.0f };
		glm::vec2 worldMax{ 1000.0f, 1000.0f };
	};

	class ISceneLoader {
	public:
		virtual ~ISceneLoader() = default;
		virtual SceneLoadResult LoadScene(const std::string& filePath, entt::registry& registry) = 0;
		virtual bool SupportsFormat(const std::string& extension) const = 0;
	};

	class ISceneSaver {
	public:
		virtual ~ISceneSaver() = default;
		virtual SceneSaveResult SaveScene(const std::string& filePath,
			const entt::registry& registry,
			const SceneMetadata& metadata = {}) = 0;
		virtual bool SupportsFormat(const std::string& extension) const = 0;
	};

	class IEntityPostProcessor {
	public:
		virtual ~IEntityPostProcessor() = default;
		virtual void ProcessLoadedEntities(entt::registry& registry,
			const std::vector<entt::entity>& entities) = 0;
		virtual int GetPriority() const = 0; // Lower numbers run first
	};

} // namespace WanderSpire::Scene