#pragma once

#include <string>
#include <glad/glad.h>

namespace WanderSpire {


	class Texture {
	public:
		explicit Texture(const std::string& relativePath, bool flipVertically = false);

		Texture();

		~Texture();

		void Bind(uint32_t slot = 0) const noexcept;
		void Unbind() const noexcept;

		GLuint GetID()     const noexcept { return m_TextureID; }
		int    GetWidth()  const noexcept { return m_Width; }
		int    GetHeight() const noexcept { return m_Height; }
		const std::string& GetPath() const noexcept { return m_Path; }

		void UploadFromData(const unsigned char* data, int width, int height);

	private:
		GLuint      m_TextureID = 0;
		int         m_Width = 0;
		int         m_Height = 0;
		int         m_Channels = 0;
		std::string m_Path;
	};

}
