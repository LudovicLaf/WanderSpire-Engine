#pragma once
#include "WanderSpire/Editor/TilePaint/TileBrush.h"
#include <vector>
#include <glm/glm.hpp>
#include <climits>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct TileBrushComponent {
		TileBrush brush;
		bool isActive = false;
		glm::ivec2 lastPaintPosition{ INT_MAX, INT_MAX };
		bool isPainting = false;
		glm::ivec2 paintStartPos{ 0, 0 };
		std::vector<glm::ivec2> paintPreview;
		std::vector<glm::ivec2> currentStroke;
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::TileBrushComponent,
	FIELD(Bool, isActive, 0, 1, 1),
	FIELD(Bool, isPainting, 0, 1, 1),
	FIELD(Vec2, paintStartPos, -10000, 10000, 1),
	FIELD(Vec2, lastPaintPosition, -10000, 10000, 1)
)
