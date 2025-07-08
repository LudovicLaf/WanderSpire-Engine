#include "WanderSpire/Systems/AnimationSystem.h"
#include "WanderSpire/Components/AnimationStateComponent.h"
#include "WanderSpire/Components/AnimationClipsComponent.h"
#include "WanderSpire/Components/SpriteAnimationComponent.h"

#include "WanderSpire/Core/EventBus.h"
#include "WanderSpire/Core/Events.h"

namespace WanderSpire {

	void AnimationSystem::Initialize(entt::registry& registry)
	{
		// Whenever AnimationStateComponent is constructed or updated, re‐apply the clip:
		registry.on_construct<AnimationStateComponent>()
			.connect<&AnimationSystem::applyClip>();
		registry.on_update<AnimationStateComponent>()
			.connect<&AnimationSystem::applyClip>();
	}

	void AnimationSystem::applyClip(entt::registry& reg, entt::entity e)
	{
		// Must have all three components:
		if (!reg.all_of<AnimationStateComponent, AnimationClipsComponent, SpriteAnimationComponent>(e))
			return;

		auto& stateComp = reg.get<AnimationStateComponent>(e);
		const std::string& st = stateComp.state;

		auto& clipsComp = reg.get<AnimationClipsComponent>(e);
		auto it = clipsComp.clips.find(st);
		if (it == clipsComp.clips.end())
			return; // no matching clip → do nothing

		const auto& clip = it->second;
		auto& anim = reg.get<SpriteAnimationComponent>(e);

		// Configure the SpriteAnimationComponent fields:
		anim.startFrame = clip.startFrame;
		anim.frameCount = clip.frameCount;
		anim.frameDuration = clip.frameDuration;
		anim.loop = clip.loop;
		anim.currentFrame = 0;
		anim.elapsedTime = 0.0f;
		anim.finished = false;
	}

} // namespace WanderSpire
