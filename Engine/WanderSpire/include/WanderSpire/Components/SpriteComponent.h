#pragma once
#include <string>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct SpriteComponent {
		std::string atlasName;
		std::string frameName;
	};

}

REFLECTABLE(WanderSpire::SpriteComponent,
	FIELD(String, atlasName, 0, 0, 0),
	FIELD(String, frameName, 0, 0, 0)
)
