#pragma once
#include <glm/glm.hpp>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct GridPositionComponent {
		glm::ivec2 tile = { 0, 0 };

		// Scene editor enhancements
		bool snapToGrid = true;              // Editor: snap to grid during movement
		bool lockPosition = false;           // Editor: prevent position changes

		GridPositionComponent() = default;
		GridPositionComponent(const glm::ivec2& t) : tile(t) {}
		GridPositionComponent(int x, int y) : tile(x, y) {}
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::GridPositionComponent,
	FIELD(Vec2, tile, -10000, 10000, 1),
	FIELD(Bool, snapToGrid, 0, 1, 1),
	FIELD(Bool, lockPosition, 0, 1, 1)
)