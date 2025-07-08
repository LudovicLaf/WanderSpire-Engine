#pragma once
#include <string>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct TilemapLayerComponent {
		int layerIndex = 0;
		std::string layerName = "Layer_0";

		// Layer properties
		float opacity = 1.0f;
		bool visible = true;
		bool locked = false;

		// Collision and physics
		bool hasCollision = false;
		int physicsLayer = 0;

		// Rendering
		int sortingOrder = 0;
		std::string materialName;

		// Tile palette integration
		int paletteId = 0;              ///< Which palette defines the tiles for this layer
		bool autoRefreshDefinitions = true;  ///< Auto-refresh tile definitions when palette changes
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::TilemapLayerComponent,
	FIELD(Int, layerIndex, 0, 32, 1),
	FIELD(String, layerName, 0, 0, 0),
	FIELD(Float, opacity, 0.0f, 1.0f, 0.01f),
	FIELD(Bool, visible, 0, 1, 1),
	FIELD(Bool, locked, 0, 1, 1),
	FIELD(Bool, hasCollision, 0, 1, 1),
	FIELD(Int, physicsLayer, 0, 32, 1),
	FIELD(Int, sortingOrder, -1000, 1000, 1),
	FIELD(String, materialName, 0, 0, 0),
	FIELD(Int, paletteId, 0, 1000, 1),
	FIELD(Bool, autoRefreshDefinitions, 0, 1, 1)
)