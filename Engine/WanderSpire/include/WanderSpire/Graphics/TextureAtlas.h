#pragma once

#include <string>
#include <unordered_map>
#include <glm/glm.hpp>
#include <memory>
#include "WanderSpire/Graphics/Texture.h"

namespace WanderSpire {

	struct AtlasFrame {
		glm::vec2 uvOffset;
		glm::vec2 uvSize;
	};

	class TextureAtlas {
	public:

		void Load(const std::string& atlasImagePath, const std::string& mappingJsonPath);

		std::shared_ptr<Texture> GetTexture() const { return m_AtlasTexture; }
		AtlasFrame               GetFrame(const std::string& name) const;

	private:
		std::shared_ptr<Texture>              m_AtlasTexture;
		std::unordered_map<std::string, AtlasFrame> m_Frames;
	};

}
