#pragma once

#include "RenderCommand.h"
#include <vector>
#include <memory>
#include <functional>

namespace WanderSpire {

	/// Central rendering coordinator - queues and executes render commands in order
	class RenderManager {
	public:
		static RenderManager& Get();

		/// Queue a render command for execution this frame
		void Submit(std::unique_ptr<RenderCommand> command);

		/// Submit a sprite for rendering
		void SubmitSprite(GLuint textureID, const glm::vec2& position, const glm::vec2& size,
			float rotation, const glm::vec3& color, const glm::vec2& uvOffset,
			const glm::vec2& uvSize, RenderLayer layer, int order = 0);

		/// Submit instanced terrain/tiles
		void SubmitInstanced(GLuint textureID, const std::vector<glm::vec2>& positions,
			const std::vector<glm::vec4>& uvRects, float tileSize,
			RenderLayer layer = RenderLayer::Terrain);

		/// Submit a custom render callback
		void SubmitCustom(std::function<void()> callback, RenderLayer layer, int order = 0);

		/// Clear screen with color
		void SubmitClear(const glm::vec3& color = { 0.2f, 0.3f, 0.3f });

		/// Setup frame rendering state
		void BeginFrame(const glm::mat4& viewProjection);

		/// Finalize frame
		void EndFrame();

		/// Execute all queued commands and clear the queue
		void ExecuteFrame();

		/// Clear all pending commands without executing
		void Clear();

		/// Get total number of commands queued
		size_t GetCommandCount() const { return m_commands.size(); }

		/// Set default render order increment for auto-ordering
		void SetOrderIncrement(int increment) { m_orderIncrement = increment; }

	private:
		RenderManager() = default;

		std::vector<std::unique_ptr<RenderCommand>> m_commands;
		int m_orderIncrement = 1;
		int m_autoOrder = 0; ///< Auto-incrementing order for convenience

		/// Sort commands by layer, then by order within layer
		void SortCommands();
	};

	/// RAII helper for frame rendering scope
	class FrameScope {
	public:
		FrameScope(const glm::mat4& viewProjection) {
			RenderManager::Get().BeginFrame(viewProjection);
		}

		~FrameScope() {
			RenderManager::Get().EndFrame();
			RenderManager::Get().ExecuteFrame();
		}

		// Non-copyable
		FrameScope(const FrameScope&) = delete;
		FrameScope& operator=(const FrameScope&) = delete;
	};

} // namespace WanderSpire