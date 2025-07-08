#pragma once
#include <string>
#include <unordered_map>
#include <cstdint>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct PrefabInstanceComponent {
		std::string prefabPath;
		uint64_t prefabVersion = 0;

		// Override tracking
		std::unordered_map<std::string, std::string> overrides; // component.field -> json_value
		bool hasOverrides = false;

		// Instance state
		bool broken = false;         // Prefab asset missing
		bool outdated = false;       // Prefab version changed
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::PrefabInstanceComponent,
	FIELD(String, prefabPath, 0, 0, 0),
	FIELD(Int, prefabVersion, 0, 100000, 1),
	FIELD(Bool, hasOverrides, 0, 1, 1),
	FIELD(Bool, broken, 0, 1, 1),
	FIELD(Bool, outdated, 0, 1, 1)
)
