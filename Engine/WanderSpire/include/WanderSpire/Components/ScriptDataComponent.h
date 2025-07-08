#pragma once
#include <string>
#include <nlohmann/json.hpp>

namespace WanderSpire {

	struct ScriptDataComponent {
		std::string data; // Store as JSON string for max compatibility
		// If you want, you can use: nlohmann::json data; and reflect as string
	};

}
REFLECTABLE(WanderSpire::ScriptDataComponent,
	FIELD(String, data, 0, 0, 0)
)
