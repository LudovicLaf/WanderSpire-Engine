#pragma once
#include "SceneManager.h"
#include <memory>

namespace WanderSpire::Scene {

	class SceneManagerFactory {
	public:
		// Create a fully configured scene manager
		static std::unique_ptr<SceneManager> CreateDefault();

		// Create with custom configuration
		static std::unique_ptr<SceneManager> CreateCustom(
			std::vector<std::unique_ptr<ISceneLoader>> loaders,
			std::vector<std::unique_ptr<ISceneSaver>> savers,
			std::vector<std::unique_ptr<IEntityPostProcessor>> processors
		);

	private:
		static void RegisterDefaultLoaders(SceneManager& manager);
		static void RegisterDefaultSavers(SceneManager& manager);
		static void RegisterDefaultPostProcessors(SceneManager& manager);
	};

} // namespace WanderSpire::Scene

