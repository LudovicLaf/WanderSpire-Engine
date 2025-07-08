#include "WanderSpire/Scene/SceneManagerFactory.h"
#include "WanderSpire/Scene/JsonSceneLoader.h"
#include "WanderSpire/Scene/PostProcessors.h"
#include <WanderSpire/Scene/JsonSceneSaver.h>

namespace WanderSpire::Scene {

	std::unique_ptr<SceneManager> SceneManagerFactory::CreateDefault() {
		auto manager = std::make_unique<SceneManager>();

		RegisterDefaultLoaders(*manager);
		RegisterDefaultSavers(*manager);
		RegisterDefaultPostProcessors(*manager);

		return manager;
	}

	std::unique_ptr<SceneManager> SceneManagerFactory::CreateCustom(
		std::vector<std::unique_ptr<ISceneLoader>> loaders,
		std::vector<std::unique_ptr<ISceneSaver>> savers,
		std::vector<std::unique_ptr<IEntityPostProcessor>> processors) {

		auto manager = std::make_unique<SceneManager>();

		for (auto& loader : loaders) {
			manager->RegisterLoader(std::move(loader));
		}

		for (auto& saver : savers) {
			manager->RegisterSaver(std::move(saver));
		}

		for (auto& processor : processors) {
			manager->RegisterPostProcessor(std::move(processor));
		}

		return manager;
	}

	void SceneManagerFactory::RegisterDefaultLoaders(SceneManager& manager) {
		manager.RegisterLoader(std::make_unique<JsonSceneLoader>());
		// Add other loaders here (Binary, XML, etc.)
	}

	void SceneManagerFactory::RegisterDefaultSavers(SceneManager& manager) {
		manager.RegisterSaver(std::make_unique<JsonSceneSaver>());
		// Add other savers here
	}

	void SceneManagerFactory::RegisterDefaultPostProcessors(SceneManager& manager) {
		manager.RegisterPostProcessor(std::make_unique<HierarchyPostProcessor>());
		manager.RegisterPostProcessor(std::make_unique<TilemapPostProcessor>());
		manager.RegisterPostProcessor(std::make_unique<TexturePostProcessor>());
		manager.RegisterPostProcessor(std::make_unique<AnimationPostProcessor>());
		manager.RegisterPostProcessor(std::make_unique<PositionPostProcessor>());
	}

} // namespace WanderSpire::Scene