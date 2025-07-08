#pragma once

#include <entt/entt.hpp>

namespace WanderSpire {
	struct EngineContext;

	/** Translates SpriteComponent *or* SpriteAnimationComponent into a
	 *  SpriteRenderComponent (texture + uv + size) every frame. */
	struct SpriteUpdateSystem {
		static void Update(entt::registry& registry, const EngineContext& ctx);
	};
}