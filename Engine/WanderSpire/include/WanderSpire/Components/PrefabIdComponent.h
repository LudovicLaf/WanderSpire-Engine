#pragma once
#include <cstdint>
#include <string>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	/// Tags every prefab-spawned entity with a stable prefab type ID + its name.
	struct PrefabIdComponent {
		uint32_t    prefabId;    // stable numeric ID per prefab kind
		std::string prefabName;  // human-readable key

		PrefabIdComponent() = default;
		PrefabIdComponent(uint32_t id, std::string name)
			: prefabId(id), prefabName(std::move(name)) {
		}
	};

}

// Register for reflection + JSON (so it will be saved/loaded)
REFLECTABLE(WanderSpire::PrefabIdComponent,
	FIELD(Int, prefabId, 0, UINT32_MAX, 1),
	FIELD(String, prefabName, 0, 0, 0)
)
