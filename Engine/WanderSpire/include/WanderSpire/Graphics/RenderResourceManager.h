// include/WanderSpire/Graphics/RenderResourceManager.h
#pragma once

#include <string>
#include <unordered_map>
#include <memory>
#include <glad/glad.h>
#include "WanderSpire/Graphics/Shader.h"
#include "WanderSpire/Graphics/Texture.h"
#include "WanderSpire/Graphics/TextureAtlas.h"

namespace WanderSpire {

	class RenderResourceManager {
	public:
		// publicly default-constructible
		RenderResourceManager() = default;

		void OnContextBound();

		/// Singleton accessor
		static RenderResourceManager& Get();

		/// Must be called once after you create VAO + VBO + EBO
		/// Now takes both the VAO *and* its EBO.
		void Init(GLuint vao, GLuint ebo);

		// Shaders
		void RegisterShader(const std::string& name,
			const std::string& vsPath,
			const std::string& fsPath);
		Shader* GetShader(const std::string& name);

		// Single textures (for spritesheets)
		void RegisterTexture(const std::string& name,
			const std::string& texturePath);
		std::shared_ptr<Texture> GetTexture(const std::string& name);

		// Atlases (for static sprites with multiple frames)
		void RegisterAtlas(const std::string& name,
			const std::string& atlasImagePath,
			const std::string& mappingJsonPath);
		TextureAtlas* GetAtlas(const std::string& name);
		size_t GetAtlasCount() const;

		void GenerateAtlases(const std::string& texturesSubfolder);

		/// NEW: Auto-register all spritesheets from Assets/SpriteSheets/
		void RegisterSpritesheets(const std::string& spriteSheetsRoot = "SpriteSheets");

		// Expose map for logging
		const std::unordered_map<std::string, std::unique_ptr<TextureAtlas>>&
			GetAtlasMap() const { return m_Atlases; }

		// Expose for binding before glDrawElements
		GLuint GetQuadVAO() const { return m_QuadVAO; }
		GLuint GetQuadEBO() const { return m_QuadEBO; }

	private:
		std::unordered_map<std::string, std::unique_ptr<Shader>>       m_Shaders;
		std::unordered_map<std::string, std::shared_ptr<Texture>>      m_Textures;
		std::unordered_map<std::string, std::unique_ptr<TextureAtlas>> m_Atlases;

		GLuint m_QuadVAO = 0;  ///< The quad VAO
		GLuint m_QuadEBO = 0;  ///< The quad EBO (must be bound with VAO)
	};

} // namespace WanderSpire