// include/WanderSpire/Graphics/SpriteRenderer.h
#pragma once

#include <glm/glm.hpp>
#include <glad/glad.h>
#include "WanderSpire/Graphics/Shader.h"

namespace WanderSpire {

	/// Renders quads, either textured or solid-color (when textureID==0).
	class SpriteRenderer {
	public:
		static SpriteRenderer& Get();

		/// Must be called each frame before any DrawSprite.
		void BeginFrame(const glm::mat4& viewProjection);

		/// Draw a quad. If textureID==0, draws a solid quad with 'color'.
		void DrawSprite(GLuint textureID,
			const glm::vec2& position,
			const glm::vec2& size,
			float rotation,
			const glm::vec3& color,
			const glm::vec2& uvMin,
			const glm::vec2& uvMax);

		/// Helper to draw a colored border around a tile.
		void DrawTileBorder(const glm::vec2& worldPos,
			float tileSize,
			const glm::vec3& color);

		/// Call when finished drawing.
		void EndFrame();

	private:
		SpriteRenderer();
		Shader* m_Shader = nullptr;
		GLuint  m_QuadVAO = 0;
	};

} // namespace WanderSpire
