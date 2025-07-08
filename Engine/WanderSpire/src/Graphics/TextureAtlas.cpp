#include "WanderSpire/Graphics/TextureAtlas.h"
#include "WanderSpire/Core/AssetManager.h"
#include "WanderSpire/External/stb_image.h"
#include <nlohmann/json.hpp>
#include <fstream>
#include <spdlog/spdlog.h>
#include <filesystem>

namespace WanderSpire {

	void TextureAtlas::Load(const std::string& atlasImagePath, const std::string& mappingJsonPath) {
		namespace fs = std::filesystem;

		try {
			// Load texture with RAII
			fs::path imgPath = AssetManager::GetAssetsRoot() / atlasImagePath;

			int w, h, ch;
			stbi_set_flip_vertically_on_load(false);
			std::unique_ptr<unsigned char, decltype(&stbi_image_free)> pix(
				stbi_load(imgPath.string().c_str(), &w, &h, &ch, STBI_rgb_alpha),
				stbi_image_free
			);

			if (!pix) {
				spdlog::error("[TextureAtlas] Can't open '{}'", imgPath.string());
				return;
			}

			if (!m_AtlasTexture) {
				m_AtlasTexture = std::make_shared<Texture>();
			}

			m_AtlasTexture->UploadFromData(pix.get(), w, h);

			// JSON parsing with better error handling
			fs::path mapPath = AssetManager::GetAssetsRoot() / mappingJsonPath;
			std::ifstream file(mapPath);
			if (!file.is_open()) {
				spdlog::error("[TextureAtlas] Failed to open atlas JSON: {}", mapPath.string());
				return;
			}

			nlohmann::json j;
			file >> j;

			if (!j.contains("meta") || !j.contains("frames")) {
				spdlog::error("[TextureAtlas] Invalid atlas JSON format: {}", mapPath.string());
				return;
			}

			int atlasW = j["meta"].value("width", 0);
			int atlasH = j["meta"].value("height", 0);
			if (atlasW <= 0 || atlasH <= 0) {
				spdlog::error("[TextureAtlas] Invalid atlas dimensions in {}", mapPath.string());
				return;
			}

			m_Frames.clear();
			float invW = 1.0f / float(atlasW);
			float invH = 1.0f / float(atlasH);

			for (auto& [name, frame] : j["frames"].items()) {
				int x = frame.value("x", 0);
				int y = frame.value("y", 0);
				int frameW = frame.value("w", 0);
				int frameH = frame.value("h", 0);

				// Add bounds checking
				if (x < 0 || y < 0 || frameW <= 0 || frameH <= 0 ||
					x + frameW > atlasW || y + frameH > atlasH) {
					spdlog::warn("[TextureAtlas] Invalid frame bounds for '{}': x={}, y={}, w={}, h={}",
						name, x, y, frameW, frameH);
					continue;
				}

				float u0 = (x + 0.5f) * invW;
				float v0 = (y + 0.5f) * invH;
				float u1 = (x + frameW - 0.5f) * invW;
				float v1 = (y + frameH - 0.5f) * invH;

				AtlasFrame af;
				af.uvOffset = { u0, v0 };
				af.uvSize = { u1 - u0, v1 - v0 };
				m_Frames[name] = af;
			}

			spdlog::info("[TextureAtlas] Loaded atlas '{}' with {} frames",
				atlasImagePath, m_Frames.size());

		}
		catch (const std::exception& e) {
			spdlog::error("[TextureAtlas] Exception loading atlas: {}", e.what());
		}
	}

	// Add missing implementation of GetFrame to resolve linker errors
	AtlasFrame TextureAtlas::GetFrame(const std::string& name) const {
		auto it = m_Frames.find(name);
		if (it == m_Frames.end()) {
			spdlog::warn("[TextureAtlas] Frame '{}' not found in atlas", name);
			return AtlasFrame{};
		}
		return it->second;
	}

} // namespace WanderSpire
