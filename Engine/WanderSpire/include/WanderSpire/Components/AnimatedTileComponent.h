#pragma once
#include <vector>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct AnimatedTileComponent {
		struct AnimationFrame {
			int tileId;
			float duration = 0.1f;
		};

		std::vector<AnimationFrame> frames;
		int currentFrame = 0;
		float elapsedTime = 0.0f;
		bool loop = true;
		bool playing = true;
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::AnimatedTileComponent,
	FIELD(Int, currentFrame, 0, 1000, 1),
	FIELD(Float, elapsedTime, 0.0f, 10.0f, 0.01f),
	FIELD(Bool, loop, 0, 1, 1),
	FIELD(Bool, playing, 0, 1, 1)
)
