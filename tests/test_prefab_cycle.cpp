#include <catch2/catch_test_macros.hpp>
#include "TestHelpers.h"

#include <string>
#include <filesystem>
#include <glm/vec2.hpp>
#include <entt/entt.hpp>
#include <WanderSpire/Core/EngineContext.h>
#include <WanderSpire/ECS/PrefabManager.h>
#include <WanderSpire/Components/AllComponents.h>

static std::string GetAssetsDirectory()
{
	// __FILE__ is ".../tests/test_prefab_cycle.cpp"
	std::string thisFile = __FILE__;
	size_t pos = thisFile.find("/tests/");
	if (pos == std::string::npos)
		pos = thisFile.find("\\tests\\");
	std::string projectRoot;
	if (pos != std::string::npos) {
		projectRoot = thisFile.substr(0, pos);
	}
	else {
		projectRoot = std::filesystem::current_path().string();
	}

	std::filesystem::path assets = projectRoot;
	assets /= "Engine";
	assets /= "WanderSpire";
	assets /= "src";
	assets /= "assets";
	return assets.lexically_normal().string();
}

TEST_CASE("PrefabManager loads and instantiates a bush", "[prefab]") {
	entt::registry reg;

	// We need an EngineContext* in the registry so PrefabManager can compute grid tiles
	WanderSpire::EngineContext ctx;
	reg.ctx().emplace<WanderSpire::EngineContext*>(&ctx);

	// Point assets root at your engine’s asset folder (absolute path)
	auto assetsDir = GetAssetsDirectory();
	ctx.assets.Initialize(assetsDir);

	// Load and instantiate
	auto& pm = WanderSpire::PrefabManager::GetInstance();
	pm.LoadPrefabsFromFolder(assetsDir + "/prefabs");

	auto ent = pm.Instantiate("bush", reg, glm::vec2{ 0.0f, 0.0f });

	// Instantiation should produce a valid entity
	REQUIRE(reg.valid(ent));

	// Tear down and ensure it's destroyed
	reg.destroy(ent);
	REQUIRE(!reg.valid(ent));
}
