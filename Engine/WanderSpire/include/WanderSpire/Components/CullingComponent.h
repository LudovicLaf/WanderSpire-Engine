#pragma once
#include <glm/glm.hpp>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct CullingComponent {
		glm::vec2 boundsMin, boundsMax;  // AABB in world space
		float cullingDistance = 1000.0f; // Max view distance
		bool frustumCull = true;
		bool occlusionCull = false;
		bool alwaysVisible = false;      // Never cull (UI, etc.)
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::CullingComponent,
	FIELD(Float, cullingDistance, 0.0f, 100000.0f, 1.0f),
	FIELD(Bool, frustumCull, 0, 1, 1),
	FIELD(Bool, occlusionCull, 0, 1, 1),
	FIELD(Bool, alwaysVisible, 0, 1, 1)
)
