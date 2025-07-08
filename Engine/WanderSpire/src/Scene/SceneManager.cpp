#include "WanderSpire/Scene/SceneManager.h"
#include "WanderSpire/Components/TransformComponent.h"
#include <filesystem>
#include <algorithm>

namespace WanderSpire::Scene {

	void SceneManager::RegisterLoader(std::unique_ptr<ISceneLoader> loader) {
		if (loader) {
			m_loaders.push_back(std::move(loader));
		}
	}

	void SceneManager::RegisterSaver(std::unique_ptr<ISceneSaver> saver) {
		if (saver) {
			m_savers.push_back(std::move(saver));
		}
	}

	void SceneManager::RegisterPostProcessor(std::unique_ptr<IEntityPostProcessor> processor) {
		if (processor) {
			m_postProcessors.push_back(std::move(processor));

			// Sort by priority
			std::sort(m_postProcessors.begin(), m_postProcessors.end(),
				[](const auto& a, const auto& b) {
					return a->GetPriority() < b->GetPriority();
				});
		}
	}

	SceneLoadResult SceneManager::LoadScene(const std::string& filePath, entt::registry& registry) {
		auto* loader = FindLoader(filePath);
		if (!loader) {
			return { false, "No loader found for format: " + GetFileExtension(filePath) };
		}

		auto result = loader->LoadScene(filePath, registry);

		if (result.success && !result.loadedEntities.empty()) {
			RunPostProcessors(registry, result.loadedEntities);
		}

		return result;
	}

	SceneSaveResult SceneManager::SaveScene(const std::string& filePath,
		const entt::registry& registry,
		const SceneMetadata& metadata) {

		auto* saver = FindSaver(filePath);
		if (!saver) {
			return { false, "No saver found for format: " + GetFileExtension(filePath) };
		}

		return saver->SaveScene(filePath, registry, metadata);
	}

	SceneLoadResult SceneManager::LoadTilemap(const std::string& filePath,
		entt::registry& registry,
		const glm::vec2& position) {

		auto result = LoadScene(filePath, registry);

		if (result.success && result.mainTilemap != entt::null) {
			if (auto* transform = registry.try_get<TransformComponent>(result.mainTilemap)) {
				transform->localPosition += position;
			}
		}

		return result;
	}

	SceneSaveResult SceneManager::SaveTilemap(const std::string& filePath,
		const entt::registry& registry,
		entt::entity tilemapEntity) {

		if (!registry.valid(tilemapEntity)) {
			return { false, "Invalid tilemap entity" };
		}

		// Create temporary registry with tilemap entities
		entt::registry tempRegistry;
		// TODO: Implement tilemap entity collection

		SceneMetadata metadata{ .name = "Tilemap", .version = "2.0" };
		return SaveScene(filePath, tempRegistry, metadata);
	}

	std::vector<std::string> SceneManager::GetSupportedLoadFormats() const {
		std::vector<std::string> formats;
		// TODO: Implement based on loader capabilities
		return formats;
	}

	std::vector<std::string> SceneManager::GetSupportedSaveFormats() const {
		std::vector<std::string> formats;
		// TODO: Implement based on saver capabilities
		return formats;
	}

	ISceneLoader* SceneManager::FindLoader(const std::string& filePath) const {
		std::string ext = GetFileExtension(filePath);
		for (const auto& loader : m_loaders) {
			if (loader->SupportsFormat(ext)) {
				return loader.get();
			}
		}
		return nullptr;
	}

	ISceneSaver* SceneManager::FindSaver(const std::string& filePath) const {
		std::string ext = GetFileExtension(filePath);
		for (const auto& saver : m_savers) {
			if (saver->SupportsFormat(ext)) {
				return saver.get();
			}
		}
		return nullptr;
	}

	std::string SceneManager::GetFileExtension(const std::string& filePath) const {
		std::filesystem::path path(filePath);
		std::string ext = path.extension().string();
		std::transform(ext.begin(), ext.end(), ext.begin(), ::tolower);
		return ext;
	}

	void SceneManager::RunPostProcessors(entt::registry& registry,
		const std::vector<entt::entity>& entities) {

		for (const auto& processor : m_postProcessors) {
			processor->ProcessLoadedEntities(registry, entities);
		}
	}

} // namespace WanderSpire::Scene