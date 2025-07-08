// File: WanderSpire/Core/Events.h
#pragma once

#include <entt/entt.hpp>
#include "WanderSpire/World/Pathfinder2D.h"   // PathResult
#include <string>

namespace WanderSpire {

	struct AppState;

	/* ───── tick ───────────────────────────────────────────────────────────── */
	struct LogicTickEvent { uint64_t index; };

	/* ───── camera ─────────────────────────────────────────────────────────── */
	struct CameraMovedEvent {
		glm::vec2 minBound;   ///< world-space AABB min of current view
		glm::vec2 maxBound;   ///< world-space AABB max of current view
	};

	/* ───── path-finding (NEW) ─────────────────────────────────────────────── */
	class GridMap2D;   // fwd

	/** Fired by PathApplySystem after PathFollowingComponent is updated. */
	struct PathAppliedEvent {
		entt::entity              entity;
		std::vector<glm::ivec2>   checkpoints;
	};

	/* ───── movement interpolation ─────────────────────────────────────────── */
	struct MoveStartedEvent {
		entt::entity entity;
		glm::ivec2   fromTile;
		glm::ivec2   toTile;
	};

	struct MoveCompletedEvent {
		entt::entity entity;
		glm::ivec2   tile;          // logical tile on arrival
	};

	/* ───── animation ─────────────────────────────────────────────────────── */
	struct AnimationFinishedEvent { entt::entity entity; };

	/* ───── rendering ──────────────────────────────────────────────────────── */
	/// Published each frame by Application; listeners perform draw calls.
	struct FrameRenderEvent { const AppState* state; };

	/* ─── generic FSM notifications ───────────────────────────────────────── */
	/// Fired whenever an entity enters a new FSM state (managed-only now).
	struct StateEnteredEvent {
		entt::entity entity;
		std::string  state;
	};
}

