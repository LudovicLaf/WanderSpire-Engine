#pragma once
#include <glm/glm.hpp>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct TransformComponent {
		// Local transform (relative to parent)
		glm::vec2 localPosition{ 0.0f, 0.0f };
		float localRotation = 0.0f;
		glm::vec2 localScale{ 1.0f, 1.0f };

		// World transform cache
		glm::vec2 worldPosition{ 0.0f, 0.0f };
		float worldRotation = 0.0f;
		glm::vec2 worldScale{ 1.0f, 1.0f };

		// Transform state
		bool isDirty = true;
		bool freezeTransform = false;  // Editor lock

		// Pivot support
		glm::vec2 pivot{ 0.5f, 0.5f };   // Normalized pivot point

		// Transform constraints
		bool lockX = false, lockY = false, lockRotation = false;
		bool lockScaleX = false, lockScaleY = false;
	};

} // namespace WanderSpire

// Reflection registration
REFLECTABLE(WanderSpire::TransformComponent,
	FIELD(Vec2, localPosition, -10000.0f, 10000.0f, 0.1f),
	FIELD(Float, localRotation, -6.28f, 6.28f, 0.01f),
	FIELD(Vec2, localScale, 0.01f, 100.0f, 0.01f),
	FIELD(Vec2, pivot, 0.0f, 1.0f, 0.01f),
	FIELD(Bool, freezeTransform, 0, 1, 1)
)
