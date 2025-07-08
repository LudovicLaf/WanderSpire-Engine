#pragma once

#include <entt/entt.hpp>

namespace WanderSpire {
	struct EngineContext;

	/// Encapsulates tick‑ and frame‑based systems.
	class World {
	public:
		World() = default;
		~World() = default;

		/// Must be called once after AppState.ctx is ready.
		void Initialize(EngineContext& ctx);

		entt::entity CreateEntity();
		void         DestroyEntity(entt::entity entity);

		/// Fixed‑timestep logic.
		void Tick(float deltaTime, EngineContext& ctx);
		/// Per‑frame systems (interpolation, animation …).
		void Update(float deltaTime, EngineContext& ctx);

		/// Access the registry (mutable / read‑only)
		entt::registry& GetRegistry() { return m_Registry; }
		const entt::registry& GetRegistry() const { return m_Registry; }

	private:
		entt::registry m_Registry;
	};
} // namespace WanderSpire
