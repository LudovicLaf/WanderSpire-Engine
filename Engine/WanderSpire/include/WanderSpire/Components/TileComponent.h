#pragma once
#include <glm/glm.hpp>
#include <cstdint>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct TileComponent {
		int tileId = -1;
		glm::ivec2 gridPosition{ 0, 0 };

		// Tile properties
		bool walkable = true;
		bool destructible = false;
		float hardness = 1.0f;

		// Visual variants
		int variantIndex = 0;
		bool flipX = false, flipY = false;
		float rotation = 0.0f;

		// Tile connections (for auto-tiling)
		uint8_t connections = 0;     // Bitmask for connected directions
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::TileComponent,
	FIELD(Int, tileId, -1, 10000, 1),
	FIELD(Vec2, gridPosition, -10000, 10000, 1),
	FIELD(Bool, walkable, 0, 1, 1),
	FIELD(Bool, destructible, 0, 1, 1),
	FIELD(Float, hardness, 0.0f, 100.0f, 0.1f),
	FIELD(Int, variantIndex, 0, 255, 1),
	FIELD(Bool, flipX, 0, 1, 1),
	FIELD(Bool, flipY, 0, 1, 1)
)
