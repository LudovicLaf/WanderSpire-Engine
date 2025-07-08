#pragma once

#include <glad/glad.h>
#include <glm/glm.hpp>
#include <vector>

namespace WanderSpire {

	class Shader;

	/// RAII wrapper for instanced terrain/tile rendering
	/// Eliminates duplication between GridMap2D and RenderCommand
	class InstanceRenderer {
	public:
		struct InstanceData {
			glm::vec2 position;
			glm::vec2 uvOffset;
			glm::vec2 uvSize;
		};

		InstanceRenderer();
		~InstanceRenderer();

		// Non-copyable, movable
		InstanceRenderer(const InstanceRenderer&) = delete;
		InstanceRenderer& operator=(const InstanceRenderer&) = delete;
		InstanceRenderer(InstanceRenderer&&) noexcept;
		InstanceRenderer& operator=(InstanceRenderer&&) noexcept;

		/// Setup for frame rendering with given shader and VAO
		void BeginFrame(Shader* shader, GLuint quadVAO, GLuint quadEBO);

		/// Render instances with given texture and tile size
		void RenderInstances(GLuint textureID,
			const std::vector<InstanceData>& instances,
			float tileSize);

		/// End frame rendering
		void EndFrame();

		/// Get singleton instance
		static InstanceRenderer& Get();

	private:
		void SetupVertexAttributes();
		void CleanupResources();

		GLuint m_InstanceVBO = 0;
		Shader* m_CurrentShader = nullptr;
		GLuint m_CurrentVAO = 0;
		GLuint m_CurrentEBO = 0;
		bool m_AttributesSetup = false;
	};

} // namespace WanderSpire