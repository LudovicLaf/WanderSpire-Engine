#pragma once

#include <memory>
#include "WanderSpire/Core/EngineConfig.h"
#include "WanderSpire/Core/AssetManager.h"
#include "WanderSpire/Graphics/RenderResourceManager.h"
#include "WanderSpire/Core/TickManager.h"
#include "WanderSpire/ECS/PrefabManager.h"
#include "WanderSpire/Input/InputManager.h"
#include "WanderSpire/Scene/SceneManager.h"
#include "WanderSpire/Scene/SceneManagerFactory.h"


namespace WanderSpire {

	struct EngineContext {
		EngineContext()
			: settings{}
			, assets{}
			, renderer(RenderResourceManager::Get())
			, tick{}
			, prefabs(PrefabManager::GetInstance())
			, sceneManager(Scene::SceneManagerFactory::CreateDefault())

		{
		}

		EngineConfig              settings;
		AssetManager              assets;
		RenderResourceManager& renderer;
		TickManager               tick;
		PrefabManager& prefabs;
		std::unique_ptr<Scene::SceneManager> sceneManager;

	};

}