#include "WanderSpire/Editor/SelectionManager.h"
#include "WanderSpire/Components/SelectableComponent.h"
#include "WanderSpire/Components/TransformComponent.h"
#include "WanderSpire/Components/SceneNodeComponent.h"
#include <algorithm>
#include <limits>
#include <spdlog/spdlog.h>
#define GLM_ENABLE_EXPERIMENTAL
#include <glm/gtx/norm.hpp>

namespace WanderSpire {

	SelectionManager& SelectionManager::GetInstance() {
		static SelectionManager instance;
		return instance;
	}

	void SelectionManager::SelectEntity(entt::registry& registry, entt::entity entity) {
		if (!registry.valid(entity)) {
			return;
		}

		// Clear current selection
		DeselectAll(registry);

		// Add to selection
		selectedEntities.insert(entity);
		primarySelection = entity;

		UpdateSelectionComponents(registry);
		NotifySelectionChanged();

		spdlog::debug("[Selection] Selected entity {}", entt::to_integral(entity));
	}

	void SelectionManager::DeselectEntity(entt::registry& registry, entt::entity entity) {
		auto it = selectedEntities.find(entity);
		if (it != selectedEntities.end()) {
			selectedEntities.erase(it);

			// Update primary selection
			if (primarySelection == entity) {
				primarySelection = selectedEntities.empty() ? entt::null : *selectedEntities.begin();
			}

			UpdateSelectionComponents(registry);
			NotifySelectionChanged();

			spdlog::debug("[Selection] Deselected entity {}", entt::to_integral(entity));
		}
	}

	void SelectionManager::ToggleSelection(entt::registry& registry, entt::entity entity) {
		if (IsSelected(entity)) {
			DeselectEntity(registry, entity);
		}
		else {
			AddToSelection(registry, entity);
		}
	}

	void SelectionManager::SelectAll(entt::registry& registry) {
		selectedEntities.clear();

		auto view = registry.view<SelectableComponent>();
		for (auto entity : view) {
			const auto& selectable = view.get<SelectableComponent>(entity);
			if (selectable.selectable) {
				selectedEntities.insert(entity);
			}
		}

		primarySelection = selectedEntities.empty() ? entt::null : *selectedEntities.begin();

		UpdateSelectionComponents(registry);
		NotifySelectionChanged();

		spdlog::debug("[Selection] Selected all entities ({})", selectedEntities.size());
	}

	void SelectionManager::DeselectAll(entt::registry& registry) {
		if (selectedEntities.empty()) {
			return;
		}

		selectedEntities.clear();
		primarySelection = entt::null;

		UpdateSelectionComponents(registry);
		NotifySelectionChanged();

		spdlog::debug("[Selection] Deselected all entities");
	}

	void SelectionManager::AddToSelection(entt::registry& registry, entt::entity entity) {
		if (!registry.valid(entity)) {
			return;
		}

		auto* selectable = registry.try_get<SelectableComponent>(entity);
		if (!selectable || !selectable->selectable) {
			return;
		}

		selectedEntities.insert(entity);

		if (primarySelection == entt::null) {
			primarySelection = entity;
		}

		UpdateSelectionComponents(registry);
		NotifySelectionChanged();

		spdlog::debug("[Selection] Added entity {} to selection", entt::to_integral(entity));
	}

	void SelectionManager::RemoveFromSelection(entt::registry& registry, entt::entity entity) {
		DeselectEntity(registry, entity);
	}

	void SelectionManager::SetSelection(entt::registry& registry, const std::vector<entt::entity>& entities) {
		selectedEntities.clear();

		for (entt::entity entity : entities) {
			if (registry.valid(entity)) {
				auto* selectable = registry.try_get<SelectableComponent>(entity);
				if (selectable && selectable->selectable) {
					selectedEntities.insert(entity);
				}
			}
		}

		primarySelection = selectedEntities.empty() ? entt::null : *selectedEntities.begin();

		UpdateSelectionComponents(registry);
		NotifySelectionChanged();

		spdlog::debug("[Selection] Set selection to {} entities", selectedEntities.size());
	}

	void SelectionManager::SelectInBounds(entt::registry& registry, const glm::vec2& min, const glm::vec2& max) {
		std::vector<entt::entity> entitiesInBounds;

		auto view = registry.view<SelectableComponent, TransformComponent>();
		for (auto entity : view) {
			const auto& selectable = view.get<SelectableComponent>(entity);
			const auto& transform = view.get<TransformComponent>(entity);

			if (!selectable.selectable) {
				continue;
			}

			// Check if entity bounds intersect with selection bounds
			glm::vec2 entityMin = transform.worldPosition + selectable.boundsMin;
			glm::vec2 entityMax = transform.worldPosition + selectable.boundsMax;

			if (entityMax.x >= min.x && entityMin.x <= max.x &&
				entityMax.y >= min.y && entityMin.y <= max.y) {
				entitiesInBounds.push_back(entity);
			}
		}

		SetSelection(registry, entitiesInBounds);
		spdlog::debug("[Selection] Selected {} entities in bounds", entitiesInBounds.size());
	}

	void SelectionManager::SelectInCircle(entt::registry& registry, const glm::vec2& center, float radius) {
		std::vector<entt::entity> entitiesInCircle;
		float radiusSquared = radius * radius;

		auto view = registry.view<SelectableComponent, TransformComponent>();
		for (auto entity : view) {
			const auto& selectable = view.get<SelectableComponent>(entity);
			const auto& transform = view.get<TransformComponent>(entity);

			if (!selectable.selectable) {
				continue;
			}

			// Check distance from center to entity center
			float distanceSquared = glm::distance2(transform.worldPosition, center);
			if (distanceSquared <= radiusSquared) {
				entitiesInCircle.push_back(entity);
			}
		}

		SetSelection(registry, entitiesInCircle);
		spdlog::debug("[Selection] Selected {} entities in circle", entitiesInCircle.size());
	}

	bool SelectionManager::IsSelected(entt::entity entity) const {
		return selectedEntities.find(entity) != selectedEntities.end();
	}

	int SelectionManager::GetSelectionCount() const {
		return static_cast<int>(selectedEntities.size());
	}

	const std::unordered_set<entt::entity>& SelectionManager::GetSelectedEntities() const {
		return selectedEntities;
	}

	bool SelectionManager::GetSelectionBounds(entt::registry& registry, glm::vec2& outMin, glm::vec2& outMax) const {
		if (selectedEntities.empty()) {
			return false;
		}

		outMin = glm::vec2(std::numeric_limits<float>::max());
		outMax = glm::vec2(std::numeric_limits<float>::lowest());

		bool hasValidBounds = false;

		for (entt::entity entity : selectedEntities) {
			auto* transform = registry.try_get<TransformComponent>(entity);
			auto* selectable = registry.try_get<SelectableComponent>(entity);

			if (transform && selectable) {
				glm::vec2 entityMin = transform->worldPosition + selectable->boundsMin;
				glm::vec2 entityMax = transform->worldPosition + selectable->boundsMax;

				outMin = glm::min(outMin, entityMin);
				outMax = glm::max(outMax, entityMax);
				hasValidBounds = true;
			}
		}

		return hasValidBounds;
	}

	glm::vec2 SelectionManager::GetSelectionCenter(entt::registry& registry) const {
		glm::vec2 min, max;
		if (GetSelectionBounds(registry, min, max)) {
			return (min + max) * 0.5f;
		}
		return glm::vec2(0.0f);
	}

	void SelectionManager::RegisterSelectionChangedCallback(SelectionCallback callback) {
		selectionChangedCallbacks.push_back(std::move(callback));
	}

	void SelectionManager::NotifySelectionChanged() {
		for (auto& callback : selectionChangedCallbacks) {
			callback(selectedEntities);
		}
	}

	void SelectionManager::UpdateSelectionComponents(entt::registry& registry) {
		// Update SelectableComponent::selected for all entities
		auto view = registry.view<SelectableComponent>();
		for (auto entity : view) {
			auto& selectable = view.get<SelectableComponent>(entity);
			selectable.selected = IsSelected(entity);
		}
	}

} // namespace WanderSpire