// src/ECS/World.cpp
#include "WanderSpire/ECS/World.h"
#include "WanderSpire/Core/EngineContext.h"

#include "WanderSpire/Systems/AnimationPlaybackSystem.h" 
#include "WanderSpire/Systems/ChunkStreamSystem.h"
#include "WanderSpire/Systems/RenderSystem.h" 
#include "WanderSpire/Systems/AnimationSystem.h" 
#include "WanderSpire/Systems/SpriteUpdateSystem.h"

namespace WanderSpire {

	void World::Initialize(EngineContext& ctx)
	{
		// ---------------------------------------------------------------------
		// Make EngineContext accessible everywhere via entt::registry::ctx()
		// We store a *pointer* to avoid an expensive and potentially unsafe
		// copy of the context object (it owns singletons, OpenGL handles, etc.).
		// ---------------------------------------------------------------------
		if (!m_Registry.ctx().contains<EngineContext*>())
			m_Registry.ctx().emplace<EngineContext*>(&ctx);

		// ---------------------------------------------------------------------
		// System bootstrap – order does not matter here, each system registers
		// its own callbacks/subscriptions internally.
		// ---------------------------------------------------------------------
		AnimationSystem::Initialize(m_Registry);
		ChunkStreamSystem::Initialize(ctx, m_Registry);
		RenderSystem::Initialize();
	}


	entt::entity World::CreateEntity() { return m_Registry.create(); }
	void         World::DestroyEntity(entt::entity e) { m_Registry.destroy(e); }

	void World::Tick(float dt, EngineContext& ctx) { ctx.tick.Update(dt); }

	void World::Update(float dt, EngineContext& ctx)
	{
		AnimationPlaybackSystem::Update(m_Registry, dt);

		SpriteUpdateSystem::Update(m_Registry, ctx);
	}

} // namespace WanderSpire