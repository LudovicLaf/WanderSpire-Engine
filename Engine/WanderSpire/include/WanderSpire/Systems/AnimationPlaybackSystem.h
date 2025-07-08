#pragma once
#include <entt/entt.hpp>

namespace WanderSpire {

	/** Purely visual animation advancement – no gameplay logic.
	 *  Ticks SpriteAnimationComponent each frame and fires
	 *  AnimationFinishedEvent when a non‑looping clip completes. */
	struct AnimationPlaybackSystem {
		static void Update(entt::registry& registry, float deltaTime);
	};

}