#include "WanderSpire/Systems/AnimationPlaybackSystem.h"

#include "WanderSpire/Components/SpriteAnimationComponent.h"
#include "WanderSpire/Core/EventBus.h"
#include "WanderSpire/Core/Events.h"

namespace WanderSpire {

	void AnimationPlaybackSystem::Update(entt::registry& registry, float dt)
	{
		auto view = registry.view<SpriteAnimationComponent>();

		for (auto e : view)
		{
			auto& anim = view.get<SpriteAnimationComponent>(e);
			if (anim.finished) continue;            // already done

			anim.elapsedTime += dt;
			while (anim.elapsedTime >= anim.frameDuration && !anim.finished)
			{
				anim.elapsedTime -= anim.frameDuration;
				++anim.currentFrame;

				if (anim.currentFrame >= anim.frameCount)
				{
					if (anim.loop)
						anim.currentFrame = 0;           // wrap‑around
					else {
						anim.currentFrame = anim.frameCount - 1;
						anim.finished = true;
						EventBus::Get().Publish<AnimationFinishedEvent>({ e });
					}
				}
			}
		}
	}

}