/* ================================================================
   Thin internal wrapper – **private** to EngineCore.
   ================================================================ */
#pragma once
#include "EngineAPI.h"

#include <mutex>
#include <string>
#include <unordered_map>
#include <vector>

#include "WanderSpire/ECS/World.h"
#include "WanderSpire/Core/EngineContext.h"
#include <vector>
#include "WanderSpire/Core/EventBus.h" 

namespace WanderSpire { struct AppState; }

/*──────── per-context data ──────────────────────────────────────────*/
namespace EngineCoreInternal {

	/* Primitive used by the managed-overlay renderer */
	struct OverlayRect
	{
		float x, y, w, h;
		uint32_t colour;
	};

	struct Wrapper
	{
		/* filled by WanderSpire::Application --------------------------- */
		void* appState = nullptr;   /* WanderSpire::AppState* */
		WanderSpire::World* world = nullptr;
		WanderSpire::EngineContext* ctx = nullptr;

		/* ── simple C-style event bus (native clients only) ──────────── */
		std::mutex                                          cSlotsMx;

		/* ── script-tick delegates registered from managed code ─────── */
		std::mutex                scriptTicksMx;

		/* helpers ------------------------------------------------------ */
		float tileSize()  const { return ctx ? ctx->settings.tileSize : 32.f; }
		auto& reg() { return world->GetRegistry(); }

		/* invoke every managed tick delegate (called by EngineTick) */
		void runScriptTicks(float dt);

		// Custom event subscribing -------------------------------------
		struct ScriptSlot { ScriptEventCallback fn; void* user; };
		std::unordered_map<std::string, std::vector<ScriptSlot>> scriptSlots;
		std::mutex                                               scriptSlotsMx;

		// keep our forwarding subscriptions alive
		std::vector<WanderSpire::EventBus::Subscription> scriptEventSubscriptions;
	};
}
