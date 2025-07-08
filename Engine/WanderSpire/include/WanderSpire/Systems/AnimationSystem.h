#pragma once

#include <entt/entt.hpp>

namespace WanderSpire {

	/** Central animation hub.
	 *  • Listens for gameplay events and mutates AnimationStateComponent.
	 *  • Whenever the state changes, it re-configures SpriteAnimationComponent
	 *    by looking up the clip in AnimationClipsComponent.clips using the string key.
	 */
	struct AnimationSystem
	{
		/// Install subscriptions and entt signals. Call once at bootstrap.
		static void Initialize(entt::registry& registry);

	private:
		/// Apply the correct clip (by string) from AnimationClipsComponent → SpriteAnimationComponent.
		static void applyClip(entt::registry& reg, entt::entity e);
	};

} // namespace WanderSpire
