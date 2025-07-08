// ─────────────────────────────────────────────────────────────────────────────
//  Graphics/SpriteRenderer.cpp   (complete, updated 2025‑05‑09)
// ─────────────────────────────────────────────────────────────────────────────
#include "WanderSpire/Graphics/SpriteRenderer.h"
#include "WanderSpire/Graphics/RenderResourceManager.h"
#include <spdlog/spdlog.h>
#include <glm/gtc/matrix_transform.hpp>

namespace WanderSpire {

	SpriteRenderer::SpriteRenderer()
	{
		m_Shader = RenderResourceManager::Get().GetShader("sprite");
		if (!m_Shader)
			spdlog::error("[SpriteRenderer] 'sprite' shader not found!");

		/* one‑time sampler / uniform setup */
		if (m_Shader && m_Shader->GetID()) {
			m_Shader->Bind();
			m_Shader->SetUniformInt("u_Texture", 0);
			m_Shader->SetUniformInt("u_UseTexture", 1);
			m_Shader->Unbind();
		}

		m_QuadVAO = RenderResourceManager::Get().GetQuadVAO();
	}

	SpriteRenderer& SpriteRenderer::Get()
	{
		static SpriteRenderer inst;
		return inst;
	}

	/* ───────────────────────────────────────── Frame scope ──────────────── */

	void SpriteRenderer::BeginFrame(const glm::mat4& viewProjection)
	{
		auto& rm = RenderResourceManager::Get();
		if (!m_Shader || !m_Shader->GetID()
			|| rm.GetQuadVAO() == 0 || rm.GetQuadEBO() == 0)
			return;

		m_Shader->Bind();
		m_Shader->SetUniformMat4("u_ViewProjection", viewProjection);
		m_Shader->SetUniformInt("u_UseTexture", 1);
		m_Shader->SetUniformInt("u_UseInstancing", 0);   // sprites only

		glBindVertexArray(rm.GetQuadVAO());
		glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, rm.GetQuadEBO());
	}

	void SpriteRenderer::EndFrame()
	{
		glBindVertexArray(0);
		if (m_Shader && m_Shader->GetID())
			m_Shader->Unbind();
	}

	/* ───────────────────────────────────────── Quad draw ────────────────── */

	void SpriteRenderer::DrawSprite(GLuint textureID,
		const glm::vec2& position,
		const glm::vec2& size,
		float           rotation,
		const glm::vec3& color,
		const glm::vec2& uvMin,
		const glm::vec2& uvSize)
	{
		auto& rm = RenderResourceManager::Get();
		if (!m_Shader || !m_Shader->GetID()
			|| rm.GetQuadVAO() == 0 || rm.GetQuadEBO() == 0)
			return;

		/* ensure VAO/EBO are bound (user may interleave raw GL elsewhere) */
		glBindVertexArray(rm.GetQuadVAO());
		glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, rm.GetQuadEBO());

		const bool useTex = (textureID != 0);
		m_Shader->SetUniformInt("u_UseTexture", useTex ? 1 : 0);

		if (useTex) {
			glActiveTexture(GL_TEXTURE0);
			glBindTexture(GL_TEXTURE_2D, textureID);
		}

		/*  IMPORTANT CHANGE  – the incoming position is already the quad
			*centre*, so we drop the ±½ size translate pair.                */
		glm::mat4 model = glm::translate(glm::mat4(1.0f),
			glm::vec3(position, 0.0f));
		model = glm::rotate(model, rotation, glm::vec3(0, 0, 1));
		model = glm::scale(model, glm::vec3(size, 1.0f));

		m_Shader->SetUniformMat4("u_Model", model);
		m_Shader->SetUniformVec3("u_Color", color);
		m_Shader->SetUniformVec2("u_UVOffset", uvMin);
		m_Shader->SetUniformVec2("u_UVSize", uvSize);

		glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, nullptr);

		/* restore default (textured) state so the next call is predictable */
		m_Shader->SetUniformInt("u_UseTexture", 1);
	}

	/* helper for debug overlays */
	void SpriteRenderer::DrawTileBorder(const glm::vec2& pos,
		float             tileSize,
		const glm::vec3& col)
	{
		const float t = 2.0f;                         // border thickness
		DrawSprite(0, pos + glm::vec2(0, tileSize - t), { tileSize, t }, 0, col, { 0,0 }, { 1,1 });
		DrawSprite(0, pos, { tileSize, t }, 0, col, { 0,0 }, { 1,1 });
		DrawSprite(0, pos, { t, tileSize }, 0, col, { 0,0 }, { 1,1 });
		DrawSprite(0, pos + glm::vec2(tileSize - t, 0), { t, tileSize }, 0, col, { 0,0 }, { 1,1 });
	}

} // namespace WanderSpire
