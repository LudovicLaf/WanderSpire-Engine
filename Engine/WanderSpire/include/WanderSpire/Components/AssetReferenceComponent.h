#pragma once
#include <string>
#include <vector>
#include <cstdint>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct AssetReferenceComponent {
		struct AssetRef {
			std::string assetId;
			std::string assetPath;
			uint64_t lastModified = 0;
			bool missing = false;
		};

		std::vector<AssetRef> dependencies;
		bool dependenciesResolved = true;
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::AssetReferenceComponent,
	FIELD(Bool, dependenciesResolved, 0, 1, 1)
)
