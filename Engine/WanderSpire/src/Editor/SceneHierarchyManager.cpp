#include "WanderSpire/Editor/SceneHierarchyManager.h"
#include "WanderSpire/Components/SceneNodeComponent.h"
#include "WanderSpire/Components/TransformComponent.h"
#include <glm/gtc/matrix_transform.hpp>
#include <algorithm>

namespace WanderSpire {

	SceneHierarchyManager& SceneHierarchyManager::GetInstance() {
		static SceneHierarchyManager instance;
		return instance;
	}

	entt::entity SceneHierarchyManager::CreateGameObject(entt::registry& registry, const std::string& name) {
		entt::entity entity = registry.create();

		registry.emplace<SceneNodeComponent>(entity, SceneNodeComponent{ .name = name });
		registry.emplace<TransformComponent>(entity);

		return entity;
	}

	void SceneHierarchyManager::SetParent(entt::registry& registry, entt::entity child, entt::entity parent) {
		if (!registry.valid(child) || (parent != entt::null && !registry.valid(parent))) {
			return;
		}

		// Prevent circular dependencies
		if (parent != entt::null && IsDescendantOf(registry, parent, child)) {
			return;
		}

		auto* childNode = registry.try_get<SceneNodeComponent>(child);
		if (!childNode) return;

		// Remove from old parent
		if (childNode->parent != entt::null) {
			RemoveParent(registry, child);
		}

		// Set new parent
		childNode->parent = parent;

		if (parent != entt::null) {
			if (auto* parentNode = registry.try_get<SceneNodeComponent>(parent)) {
				parentNode->children.push_back(child);
			}
		}

		// Mark transforms as dirty
		if (auto* transform = registry.try_get<TransformComponent>(child)) {
			transform->isDirty = true;
		}

		NotifyParentChanged(child, parent);
	}

	void SceneHierarchyManager::RemoveParent(entt::registry& registry, entt::entity child) {
		auto* childNode = registry.try_get<SceneNodeComponent>(child);
		if (!childNode || childNode->parent == entt::null) return;

		entt::entity oldParent = childNode->parent;
		if (auto* parentNode = registry.try_get<SceneNodeComponent>(oldParent)) {
			auto& children = parentNode->children;
			children.erase(std::remove(children.begin(), children.end(), child), children.end());
		}

		childNode->parent = entt::null;

		if (auto* transform = registry.try_get<TransformComponent>(child)) {
			transform->isDirty = true;
		}

		NotifyParentChanged(child, entt::null);
	}

	void SceneHierarchyManager::DestroyGameObject(entt::registry& registry, entt::entity entity) {
		if (!registry.valid(entity)) return;

		std::vector<entt::entity> children = GetChildren(registry, entity);
		RemoveParent(registry, entity);

		// Recursively destroy children
		for (entt::entity child : children) {
			DestroyGameObject(registry, child);
		}

		registry.destroy(entity);
	}

	std::vector<entt::entity> SceneHierarchyManager::GetChildren(entt::registry& registry, entt::entity parent) {
		auto* parentNode = registry.try_get<SceneNodeComponent>(parent);
		if (!parentNode) return {};

		// Clean up invalid children
		auto& children = parentNode->children;
		children.erase(
			std::remove_if(children.begin(), children.end(),
				[&registry](entt::entity child) { return !registry.valid(child); }),
			children.end()
		);

		return children;
	}

	std::vector<entt::entity> SceneHierarchyManager::GetRootObjects(entt::registry& registry) {
		std::vector<entt::entity> roots;

		for (auto entity : registry.view<SceneNodeComponent>()) {
			const auto& node = registry.get<SceneNodeComponent>(entity);
			if (node.parent == entt::null) {
				roots.push_back(entity);
			}
		}

		return roots;
	}

	entt::entity SceneHierarchyManager::GetParent(entt::registry& registry, entt::entity child) {
		auto* childNode = registry.try_get<SceneNodeComponent>(child);
		return childNode ? childNode->parent : entt::null;
	}

	bool SceneHierarchyManager::IsDescendantOf(entt::registry& registry, entt::entity descendant, entt::entity ancestor) {
		if (descendant == ancestor) return true;

		entt::entity current = descendant;
		while (current != entt::null) {
			auto* node = registry.try_get<SceneNodeComponent>(current);
			if (!node) break;

			current = node->parent;
			if (current == ancestor) return true;
		}

		return false;
	}

	void SceneHierarchyManager::UpdateWorldTransforms(entt::registry& registry) {
		auto roots = GetRootObjects(registry);
		for (entt::entity root : roots) {
			UpdateWorldTransformRecursive(registry, root, glm::mat4(1.0f));
		}
	}

	glm::mat4 SceneHierarchyManager::GetWorldMatrix(entt::registry& registry, entt::entity entity) {
		auto* node = registry.try_get<SceneNodeComponent>(entity);
		if (!node) return glm::mat4(1.0f);

		if (node->worldMatrixDirty) {
			glm::mat4 parentMatrix = glm::mat4(1.0f);
			if (node->parent != entt::null) {
				parentMatrix = GetWorldMatrix(registry, node->parent);
			}

			if (auto* transform = registry.try_get<TransformComponent>(entity)) {
				glm::mat4 local = glm::mat4(1.0f);
				local = glm::translate(local, glm::vec3(transform->localPosition, 0.0f));
				local = glm::rotate(local, transform->localRotation, glm::vec3(0, 0, 1));
				local = glm::scale(local, glm::vec3(transform->localScale, 1.0f));
				node->worldMatrix = parentMatrix * local;
			}
			else {
				node->worldMatrix = parentMatrix;
			}

			node->worldMatrixDirty = false;
		}

		return node->worldMatrix;
	}

	void SceneHierarchyManager::RegisterParentChangedCallback(HierarchyCallback callback) {
		parentChangedCallbacks.push_back(std::move(callback));
	}

	void SceneHierarchyManager::UpdateWorldTransformRecursive(entt::registry& registry, entt::entity entity, const glm::mat4& parentMatrix) {
		auto* node = registry.try_get<SceneNodeComponent>(entity);
		auto* transform = registry.try_get<TransformComponent>(entity);

		if (!node || !transform) return;

		// Calculate local matrix
		glm::mat4 localMatrix = glm::mat4(1.0f);
		localMatrix = glm::translate(localMatrix, glm::vec3(transform->localPosition, 0.0f));
		localMatrix = glm::rotate(localMatrix, transform->localRotation, glm::vec3(0, 0, 1));
		localMatrix = glm::scale(localMatrix, glm::vec3(transform->localScale, 1.0f));

		// Calculate world matrix
		node->worldMatrix = parentMatrix * localMatrix;
		node->worldMatrixDirty = false;

		// Update world transform cache
		glm::vec2 worldPos = glm::vec2(node->worldMatrix[3]);
		transform->worldPosition = worldPos;
		transform->worldRotation = transform->localRotation;
		transform->worldScale = transform->localScale;
		transform->isDirty = false;

		// Recursively update children
		for (entt::entity child : node->children) {
			UpdateWorldTransformRecursive(registry, child, node->worldMatrix);
		}
	}

	void SceneHierarchyManager::NotifyParentChanged(entt::entity child, entt::entity parent) {
		for (auto& callback : parentChangedCallbacks) {
			callback(child, parent);
		}
	}

} // namespace WanderSpire