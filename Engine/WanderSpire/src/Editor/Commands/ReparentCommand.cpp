#include "WanderSpire/Editor/Commands/HierarchyCommands.h"
#include "WanderSpire/Editor/SceneHierarchyManager.h"
#include "WanderSpire/Components/SceneNodeComponent.h"

namespace WanderSpire {

	ReparentCommand::ReparentCommand(entt::registry& registry, entt::entity child, entt::entity newParent)
		: registry(&registry), childEntity(child), newParent(newParent)
	{
		// Use the reference 'registry', not the member 'registry'
		oldParent = SceneHierarchyManager::GetInstance().GetParent(registry, child);

		auto* node = registry.try_get<SceneNodeComponent>(child);
		childName = node ? node->name : "Entity";
	}

	std::string ReparentCommand::GetDescription() const {
		std::string newParentName = "Root";
		if (newParent != entt::null) {
			auto* node = registry->try_get<SceneNodeComponent>(newParent);
			if (node) newParentName = node->name;
		}
		return "Reparent " + childName + " to " + newParentName;
	}

	void ReparentCommand::Execute() {
		SceneHierarchyManager::GetInstance().SetParent(*registry, childEntity, newParent);
	}

	void ReparentCommand::Undo() {
		SceneHierarchyManager::GetInstance().SetParent(*registry, childEntity, oldParent);
	}

} // namespace WanderSpire
