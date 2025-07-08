#pragma once
#include <string>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct TagComponent {
		std::string tag;

		TagComponent() = default;
		TagComponent(const std::string& t) : tag(t) {}
	};

}

REFLECTABLE(WanderSpire::TagComponent,
	FIELD(String, tag, 0, 0, 0)
)
