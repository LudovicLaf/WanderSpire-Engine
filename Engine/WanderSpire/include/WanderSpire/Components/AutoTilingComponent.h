#pragma once
#include "WanderSpire/Editor/TilePaint/AutoTiling.h"
#include "WanderSpire/Core/ReflectionMacros.h"
#include <vector>

namespace WanderSpire {

	struct AutoTilingComponent {
		std::vector<AutoTileSet> tileSets;
		bool enableAutoTiling = true;
		bool applyOnPaint = true;
		bool applyOnLoad = true;
		int maxIterations = 3;
		bool updateNeighborsOnly = true;
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::AutoTilingComponent,
	FIELD(Bool, enableAutoTiling, 0, 1, 1),
	FIELD(Bool, applyOnPaint, 0, 1, 1),
	FIELD(Bool, applyOnLoad, 0, 1, 1),
	FIELD(Int, maxIterations, 1, 10, 1),
	FIELD(Bool, updateNeighborsOnly, 0, 1, 1)
)
