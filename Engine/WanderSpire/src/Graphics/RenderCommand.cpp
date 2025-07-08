#include "WanderSpire/Graphics/RenderCommand.h"
#include "WanderSpire/Graphics/SpriteRenderer.h"
#include "WanderSpire/Graphics/RenderResourceManager.h"
#include "WanderSpire/Graphics/InstanceRenderer.h"
#include <spdlog/spdlog.h>

namespace WanderSpire {

	void SpriteCommand::Execute() {
		auto& renderer = SpriteRenderer::Get();
		renderer.DrawSprite(textureID, position, size, rotation, color, uvOffset, uvSize);
	}

	void InstancedCommand::Execute() {
		if (positions.empty() || uvRects.empty()) return;

		auto* shader = RenderResourceManager::Get().GetShader("sprite");
		if (!shader || !shader->GetID()) return;

		auto& rm = RenderResourceManager::Get();
		GLuint quadVAO = rm.GetQuadVAO();
		GLuint quadEBO = rm.GetQuadEBO();
		if (quadVAO == 0 || quadEBO == 0) return;

		// Ensure we have the same number of positions and UV rects
		size_t count = std::min(positions.size(), uvRects.size());
		if (count == 0) return;

		// Convert to InstanceRenderer format
		std::vector<InstanceRenderer::InstanceData> instances;
		instances.reserve(count);

		for (size_t i = 0; i < count; ++i) {
			InstanceRenderer::InstanceData data;
			data.position = positions[i];
			data.uvOffset = glm::vec2(uvRects[i].x, uvRects[i].y);
			data.uvSize = glm::vec2(uvRects[i].z, uvRects[i].w);
			instances.push_back(data);
		}

		// Use InstanceRenderer for consistent rendering
		auto& instanceRenderer = InstanceRenderer::Get();
		instanceRenderer.BeginFrame(shader, quadVAO, quadEBO);
		instanceRenderer.RenderInstances(textureID, instances, tileSize);
		instanceRenderer.EndFrame();
	}

	void BeginFrameCommand::Execute() {
		auto& renderer = SpriteRenderer::Get();
		renderer.BeginFrame(viewProjection);
	}

	void EndFrameCommand::Execute() {
		auto& renderer = SpriteRenderer::Get();
		renderer.EndFrame();
	}

} // namespace WanderSpire