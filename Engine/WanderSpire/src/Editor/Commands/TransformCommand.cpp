#include "WanderSpire/Editor/Commands/TransformCommands.h"
#include "WanderSpire/Components/SceneNodeComponent.h"
#include "WanderSpire/Components/TransformComponent.h"

namespace WanderSpire {

	TransformCommand::TransformCommand(entt::registry& registry, entt::entity entity,
		const glm::vec2& newPos, const glm::vec2& newScale, float newRotation)
		: registry(&registry), entity(entity), newPosition(newPos), newScale(newScale), newRotation(newRotation)
	{
		auto* transform = registry.try_get<TransformComponent>(entity);
		if (transform) {
			oldPosition = transform->localPosition;
			oldScale = transform->localScale;
			oldRotation = transform->localRotation;
		}
		auto* node = registry.try_get<SceneNodeComponent>(entity);
		entityName = node ? node->name : "Entity";
	}

	void TransformCommand::Execute() { ApplyTransform(newPosition, newScale, newRotation); }
	void TransformCommand::Undo() { ApplyTransform(oldPosition, oldScale, oldRotation); }

	bool TransformCommand::CanMerge(const ICommand* other) const {
		auto* otherTransform = dynamic_cast<const TransformCommand*>(other);
		return otherTransform && otherTransform->entity == entity;
	}

	void TransformCommand::MergeWith(const ICommand* other) {
		auto* otherTransform = static_cast<const TransformCommand*>(other);
		newPosition = otherTransform->newPosition;
		newScale = otherTransform->newScale;
		newRotation = otherTransform->newRotation;
	}

	void TransformCommand::ApplyTransform(const glm::vec2& pos, const glm::vec2& scale, float rotation) {
		auto* transform = registry->try_get<TransformComponent>(entity);
		if (transform) {
			transform->localPosition = pos;
			transform->localScale = scale;
			transform->localRotation = rotation;
			transform->isDirty = true;
		}
	}

} // namespace WanderSpire
