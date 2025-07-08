#pragma once
#include <vector>
#include <glm/glm.hpp>
#include <climits>

namespace WanderSpire {

	class TileBrush {
	public:
		enum class BrushType {
			Single,
			Rectangle,
			Circle,
			Line,
			Pattern,
			Multi
		};

		enum class BlendMode {
			Replace,
			Add,
			Subtract,
			Overlay
		};

		BrushType type = BrushType::Single;
		BlendMode blendMode = BlendMode::Replace;
		int size = 1;
		float opacity = 1.0f;
		bool randomize = false;
		float randomStrength = 0.5f;
		std::vector<std::vector<int>> pattern;
		glm::ivec2 patternSize{ 1, 1 };
		bool showPreview = true;
		glm::vec3 previewColor{ 1.0f, 1.0f, 0.0f };
		float previewAlpha = 0.5f;
	};
} // namespace WanderSpire

