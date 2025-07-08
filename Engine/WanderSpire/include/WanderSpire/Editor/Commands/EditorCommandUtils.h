#pragma once
#include "CompoundCommand.h"
#include <memory>
#include <glm/glm.hpp>
#include <vector>
#include <nlohmann/json.hpp>
#include <entt/entt.hpp>

namespace WanderSpire {
	namespace EditorCommands {

		std::unique_ptr<ICommand> CreateMoveCommand(entt::registry& registry,
			const std::vector<entt::entity>& entities,
			const glm::vec2& delta);

		std::unique_ptr<ICommand> CreateDuplicateCommand(entt::registry& registry,
			const std::vector<entt::entity>& entities,
			const glm::vec2& offset);

		std::unique_ptr<ICommand> CreatePasteCommand(entt::registry& registry,
			const nlohmann::json& clipboardData,
			const glm::vec2& position);

		std::unique_ptr<CompoundCommand> CreateBatchMoveCommand(entt::registry& registry,
			const std::vector<entt::entity>& entities,
			const std::vector<glm::vec2>& positions);

		std::unique_ptr<CompoundCommand> CreateBatchDeleteCommand(entt::registry& registry,
			const std::vector<entt::entity>& entities);

	} // namespace EditorCommands
} // namespace WanderSpire
