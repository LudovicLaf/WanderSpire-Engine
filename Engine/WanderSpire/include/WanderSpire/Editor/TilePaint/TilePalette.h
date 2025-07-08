#pragma once
#include <vector>
#include <string>
#include <glm/glm.hpp>

namespace WanderSpire {

	struct TilePalette {
		struct TileEntry {
			int tileId;
			std::string name;
			std::string assetPath;
			glm::ivec2 atlasPosition{ 0, 0 };
			bool walkable = true;
			int collisionType = 0;
			float weight = 1.0f;
			std::vector<int> variants;
			bool canRotate = false;
			bool canFlip = false;
		};

		std::string name;
		std::vector<TileEntry> tiles;
		std::string atlasPath;
		glm::ivec2 tileSize{ 32, 32 };
		int columns = 8;
		std::vector<std::string> categories;
	};
} // namespace WanderSpire

