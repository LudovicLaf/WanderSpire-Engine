#include "WanderSpire/Graphics/InstanceRenderer.h"
#include "WanderSpire/Graphics/Shader.h"
#include <spdlog/spdlog.h>
#include <cstring>

namespace WanderSpire {

	InstanceRenderer::InstanceRenderer() = default;

	InstanceRenderer::~InstanceRenderer() {
		CleanupResources();
	}

	InstanceRenderer::InstanceRenderer(InstanceRenderer&& other) noexcept
		: m_InstanceVBO(other.m_InstanceVBO)
		, m_CurrentShader(other.m_CurrentShader)
		, m_CurrentVAO(other.m_CurrentVAO)
		, m_CurrentEBO(other.m_CurrentEBO)
		, m_AttributesSetup(other.m_AttributesSetup)
	{
		other.m_InstanceVBO = 0;
		other.m_CurrentShader = nullptr;
		other.m_CurrentVAO = 0;
		other.m_CurrentEBO = 0;
		other.m_AttributesSetup = false;
	}

	InstanceRenderer& InstanceRenderer::operator=(InstanceRenderer&& other) noexcept {
		if (this != &other) {
			CleanupResources();

			m_InstanceVBO = other.m_InstanceVBO;
			m_CurrentShader = other.m_CurrentShader;
			m_CurrentVAO = other.m_CurrentVAO;
			m_CurrentEBO = other.m_CurrentEBO;
			m_AttributesSetup = other.m_AttributesSetup;

			other.m_InstanceVBO = 0;
			other.m_CurrentShader = nullptr;
			other.m_CurrentVAO = 0;
			other.m_CurrentEBO = 0;
			other.m_AttributesSetup = false;
		}
		return *this;
	}

	InstanceRenderer& InstanceRenderer::Get() {
		static InstanceRenderer instance;
		return instance;
	}

	void InstanceRenderer::BeginFrame(Shader* shader, GLuint quadVAO, GLuint quadEBO) {
		m_CurrentShader = shader;
		m_CurrentVAO = quadVAO;
		m_CurrentEBO = quadEBO;

		if (!m_CurrentShader || !m_CurrentVAO || !m_CurrentEBO) {
			spdlog::error("[InstanceRenderer] Invalid rendering state");
			return;
		}

		// Create VBO on first use
		if (m_InstanceVBO == 0) {
			glGenBuffers(1, &m_InstanceVBO);
			spdlog::debug("[InstanceRenderer] Created instance VBO: {}", m_InstanceVBO);
		}

		glBindVertexArray(m_CurrentVAO);
		glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, m_CurrentEBO);

		// Setup vertex attributes on first use
		if (!m_AttributesSetup) {
			SetupVertexAttributes();
			m_AttributesSetup = true;
		}
	}

	void InstanceRenderer::RenderInstances(GLuint textureID,
		const std::vector<InstanceData>& instances,
		float tileSize) {
		if (!m_CurrentShader || instances.empty()) return;

		// Upload instance data
		glBindBuffer(GL_ARRAY_BUFFER, m_InstanceVBO);
		glBufferData(GL_ARRAY_BUFFER,
			instances.size() * sizeof(InstanceData),
			instances.data(),
			GL_DYNAMIC_DRAW);

		// Set uniforms
		m_CurrentShader->SetUniformInt("u_UseInstancing", 1);
		m_CurrentShader->SetUniformFloat("u_TileSize", tileSize);

		// Bind texture
		if (textureID != 0) {
			glActiveTexture(GL_TEXTURE0);
			glBindTexture(GL_TEXTURE_2D, textureID);
		}

		// Draw instances
		glDrawElementsInstanced(GL_TRIANGLES, 6, GL_UNSIGNED_INT,
			nullptr, static_cast<GLsizei>(instances.size()));
	}

	void InstanceRenderer::EndFrame() {
		if (m_CurrentShader) {
			m_CurrentShader->SetUniformInt("u_UseInstancing", 0);
		}

		glBindVertexArray(0);
		glBindBuffer(GL_ARRAY_BUFFER, 0);

		m_CurrentShader = nullptr;
		m_CurrentVAO = 0;
		m_CurrentEBO = 0;
	}

	void InstanceRenderer::SetupVertexAttributes() {
		if (m_InstanceVBO == 0) return;

		glBindBuffer(GL_ARRAY_BUFFER, m_InstanceVBO);

		const GLsizei stride = sizeof(InstanceData);

		// Position (location 2)
		glEnableVertexAttribArray(2);
		glVertexAttribPointer(2, 2, GL_FLOAT, GL_FALSE, stride,
			reinterpret_cast<void*>(offsetof(InstanceData, position)));
		glVertexAttribDivisor(2, 1);

		// UV Offset (location 3)
		glEnableVertexAttribArray(3);
		glVertexAttribPointer(3, 2, GL_FLOAT, GL_FALSE, stride,
			reinterpret_cast<void*>(offsetof(InstanceData, uvOffset)));
		glVertexAttribDivisor(3, 1);

		// UV Size (location 4)
		glEnableVertexAttribArray(4);
		glVertexAttribPointer(4, 2, GL_FLOAT, GL_FALSE, stride,
			reinterpret_cast<void*>(offsetof(InstanceData, uvSize)));
		glVertexAttribDivisor(4, 1);

		spdlog::debug("[InstanceRenderer] Setup vertex attributes for VBO: {}", m_InstanceVBO);
	}

	void InstanceRenderer::CleanupResources() {
		if (m_InstanceVBO != 0) {
			glDeleteBuffers(1, &m_InstanceVBO);
			spdlog::debug("[InstanceRenderer] Deleted VBO: {}", m_InstanceVBO);
			m_InstanceVBO = 0;
		}
		m_AttributesSetup = false;
	}

} // namespace WanderSpire