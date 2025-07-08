#pragma once

#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	/// Marks an entity as a blocking/interactable map prop.
	struct ObstacleComponent {
		bool blocksMovement = true;  ///< If true, pathfinder and movement will stop here
		bool blocksVision = true;  ///< If true, line-of-sight / lighting treats this as occluder
		int  zOrder = 0;     ///< Draw‐order relative to other props (lower draws first)
	};

} // namespace WanderSpire

// Reflection for editor, serialization, and tweaking in ImGui:
REFLECTABLE(WanderSpire::ObstacleComponent,
	FIELD(Bool, blocksMovement, 0, 1, 1),
	FIELD(Bool, blocksVision, 0, 1, 1),
	FIELD(Int, zOrder, -10, 10, 1)
)
