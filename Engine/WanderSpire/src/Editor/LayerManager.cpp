#include "WanderSpire/Editor/LayerManager.h"
#include <algorithm>
#include <spdlog/spdlog.h>

namespace WanderSpire {

	LayerManager& LayerManager::GetInstance() {
		static LayerManager instance;
		if (instance.layers.empty()) {
			instance.InitializeDefaultLayers();
		}
		return instance;
	}

	int LayerManager::CreateLayer(const std::string& name) {
		Layer layer;
		layer.id = nextLayerId++;
		layer.name = name;
		layer.visible = true;
		layer.locked = false;
		layer.sortOrder = static_cast<int>(layers.size()) * 10; // Space layers by 10
		layer.color = glm::vec4(1.0f, 1.0f, 1.0f, 1.0f);

		layers[layer.id] = layer;

		spdlog::info("[LayerManager] Created layer '{}' with ID {}", name, layer.id);
		return layer.id;
	}

	void LayerManager::RemoveLayer(int layerId) {
		if (layerId == DEFAULT_LAYER) {
			spdlog::warn("[LayerManager] Cannot remove default layer");
			return;
		}

		auto it = layers.find(layerId);
		if (it != layers.end()) {
			spdlog::info("[LayerManager] Removed layer '{}' (ID: {})", it->second.name, layerId);
			layers.erase(it);

			// Note: Entities on this layer should be moved to default layer
			// This is handled by the calling code (Command system)
		}
	}

	void LayerManager::RenameLayer(int layerId, const std::string& newName) {
		auto it = layers.find(layerId);
		if (it != layers.end()) {
			std::string oldName = it->second.name;
			it->second.name = newName;
			spdlog::info("[LayerManager] Renamed layer '{}' to '{}'", oldName, newName);
		}
	}

	void LayerManager::SetLayerVisible(int layerId, bool visible) {
		auto it = layers.find(layerId);
		if (it != layers.end()) {
			it->second.visible = visible;
			spdlog::debug("[LayerManager] Set layer '{}' visibility to {}", it->second.name, visible);
		}
	}

	void LayerManager::SetLayerLocked(int layerId, bool locked) {
		auto it = layers.find(layerId);
		if (it != layers.end()) {
			it->second.locked = locked;
			spdlog::debug("[LayerManager] Set layer '{}' locked to {}", it->second.name, locked);
		}
	}

	void LayerManager::SetLayerSortOrder(int layerId, int sortOrder) {
		auto it = layers.find(layerId);
		if (it != layers.end()) {
			it->second.sortOrder = sortOrder;
			spdlog::debug("[LayerManager] Set layer '{}' sort order to {}", it->second.name, sortOrder);
		}
	}

	void LayerManager::SetLayerColor(int layerId, const glm::vec4& color) {
		auto it = layers.find(layerId);
		if (it != layers.end()) {
			it->second.color = color;
			spdlog::debug("[LayerManager] Set layer '{}' color", it->second.name);
		}
	}

	const LayerManager::Layer* LayerManager::GetLayer(int layerId) const {
		auto it = layers.find(layerId);
		return (it != layers.end()) ? &it->second : nullptr;
	}

	std::vector<LayerManager::Layer> LayerManager::GetAllLayers() const {
		std::vector<Layer> result;
		result.reserve(layers.size());

		for (const auto& pair : layers) {
			result.push_back(pair.second);
		}

		return result;
	}

	std::vector<LayerManager::Layer> LayerManager::GetSortedLayers() const {
		auto result = GetAllLayers();

		std::sort(result.begin(), result.end(),
			[](const Layer& a, const Layer& b) {
				return a.sortOrder < b.sortOrder;
			});

		return result;
	}

	void LayerManager::InitializeDefaultLayers() {
		// Create built-in layers
		Layer background;
		background.id = BACKGROUND_LAYER;
		background.name = "Background";
		background.sortOrder = BACKGROUND_LAYER;
		background.color = glm::vec4(0.2f, 0.2f, 0.8f, 1.0f); // Blue
		layers[BACKGROUND_LAYER] = background;

		Layer defaultLayer;
		defaultLayer.id = DEFAULT_LAYER;
		defaultLayer.name = "Default";
		defaultLayer.sortOrder = DEFAULT_LAYER;
		defaultLayer.color = glm::vec4(1.0f, 1.0f, 1.0f, 1.0f); // White
		layers[DEFAULT_LAYER] = defaultLayer;

		Layer foreground;
		foreground.id = FOREGROUND_LAYER;
		foreground.name = "Foreground";
		foreground.sortOrder = FOREGROUND_LAYER;
		foreground.color = glm::vec4(0.8f, 0.8f, 0.2f, 1.0f); // Yellow
		layers[FOREGROUND_LAYER] = foreground;

		Layer ui;
		ui.id = UI_LAYER;
		ui.name = "UI";
		ui.sortOrder = UI_LAYER;
		ui.color = glm::vec4(0.8f, 0.2f, 0.2f, 1.0f); // Red
		layers[UI_LAYER] = ui;

		defaultLayerId = DEFAULT_LAYER;
		nextLayerId = UI_LAYER + 1;

		spdlog::info("[LayerManager] Initialized default layers");
	}

} // namespace WanderSpire