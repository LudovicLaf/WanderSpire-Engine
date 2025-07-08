// src/Graphics/Texture.cpp
#include "WanderSpire/Graphics/Texture.h"
#include "WanderSpire/Core/AssetManager.h"
#include "WanderSpire/External/stb_image.h"
#include <spdlog/spdlog.h>
#include <filesystem>

namespace WanderSpire {

	Texture::Texture(const std::string& relativePath, bool flipVertically)
		: m_Path(relativePath)
	{
		auto full = AssetManager::GetAssetsRoot() / relativePath;
		stbi_set_flip_vertically_on_load(flipVertically);
		unsigned char* data = stbi_load(
			full.string().c_str(),
			&m_Width, &m_Height, &m_Channels,
			STBI_rgb_alpha
		);
		if (!data) {
			spdlog::error("[Texture] Failed to load: {}", full.string());
			return;
		}

		glGenTextures(1, &m_TextureID);
		glBindTexture(GL_TEXTURE_2D, m_TextureID);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
		glTexImage2D(
			GL_TEXTURE_2D, 0, GL_RGBA,
			m_Width, m_Height, 0,
			GL_RGBA, GL_UNSIGNED_BYTE, data
		);

		stbi_image_free(data);
		spdlog::info("[Texture] Loaded synchronously: {} ({}×{})",
			full.string(), m_Width, m_Height);
	}

	Texture::Texture() {
		// Create a 1×1 white placeholder so m_TextureID never zero
		m_Width = m_Height = 1;
		m_Channels = 4;
		unsigned char white[4] = { 255, 255, 255, 255 };

		glGenTextures(1, &m_TextureID);
		glBindTexture(GL_TEXTURE_2D, m_TextureID);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
		glTexImage2D(
			GL_TEXTURE_2D, 0, GL_RGBA,
			m_Width, m_Height, 0,
			GL_RGBA, GL_UNSIGNED_BYTE, white
		);
		glBindTexture(GL_TEXTURE_2D, 0);

		spdlog::info("[Texture] Created 1×1 white placeholder (ID={})", m_TextureID);
	}

	Texture::~Texture() {
		if (m_TextureID) {
			glDeleteTextures(1, &m_TextureID);
			spdlog::info("[Texture] Deleted GPU texture{}",
				m_Path.empty() ? "" : (" for " + m_Path));
		}
	}

	void Texture::Bind(uint32_t slot) const noexcept {
		glActiveTexture(GL_TEXTURE0 + slot);
		glBindTexture(GL_TEXTURE_2D, m_TextureID);
	}

	void Texture::Unbind() const noexcept {
		glBindTexture(GL_TEXTURE_2D, 0);
	}

	void Texture::UploadFromData(const unsigned char* data, int width, int height) {
		m_Width = width;
		m_Height = height;
		m_Channels = 4;

		if (m_TextureID) {
			glDeleteTextures(1, &m_TextureID);
		}
		glGenTextures(1, &m_TextureID);
		glBindTexture(GL_TEXTURE_2D, m_TextureID);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
		glTexImage2D(
			GL_TEXTURE_2D, 0, GL_RGBA,
			m_Width, m_Height, 0,
			GL_RGBA, GL_UNSIGNED_BYTE, data
		);
		glBindTexture(GL_TEXTURE_2D, 0);

		spdlog::info("[Texture] Async upload complete ({}×{})", m_Width, m_Height);
	}

} // namespace WanderSpire
