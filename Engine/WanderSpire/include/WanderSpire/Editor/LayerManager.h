#pragma once
#include <unordered_map>
#include <vector>
#include <string>
#include <glm/glm.hpp>

namespace WanderSpire {

	class LayerManager {
	public:
		struct Layer {
			int id;
			std::string name;
			bool visible = true;
			bool locked = false;
			int sortOrder = 0;
			glm::vec4 color{ 1.0f, 1.0f, 1.0f, 1.0f };
		};

		static LayerManager& GetInstance();

		int CreateLayer(const std::string& name);
		void RemoveLayer(int layerId);
		void RenameLayer(int layerId, const std::string& newName);

		void SetLayerVisible(int layerId, bool visible);
		void SetLayerLocked(int layerId, bool locked);
		void SetLayerSortOrder(int layerId, int sortOrder);
		void SetLayerColor(int layerId, const glm::vec4& color);

		const Layer* GetLayer(int layerId) const;
		std::vector<Layer> GetAllLayers() const;
		std::vector<Layer> GetSortedLayers() const;

		int GetDefaultLayer() const;
		void SetDefaultLayer(int layerId);

		static constexpr int BACKGROUND_LAYER = -1000;
		static constexpr int DEFAULT_LAYER = 0;
		static constexpr int FOREGROUND_LAYER = 1000;
		static constexpr int UI_LAYER = 2000;

	private:
		std::unordered_map<int, Layer> layers;
		int nextLayerId = 1;
		int defaultLayerId = DEFAULT_LAYER;

		void InitializeDefaultLayers();
	};

} // namespace WanderSpire
