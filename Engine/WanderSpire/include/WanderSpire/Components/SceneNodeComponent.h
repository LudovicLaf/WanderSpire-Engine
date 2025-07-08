#pragma once
#include <entt/entt.hpp>
#include <glm/glm.hpp>
#include <string>
#include <vector>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct SceneNodeComponent {
		entt::entity parent = entt::null;
		std::vector<entt::entity> children;

		std::string name = "GameObject";
		bool expanded = true;        // Editor UI state
		bool visible = true;         // Hierarchy visibility
		bool locked = false;         // Editor lock state
		bool static_ = false;        // Static batching hint

		// Transform caching for performance
		glm::mat4 worldMatrix = glm::mat4(1.0f);
		bool worldMatrixDirty = true;
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::SceneNodeComponent,
	FIELD(String, name, 0, 0, 0),
	FIELD(Bool, expanded, 0, 1, 1),
	FIELD(Bool, visible, 0, 1, 1),
	FIELD(Bool, locked, 0, 1, 1),
	FIELD(Bool, static_, 0, 1, 1)
)
