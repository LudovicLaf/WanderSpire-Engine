#pragma once

#include <nlohmann/json.hpp>
#include <entt/entt.hpp>
#include <glm/glm.hpp>
#include <glm/vec2.hpp>

#include <WanderSpire/Core/Reflection.h>
#include <WanderSpire/Core/EngineContext.h>
#include <WanderSpire/World/Pathfinder2D.h>
#include <WanderSpire/ECS/PrefabManager.h>
#include <WanderSpire/Core/AssetManager.h>
#include <WanderSpire/ECS/World.h>
#include <WanderSpire/Components/AllComponents.h>
#include <WanderSpire/ECS/Serialization.h>


// bring symbols into tests’ namespace
using namespace WanderSpire;
using json = nlohmann::json;
