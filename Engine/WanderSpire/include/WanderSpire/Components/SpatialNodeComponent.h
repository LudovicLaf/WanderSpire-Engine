#pragma once
#include <glm/glm.hpp>
#include <cstdint>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct SpatialNodeComponent {
		uint64_t nodeId = 0;
		glm::vec2 boundsMin, boundsMax;
		int depth = 0;

		// Optimization hints
		bool static_ = false;        // Object doesn't move
		bool large = false;          // Object spans multiple nodes
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::SpatialNodeComponent,
	FIELD(Int, nodeId, 0, 1000000000, 1),
	FIELD(Int, depth, 0, 32, 1),
	FIELD(Bool, static_, 0, 1, 1),
	FIELD(Bool, large, 0, 1, 1)
)
