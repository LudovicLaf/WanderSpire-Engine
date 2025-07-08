#include "WanderSpire/Editor/Commands/TransformCommands.h"
#include "WanderSpire/Components/SceneNodeComponent.h"
#include "WanderSpire/Components/TransformComponent.h"
#include <chrono>

namespace WanderSpire {

	MoveCommand::MoveCommand(entt::registry& registry, const std::vector<entt::entity>& entities, const glm::vec2& delta)
		: registry(&registry), entities(entities), totalDelta(delta), commandTime(std::chrono::steady_clock::now()) {
	}

	void MoveCommand::Execute() {
		for (entt::entity entity : entities) {
			auto* transform = registry->try_get<TransformComponent>(entity);
			if (transform) {
				transform->localPosition += totalDelta;
				transform->isDirty = true;
			}
		}
	}

	void MoveCommand::Undo() {
		for (entt::entity entity : entities) {
			auto* transform = registry->try_get<TransformComponent>(entity);
			if (transform) {
				transform->localPosition -= totalDelta;
				transform->isDirty = true;
			}
		}
	}

	std::string MoveCommand::GetDescription() const {
		if (entities.size() == 1) {
			auto* node = registry->try_get<SceneNodeComponent>(entities[0]);
			std::string name = node ? node->name : "Entity";
			return "Move " + name;
		}
		else {
			return "Move " + std::to_string(entities.size()) + " entities";
		}
	}

	bool MoveCommand::CanMerge(const ICommand* other) const {
		auto* otherMove = dynamic_cast<const MoveCommand*>(other);
		if (!otherMove || otherMove->entities.size() != entities.size())
			return false;
		for (size_t i = 0; i < entities.size(); ++i)
			if (entities[i] != otherMove->entities[i])
				return false;
		auto timeDiff = std::chrono::duration_cast<std::chrono::milliseconds>(
			otherMove->commandTime - commandTime).count();
		return timeDiff < (MERGE_TIME_THRESHOLD * 1000.0f);
	}

	void MoveCommand::MergeWith(const ICommand* other) {
		auto* otherMove = static_cast<const MoveCommand*>(other);
		totalDelta += otherMove->totalDelta;
		commandTime = otherMove->commandTime;
	}

} // namespace WanderSpire
