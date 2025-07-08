#pragma once
#include <vector>
#include <string>
#include <functional>
#include <glm/glm.hpp>
#include <entt/entt.hpp>

namespace WanderSpire {

	class SceneHierarchyManager {
	public:
		static SceneHierarchyManager& GetInstance();

		entt::entity CreateGameObject(entt::registry& registry, const std::string& name = "GameObject");
		void SetParent(entt::registry& registry, entt::entity child, entt::entity parent);
		void RemoveParent(entt::registry& registry, entt::entity child);
		void DestroyGameObject(entt::registry& registry, entt::entity entity);

		std::vector<entt::entity> GetChildren(entt::registry& registry, entt::entity parent);
		std::vector<entt::entity> GetRootObjects(entt::registry& registry);
		entt::entity GetParent(entt::registry& registry, entt::entity child);
		bool IsDescendantOf(entt::registry& registry, entt::entity descendant, entt::entity ancestor);

		void UpdateWorldTransforms(entt::registry& registry);
		glm::mat4 GetWorldMatrix(entt::registry& registry, entt::entity entity);

		using HierarchyCallback = std::function<void(entt::entity, entt::entity)>;
		void RegisterParentChangedCallback(HierarchyCallback callback);

	private:
		std::vector<HierarchyCallback> parentChangedCallbacks;
		void UpdateWorldTransformRecursive(entt::registry& registry, entt::entity entity, const glm::mat4& parentMatrix);
		void NotifyParentChanged(entt::entity child, entt::entity parent);
	};

} // namespace WanderSpire
