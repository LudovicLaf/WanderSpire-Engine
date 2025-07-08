#pragma once
#include <cstdint>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct IDComponent {
		uint64_t uuid;

		IDComponent() = default;
		IDComponent(uint64_t id) : uuid(id) {}
	};

}

REFLECTABLE(WanderSpire::IDComponent,
	FIELD(Int, uuid, 0, 1000000000, 1)
)
