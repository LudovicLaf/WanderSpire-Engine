#pragma once
#include "WanderSpire/Core/ConfigManager.h"
#include <cstdint>

namespace WanderSpire {

	/** Fixed‑interval logic tick accumulator.
	 *
	 *  * Call `Update(deltaSeconds)` from your frame loop.
	 *  * After enough time has accumulated it emits a `LogicTickEvent`
	 *    via the global EventBus (handled in TickManager.cpp).
	 */
	class TickManager {
	public:
		/** Advance the accumulator and fire zero‑or‑more ticks. */
		void     Update(float deltaTime);

		/** Running tick counter since application start. */
		uint64_t GetCurrentTick()   const { return m_TickCounter; }

		/** Configured tick period in seconds (defaults to config file). */
		float    GetTickInterval()  const { return m_TickInterval; }

	private:
		const float   m_TickInterval =
			ConfigManager::Get().tickInterval;

		float         m_Accumulator = 0.0f;
		uint64_t      m_TickCounter = 0;
	};

} // namespace WanderSpire
