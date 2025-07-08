#include "WanderSpire/Editor/Commands/EditorCommandUtils.h"
#include "WanderSpire/Editor/Commands/TransformCommands.h"
#include "WanderSpire/Editor/Commands/HierarchyCommands.h"
#include "WanderSpire/Editor/Commands/CompoundCommand.h"
#include "WanderSpire/Components/TransformComponent.h"

namespace WanderSpire {
	namespace EditorCommands {

		std::unique_ptr<ICommand> CreateMoveCommand(entt::registry& registry,
			const std::vector<entt::entity>& entities,
			const glm::vec2& delta) {
			return std::make_unique<MoveCommand>(registry, entities, delta);
		}

		std::unique_ptr<ICommand> CreateDuplicateCommand(entt::registry& registry,
			const std::vector<entt::entity>& entities,
			const glm::vec2& offset) {
			auto compound = std::make_unique<CompoundCommand>("Duplicate");
			// TODO: Implement entity duplication logic here
			return std::move(compound);
		}

		std::unique_ptr<CompoundCommand> CreateBatchMoveCommand(entt::registry& registry,
			const std::vector<entt::entity>& entities,
			const std::vector<glm::vec2>& positions) {
			auto compound = std::make_unique<CompoundCommand>("Batch Move");
			for (size_t i = 0; i < entities.size() && i < positions.size(); ++i) {
				auto* transform = registry.try_get<TransformComponent>(entities[i]);
				if (transform) {
					glm::vec2 delta = positions[i] - transform->localPosition;
					compound->AddCommand(std::make_unique<MoveCommand>(registry, std::vector<entt::entity>{entities[i]}, delta));
				}
			}
			return compound;
		}

		std::unique_ptr<CompoundCommand> CreateBatchDeleteCommand(entt::registry& registry,
			const std::vector<entt::entity>& entities) {
			auto compound = std::make_unique<CompoundCommand>("Batch Delete");
			compound->AddCommand(std::make_unique<DeleteGameObjectCommand>(registry, entities));
			return compound;
		}

	} // namespace EditorCommands
} // namespace WanderSpire
