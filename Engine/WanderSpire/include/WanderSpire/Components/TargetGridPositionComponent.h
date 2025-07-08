#pragma once
#include <glm/glm.hpp>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct TargetGridPositionComponent {
		glm::ivec2 target;

		TargetGridPositionComponent() = default;
		TargetGridPositionComponent(const glm::ivec2& t) : target(t) {}
	};

}

REFLECTABLE(WanderSpire::TargetGridPositionComponent,
	FIELD(Vec2, target, -10000, 10000, 1)
)
