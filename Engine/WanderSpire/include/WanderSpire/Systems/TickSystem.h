#pragma once
#include "WanderSpire/Core/TickManager.h"

namespace WanderSpire {
	class TickSystem {
	public:
		static TickManager& Get() {
			static TickManager instance;
			return instance;
		}
	};
}
