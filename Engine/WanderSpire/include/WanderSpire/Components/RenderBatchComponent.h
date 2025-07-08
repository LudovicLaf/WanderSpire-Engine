#pragma once
#include <cstdint>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct RenderBatchComponent {
		uint32_t batchId = 0;
		uint32_t sortKey = 0;        // For state sorting
		bool dynamic = true;         // Can change frequently
		bool occluder = false;       // Occludes other objects
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::RenderBatchComponent,
	FIELD(Int, batchId, 0, 100000, 1),
	FIELD(Int, sortKey, 0, 1000000, 1),
	FIELD(Bool, dynamic, 0, 1, 1),
	FIELD(Bool, occluder, 0, 1, 1)
)
