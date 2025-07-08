#pragma once
#include <glm/glm.hpp>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct SelectableComponent {
		bool selectable = true;
		bool selected = false;       // Current selection state

		// Selection bounds (world space)
		glm::vec2 boundsMin{ -0.5f, -0.5f };
		glm::vec2 boundsMax{ 0.5f, 0.5f };

		// Selection visualization
		glm::vec3 selectionColor{ 1.0f, 0.5f, 0.0f };
		bool showBounds = true;
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::SelectableComponent,
	FIELD(Bool, selectable, 0, 1, 1),
	FIELD(Bool, selected, 0, 1, 1),
	FIELD(Vec2, boundsMin, -1000.0f, 1000.0f, 0.1f),
	FIELD(Vec2, boundsMax, -1000.0f, 1000.0f, 0.1f),
	FIELD(Bool, showBounds, 0, 1, 1)
)
