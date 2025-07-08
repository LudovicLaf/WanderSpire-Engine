#pragma once
#include <unordered_set>
#include <vector>
#include <functional>
#include <glm/glm.hpp>
#include <entt/entt.hpp>

namespace WanderSpire {

	class SelectionManager {
	public:
		static SelectionManager& GetInstance();

		void SelectEntity(entt::registry& registry, entt::entity entity);
		void DeselectEntity(entt::registry& registry, entt::entity entity);
		void ToggleSelection(entt::registry& registry, entt::entity entity);
		void SelectAll(entt::registry& registry);
		void DeselectAll(entt::registry& registry);

		void AddToSelection(entt::registry& registry, entt::entity entity);
		void RemoveFromSelection(entt::registry& registry, entt::entity entity);
		void SetSelection(entt::registry& registry, const std::vector<entt::entity>& entities);

		void SelectInBounds(entt::registry& registry, const glm::vec2& min, const glm::vec2& max);
		void SelectInCircle(entt::registry& registry, const glm::vec2& center, float radius);

		const std::unordered_set<entt::entity>& GetSelectedEntities() const;
		bool IsSelected(entt::entity entity) const;
		int GetSelectionCount() const;
		entt::entity GetPrimarySelection() const;

		bool GetSelectionBounds(entt::registry& registry, glm::vec2& outMin, glm::vec2& outMax) const;
		glm::vec2 GetSelectionCenter(entt::registry& registry) const;

		using SelectionCallback = std::function<void(const std::unordered_set<entt::entity>&)>;
		void RegisterSelectionChangedCallback(SelectionCallback callback);

	private:
		std::unordered_set<entt::entity> selectedEntities;
		entt::entity primarySelection = entt::null;
		std::vector<SelectionCallback> selectionChangedCallbacks;

		void NotifySelectionChanged();
		void UpdateSelectionComponents(entt::registry& registry);
	};

} // namespace WanderSpire
