#pragma once

#include <string>
#include <nlohmann/json.hpp>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	/// Tracks which animation clip an entity should play.
	/// Now purely string-based (no enum). Prefab JSON keys (e.g. "Idle","Walk", etc).
	struct AnimationStateComponent {
		std::string state;
	};

} // namespace WanderSpire

// Reflection so it appears in the editor, with a single string field "state"
REFLECTABLE(WanderSpire::AnimationStateComponent,
	FIELD(String, state, 0, 0, 0)
)
