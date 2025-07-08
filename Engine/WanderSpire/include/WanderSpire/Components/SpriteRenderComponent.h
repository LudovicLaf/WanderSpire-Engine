#pragma once

#include <glm/glm.hpp>
#include <glad/glad.h>

namespace WanderSpire {

	/** Runtime‑only packet prepared each frame by SpriteUpdateSystem.
	 *  RenderSystem consumes it directly; no reflection or serialisation. */
	struct SpriteRenderComponent {
		GLuint    textureID = 0;                ///< GL texture handle
		glm::vec2 uvOffset{ 0.0f, 0.0f };      ///< lower‑left UV
		glm::vec2 uvSize{ 1.0f, 1.0f };      ///< size in UV space
		glm::vec2 worldSize{ 1.0f, 1.0f };      ///< size in world units
	};

}