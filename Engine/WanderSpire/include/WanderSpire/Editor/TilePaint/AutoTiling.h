#pragma once
#include <vector>
#include <string>

namespace WanderSpire {

	struct AutoTileRule {
		enum class NeighborState {
			DontCare = 0,
			Empty = 1,
			Filled = 2,
			Different = 3
		};
		NeighborState neighbors[9];
		int resultTileId;
		int priority = 0;
		std::string ruleName;
	};

	struct AutoTileSet {
		std::string name;
		std::vector<AutoTileRule> rules;
		std::vector<int> baseTileIds;
		bool enabled = true;
	};
} // namespace WanderSpire

