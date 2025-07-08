#include "WanderSpire/Core/TickManager.h"
#include "WanderSpire/Core/EventBus.h"
#include "WanderSpire/Core/Events.h"

namespace WanderSpire {

	void TickManager::Update(float dt)
	{
		m_Accumulator += dt;
		if (m_Accumulator >= m_TickInterval)
		{
			m_Accumulator -= m_TickInterval;
			++m_TickCounter;
			EventBus::Get().Publish<LogicTickEvent>({ m_TickCounter });
		}
	}

} // namespace WanderSpire
