#pragma once
#include <glm/glm.hpp>
#include <cstdint>
#include <string>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct GizmoComponent {
		enum class Mode { None, Translate, Rotate, Scale, Universal };
		enum class Space { Local, World };

		Mode mode = Mode::Translate;
		Space space = Space::World;
		bool visible = true;
		bool active = false;

		// Gizmo visual settings
		float size = 1.0f;
		glm::vec3 colorX{ 1.0f, 0.0f, 0.0f };
		glm::vec3 colorY{ 0.0f, 1.0f, 0.0f };
		glm::vec3 colorZ{ 0.0f, 0.0f, 1.0f };
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::GizmoComponent,
	FIELD(Int, mode, 0, 4, 1),
	FIELD(Int, space, 0, 1, 1),
	FIELD(Bool, visible, 0, 1, 1),
	FIELD(Bool, active, 0, 1, 1),
	FIELD(Float, size, 0.01f, 10.0f, 0.01f)
)
