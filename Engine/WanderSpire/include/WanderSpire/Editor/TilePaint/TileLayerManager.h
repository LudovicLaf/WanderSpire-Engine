// WanderSpire/include/WanderSpire/Editor/TilePaint/TileLayerManager.h - ENHANCED VERSION
#pragma once
#include <entt/entt.hpp>
#include <glm/glm.hpp>
#include <vector>
#include <functional>

namespace WanderSpire {

	/// Layer property types for change notifications
	enum class LayerProperty {
		Visibility,
		Locked,
		Opacity,
		SortOrder,
		Collision,
		Material
	};

	/// Complete layer information structure
	struct LayerInfo {
		entt::entity entity = entt::null;
		std::string name;
		bool visible = true;
		bool locked = false;
		float opacity = 1.0f;
		int sortingOrder = 0;
		bool hasCollision = false;
		std::string materialName;
	};

	/// Clipboard tile data for copy/paste operations
	struct ClipboardTile {
		glm::ivec2 position;
		int tileId = -1;
	};

	/// Professional tile layer management system for the scene editor
	class TileLayerManager {
	public:
		/// Callback type for active layer changes
		using LayerChangedCallback = std::function<void(entt::entity oldLayer, entt::entity newLayer)>;

		/// Callback type for layer property changes
		using PropertyChangedCallback = std::function<void(entt::entity layer, LayerProperty property)>;

		static TileLayerManager& GetInstance();

		// ═════════════════════════════════════════════════════════════════════
		// ACTIVE LAYER MANAGEMENT
		// ═════════════════════════════════════════════════════════════════════

		/// Set the currently active layer for painting operations
		void SetActiveLayer(entt::entity layer);

		/// Get the currently active layer
		entt::entity GetActiveLayer() const;

		/// Check if a layer entity is valid for operations
		bool IsLayerValid(entt::registry& registry, entt::entity layer) const;

		/// Check if a layer is currently visible
		bool IsLayerVisible(entt::registry& registry, entt::entity layer) const;

		/// Check if a layer is locked (read-only)
		bool IsLayerLocked(entt::registry& registry, entt::entity layer) const;

		/// Set layer visibility
		void SetLayerVisible(entt::registry& registry, entt::entity layer, bool visible);

		/// Set layer locked state
		void SetLayerLocked(entt::registry& registry, entt::entity layer, bool locked);

		/// Set layer opacity (0.0 - 1.0)
		void SetLayerOpacity(entt::registry& registry, entt::entity layer, float opacity);

		/// Set layer sorting order
		void SetLayerSortOrder(entt::registry& registry, entt::entity layer, int sortOrder);

		// ═════════════════════════════════════════════════════════════════════
		// MULTI-LAYER PAINTING OPERATIONS
		// ═════════════════════════════════════════════════════════════════════

		/// Paint a tile to all specified layers simultaneously
		void PaintToAllLayers(entt::registry& registry, const std::vector<entt::entity>& layers,
			const glm::ivec2& position, int tileId);

		/// Paint to all currently paintable (visible, unlocked) layers
		void PaintToActiveLayers(entt::registry& registry, const glm::ivec2& position, int tileId);

		// ═════════════════════════════════════════════════════════════════════
		// LAYER COPYING AND REGION OPERATIONS
		// ═════════════════════════════════════════════════════════════════════

		/// Copy a rectangular region from one layer to another
		void CopyLayerRegion(entt::registry& registry, entt::entity srcLayer, entt::entity dstLayer,
			const glm::ivec2& srcMin, const glm::ivec2& srcMax, const glm::ivec2& dstPos);

		/// Copy a region to internal clipboard
		void CopyLayerToClipboard(entt::registry& registry, entt::entity layer,
			const glm::ivec2& min, const glm::ivec2& max);

		/// Paste from internal clipboard to specified position
		void PasteFromClipboard(entt::registry& registry, entt::entity layer,
			const glm::ivec2& position);

		// ═════════════════════════════════════════════════════════════════════
		// LAYER BLENDING AND COMPOSITING
		// ═════════════════════════════════════════════════════════════════════

		/// Blend overlay layer onto base layer with specified opacity
		void BlendLayers(entt::registry& registry, entt::entity baseLayer, entt::entity overlayLayer,
			const glm::ivec2& min, const glm::ivec2& max, float opacity = 1.0f);

		/// Merge multiple source layers into a target layer
		void MergeLayers(entt::registry& registry, entt::entity targetLayer,
			const std::vector<entt::entity>& sourceLayers, const glm::ivec2& min, const glm::ivec2& max);

		// ═════════════════════════════════════════════════════════════════════
		// LAYER ANALYSIS AND UTILITIES
		// ═════════════════════════════════════════════════════════════════════

		/// Get all layers that can be painted to (visible and unlocked)
		std::vector<entt::entity> GetPaintableLayers(entt::registry& registry) const;

		/// Get all layer entities sorted by render order
		std::vector<entt::entity> GetAllLayers(entt::registry& registry) const;

		/// Get all layers belonging to a specific tilemap
		std::vector<entt::entity> GetLayersInTilemap(entt::registry& registry, entt::entity tilemap) const;

		/// Get complete information about a layer
		LayerInfo GetLayerInfo(entt::registry& registry, entt::entity layer) const;

		/// Check if a tile position is within valid bounds for the layer
		bool IsPositionInBounds(entt::registry& registry, entt::entity layer,
			const glm::ivec2& position) const;

		// ═════════════════════════════════════════════════════════════════════
		// CALLBACK SYSTEM
		// ═════════════════════════════════════════════════════════════════════

		/// Register callback for active layer changes
		void RegisterLayerChangedCallback(LayerChangedCallback callback);

		/// Register callback for layer property changes
		void RegisterPropertyChangedCallback(PropertyChangedCallback callback);

	private:
		entt::entity activeLayer = entt::null;

		// Clipboard data for copy/paste operations
		std::vector<ClipboardTile> clipboardData;
		glm::ivec2 clipboardSize{ 0, 0 };

		// Callback lists
		std::vector<LayerChangedCallback> layerChangedCallbacks;
		std::vector<PropertyChangedCallback> propertyChangedCallbacks;

		// ═════════════════════════════════════════════════════════════════════
		// PRIVATE HELPER METHODS
		// ═════════════════════════════════════════════════════════════════════

		/// Blend two tile IDs with specified opacity
		int BlendTiles(int baseTile, int overlayTile, float opacity) const;

		/// Merge two tile IDs (overlay takes precedence)
		int MergeTiles(int baseTile, int overlayTile) const;

		/// Notify callbacks about active layer changes
		void NotifyActiveLayerChanged(entt::entity oldLayer, entt::entity newLayer);

		/// Notify callbacks about layer property changes
		void NotifyLayerPropertyChanged(entt::entity layer, LayerProperty property);
	};

} // namespace WanderSpire