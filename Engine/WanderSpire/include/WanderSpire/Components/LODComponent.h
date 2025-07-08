#pragma once
#include <string>
#include <vector>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct LODComponent {
		struct LODLevel {
			float distance;
			std::string prefabName;
			bool cullCompletely = false;
		};

		std::vector<LODLevel> levels;
		int currentLOD = 0;
		float lodBias = 1.0f;
		bool enableLOD = true;
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::LODComponent,
	FIELD(Int, currentLOD, 0, 16, 1),
	FIELD(Float, lodBias, 0.01f, 16.0f, 0.01f),
	FIELD(Bool, enableLOD, 0, 1, 1)
)
