#pragma once

#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	/// Cardinal facing direction for 2D sprites
	enum class Facing {
		Right = 0,
		Left,
		Up,
		Down
	};

	/// Tracks which way an entity is facing; used for flipping sprites
	struct FacingComponent {
		Facing facing = Facing::Right;
	};

} // namespace WanderSpire

// Expose to reflection so you can tweak in the editor if desired:
REFLECTABLE(WanderSpire::FacingComponent,
	FIELD(Int, facing, 0, 3, 1)
)
