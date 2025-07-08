#pragma once
#include <string>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct LayerComponent {
		int renderLayer = 0;         // Main rendering layer
		int sortingOrder = 0;        // Order within layer
		std::string layerName = "Default";

		// Multi-layer support
		int collisionLayer = 0;      // Physics/collision layer
		int cullingLayer = 0;        // Frustum culling layer

		bool visible = true;
		bool castsShadows = false;   // Future shadow system
		bool receivesShadows = false;
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::LayerComponent,
	FIELD(Int, renderLayer, -100, 100, 1),
	FIELD(Int, sortingOrder, -1000, 1000, 1),
	FIELD(String, layerName, 0, 0, 0),
	FIELD(Int, collisionLayer, 0, 32, 1),
	FIELD(Bool, visible, 0, 1, 1)
)
