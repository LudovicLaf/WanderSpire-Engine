#pragma once
#include <string>
#include <vector>

namespace WanderSpire {

	struct EditorMetadataComponent {
		std::string category = "General";
		std::string description;
		std::vector<std::string> tags;

		// Asset references for dependency tracking
		std::vector<std::string> assetDependencies;

		// Editor state
		bool expanded = true;
		bool bookmarked = false;
		uint32_t editorColor = 0xFFFFFFFF;
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::EditorMetadataComponent,
	FIELD(String, category, 0, 0, 0),
	FIELD(String, description, 0, 0, 0),
	FIELD(Bool, expanded, 0, 1, 1),
	FIELD(Bool, bookmarked, 0, 1, 1),
	FIELD(Int, editorColor, 0, 0xFFFFFFFF, 1)
)