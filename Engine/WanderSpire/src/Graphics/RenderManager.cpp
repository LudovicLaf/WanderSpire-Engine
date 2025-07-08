#include "WanderSpire/Graphics/RenderManager.h"
#include <algorithm>
#include <spdlog/spdlog.h>

namespace WanderSpire {

	RenderManager& RenderManager::Get() {
		static RenderManager instance;
		return instance;
	}

	void RenderManager::Submit(std::unique_ptr<RenderCommand> command) {
		if (!command) return;
		m_commands.push_back(std::move(command));
	}

	void RenderManager::SubmitSprite(GLuint textureID, const glm::vec2& position,
		const glm::vec2& size, float rotation,
		const glm::vec3& color, const glm::vec2& uvOffset,
		const glm::vec2& uvSize, RenderLayer layer, int order) {
		auto cmd = std::make_unique<SpriteCommand>(layer, order);
		cmd->textureID = textureID;
		cmd->position = position;
		cmd->size = size;
		cmd->rotation = rotation;
		cmd->color = color;
		cmd->uvOffset = uvOffset;
		cmd->uvSize = uvSize;
		Submit(std::move(cmd));
	}

	void RenderManager::SubmitInstanced(GLuint textureID,
		const std::vector<glm::vec2>& positions,
		const std::vector<glm::vec4>& uvRects,
		float tileSize, RenderLayer layer) {
		auto cmd = std::make_unique<InstancedCommand>(layer);
		cmd->textureID = textureID;
		cmd->positions = positions;
		cmd->uvRects = uvRects;
		cmd->tileSize = tileSize;
		Submit(std::move(cmd));
	}

	void RenderManager::SubmitCustom(std::function<void()> callback,
		RenderLayer layer, int order) {
		auto cmd = std::make_unique<CustomCommand>(std::move(callback), layer, order);
		Submit(std::move(cmd));
	}

	void RenderManager::SubmitClear(const glm::vec3& color) {
		auto cmd = std::make_unique<ClearCommand>(color);
		Submit(std::move(cmd));
	}

	void RenderManager::BeginFrame(const glm::mat4& viewProjection) {
		// Clear any previous frame's commands
		Clear();

		// Always start with clear and begin frame
		SubmitClear();
		auto cmd = std::make_unique<BeginFrameCommand>(viewProjection);
		Submit(std::move(cmd));

		m_autoOrder = 0; // Reset auto-ordering
	}

	void RenderManager::EndFrame() {
		auto cmd = std::make_unique<EndFrameCommand>();
		Submit(std::move(cmd));
	}

	void RenderManager::ExecuteFrame() {
		if (m_commands.empty()) return;

		// Sort commands by layer, then by order within layer
		SortCommands();

		// Execute all commands in order
		for (auto& command : m_commands) {
			try {
				command->Execute();
			}
			catch (const std::exception& e) {
				spdlog::error("[RenderManager] Command execution failed: {}", e.what());
			}
		}

		// Clear for next frame
		Clear();
	}

	void RenderManager::Clear() {
		m_commands.clear();
		m_autoOrder = 0;
	}

	void RenderManager::SortCommands() {
		std::sort(m_commands.begin(), m_commands.end(),
			[](const std::unique_ptr<RenderCommand>& a, const std::unique_ptr<RenderCommand>& b) {
				// Sort by layer first
				if (static_cast<int>(a->layer) != static_cast<int>(b->layer)) {
					return static_cast<int>(a->layer) < static_cast<int>(b->layer);
				}
				// Then by order within the same layer
				return a->order < b->order;
			});
	}

} // namespace WanderSpire