#pragma once
#include <string>
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct CommentComponent {
		std::string comment;

		CommentComponent() = default;
		CommentComponent(const std::string& text) : comment(text) {}
	};

}

REFLECTABLE(WanderSpire::CommentComponent,
	FIELD(String, comment, 0, 0, 0)
)
