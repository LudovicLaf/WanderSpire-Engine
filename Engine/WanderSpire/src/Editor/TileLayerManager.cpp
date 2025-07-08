#include "WanderSpire/Editor/TilePaint/TileLayerManager.h"
#include "WanderSpire/World/TilemapSystem.h"
#include "WanderSpire/Components/TilemapLayerComponent.h"
#include "WanderSpire/Components/SceneNodeComponent.h"
#include "WanderSpire/Editor/Commands/TilemapCommands.h"
#include "WanderSpire/Editor/CommandHistory.h"

#include <spdlog/spdlog.h>
#include <algorithm>
#include <cmath>
#include <WanderSpire/Editor/EditorGlobals.h>

namespace WanderSpire {

	TileLayerManager& TileLayerManager::GetInstance() {
		static TileLayerManager instance;
		return instance;
	}

	// ═════════════════════════════════════════════════════════════════════
	// ACTIVE LAYER MANAGEMENT
	// ═════════════════════════════════════════════════════════════════════

	void TileLayerManager::SetActiveLayer(entt::entity layer) {
		if (activeLayer != layer) {
			entt::entity previousLayer = activeLayer;
			activeLayer = layer;

			// Notify callbacks about layer change
			NotifyActiveLayerChanged(previousLayer, layer);

			spdlog::debug("[TileLayerManager] Set active layer to entity {}",
				layer == entt::null ? 0 : entt::to_integral(layer));
		}
	}

	entt::entity TileLayerManager::GetActiveLayer() const {
		return activeLayer;
	}

	bool TileLayerManager::IsLayerValid(entt::registry& registry, entt::entity layer) const {
		return registry.valid(layer) && registry.any_of<TilemapLayerComponent>(layer);
	}

	bool TileLayerManager::IsLayerVisible(entt::registry& registry, entt::entity layer) const {
		if (!IsLayerValid(registry, layer)) return false;

		const auto* layerComp = registry.try_get<TilemapLayerComponent>(layer);
		return layerComp ? layerComp->visible : false;
	}

	bool TileLayerManager::IsLayerLocked(entt::registry& registry, entt::entity layer) const {
		if (!IsLayerValid(registry, layer)) return true; // Treat invalid layers as locked

		const auto* layerComp = registry.try_get<TilemapLayerComponent>(layer);
		return layerComp ? layerComp->locked : true;
	}

	void TileLayerManager::SetLayerVisible(entt::registry& registry, entt::entity layer, bool visible) {
		if (!IsLayerValid(registry, layer)) return;

		auto& layerComp = registry.get<TilemapLayerComponent>(layer);
		if (layerComp.visible != visible) {
			layerComp.visible = visible;
			NotifyLayerPropertyChanged(layer, LayerProperty::Visibility);

			spdlog::debug("[TileLayerManager] Set layer {} visibility to {}",
				entt::to_integral(layer), visible);
		}
	}

	void TileLayerManager::SetLayerLocked(entt::registry& registry, entt::entity layer, bool locked) {
		if (!IsLayerValid(registry, layer)) return;

		auto& layerComp = registry.get<TilemapLayerComponent>(layer);
		if (layerComp.locked != locked) {
			layerComp.locked = locked;
			NotifyLayerPropertyChanged(layer, LayerProperty::Locked);

			spdlog::debug("[TileLayerManager] Set layer {} locked to {}",
				entt::to_integral(layer), locked);
		}
	}

	void TileLayerManager::SetLayerOpacity(entt::registry& registry, entt::entity layer, float opacity) {
		if (!IsLayerValid(registry, layer)) return;

		auto& layerComp = registry.get<TilemapLayerComponent>(layer);
		float clampedOpacity = std::clamp(opacity, 0.0f, 1.0f);

		if (std::abs(layerComp.opacity - clampedOpacity) > 0.001f) {
			layerComp.opacity = clampedOpacity;
			NotifyLayerPropertyChanged(layer, LayerProperty::Opacity);

			spdlog::debug("[TileLayerManager] Set layer {} opacity to {:.3f}",
				entt::to_integral(layer), clampedOpacity);
		}
	}

	void TileLayerManager::SetLayerSortOrder(entt::registry& registry, entt::entity layer, int sortOrder) {
		if (!IsLayerValid(registry, layer)) return;

		auto& layerComp = registry.get<TilemapLayerComponent>(layer);
		if (layerComp.sortingOrder != sortOrder) {
			layerComp.sortingOrder = sortOrder;
			NotifyLayerPropertyChanged(layer, LayerProperty::SortOrder);

			spdlog::debug("[TileLayerManager] Set layer {} sort order to {}",
				entt::to_integral(layer), sortOrder);
		}
	}

	// ═════════════════════════════════════════════════════════════════════
	// MULTI-LAYER PAINTING OPERATIONS
	// ═════════════════════════════════════════════════════════════════════

	void TileLayerManager::PaintToAllLayers(entt::registry& registry, const std::vector<entt::entity>& layers,
		const glm::ivec2& position, int tileId) {

		if (layers.empty()) return;

		auto& tilemapSystem = TilemapSystem::GetInstance();
		std::vector<PaintTilesCommand::TileChange> allChanges;

		// Collect changes for all layers
		for (entt::entity layer : layers) {
			if (!IsLayerValid(registry, layer) || IsLayerLocked(registry, layer)) {
				continue;
			}

			PaintTilesCommand::TileChange change;
			change.position = position;
			change.oldTileId = tilemapSystem.GetTile(registry, layer, position);
			change.newTileId = tileId;

			if (change.oldTileId != change.newTileId) {
				allChanges.push_back(change);
				tilemapSystem.SetTile(registry, layer, position, tileId);
			}
		}

		// Create command for undo/redo if we have a command history
		if (!allChanges.empty() && g_commandHistory) {
			// For multi-layer operations, we'd need a compound command
			// For now, just execute the operations directly
			spdlog::debug("[TileLayerManager] Painted tile {} to {} layers at ({}, {})",
				tileId, allChanges.size(), position.x, position.y);
		}
	}

	void TileLayerManager::PaintToActiveLayers(entt::registry& registry, const glm::ivec2& position, int tileId) {
		std::vector<entt::entity> activeLayers = GetPaintableLayers(registry);
		PaintToAllLayers(registry, activeLayers, position, tileId);
	}

	// ═════════════════════════════════════════════════════════════════════
	// LAYER COPYING AND REGION OPERATIONS
	// ═════════════════════════════════════════════════════════════════════

	void TileLayerManager::CopyLayerRegion(entt::registry& registry, entt::entity srcLayer, entt::entity dstLayer,
		const glm::ivec2& srcMin, const glm::ivec2& srcMax, const glm::ivec2& dstPos) {

		if (!IsLayerValid(registry, srcLayer) || !IsLayerValid(registry, dstLayer)) {
			spdlog::error("[TileLayerManager] Invalid source or destination layer for copy operation");
			return;
		}

		if (IsLayerLocked(registry, dstLayer)) {
			spdlog::warn("[TileLayerManager] Destination layer is locked, cannot copy");
			return;
		}

		auto& tilemapSystem = TilemapSystem::GetInstance();
		glm::ivec2 offset = dstPos - srcMin;

		std::vector<PaintTilesCommand::TileChange> changes;
		int tilesCopied = 0;

		for (int y = srcMin.y; y <= srcMax.y; ++y) {
			for (int x = srcMin.x; x <= srcMax.x; ++x) {
				glm::ivec2 srcTilePos{ x, y };
				glm::ivec2 dstTilePos = srcTilePos + offset;

				int srcTileId = tilemapSystem.GetTile(registry, srcLayer, srcTilePos);
				if (srcTileId != -1) {
					int oldDstTileId = tilemapSystem.GetTile(registry, dstLayer, dstTilePos);

					if (oldDstTileId != srcTileId) {
						PaintTilesCommand::TileChange change;
						change.position = dstTilePos;
						change.oldTileId = oldDstTileId;
						change.newTileId = srcTileId;
						changes.push_back(change);

						tilemapSystem.SetTile(registry, dstLayer, dstTilePos, srcTileId);
						tilesCopied++;
					}
				}
			}
		}

		// Create command for undo/redo
		if (!changes.empty() && g_commandHistory) {
			auto command = std::make_unique<PaintTilesCommand>(registry, dstLayer, changes);
			g_commandHistory->ExecuteCommand(std::move(command));
		}

		spdlog::info("[TileLayerManager] Copied {} tiles from region ({},{}) to ({},{}) from layer {} to layer {}",
			tilesCopied, srcMin.x, srcMin.y, srcMax.x, srcMax.y,
			entt::to_integral(srcLayer), entt::to_integral(dstLayer));
	}

	void TileLayerManager::CopyLayerToClipboard(entt::registry& registry, entt::entity layer,
		const glm::ivec2& min, const glm::ivec2& max) {

		if (!IsLayerValid(registry, layer)) return;

		auto& tilemapSystem = TilemapSystem::GetInstance();

		// Clear previous clipboard data
		clipboardData.clear();
		clipboardSize = max - min + glm::ivec2{ 1, 1 };

		// Copy tiles to clipboard
		for (int y = min.y; y <= max.y; ++y) {
			for (int x = min.x; x <= max.x; ++x) {
				glm::ivec2 pos{ x, y };
				int tileId = tilemapSystem.GetTile(registry, layer, pos);

				ClipboardTile clipTile;
				clipTile.position = pos - min; // Store relative position
				clipTile.tileId = tileId;
				clipboardData.push_back(clipTile);
			}
		}

		spdlog::debug("[TileLayerManager] Copied {}x{} region to clipboard with {} tiles",
			clipboardSize.x, clipboardSize.y, clipboardData.size());
	}

	void TileLayerManager::PasteFromClipboard(entt::registry& registry, entt::entity layer,
		const glm::ivec2& position) {

		if (!IsLayerValid(registry, layer) || IsLayerLocked(registry, layer)) return;
		if (clipboardData.empty()) return;

		auto& tilemapSystem = TilemapSystem::GetInstance();
		std::vector<PaintTilesCommand::TileChange> changes;

		for (const auto& clipTile : clipboardData) {
			if (clipTile.tileId != -1) { // Only paste non-empty tiles
				glm::ivec2 pastePos = position + clipTile.position;
				int oldTileId = tilemapSystem.GetTile(registry, layer, pastePos);

				if (oldTileId != clipTile.tileId) {
					PaintTilesCommand::TileChange change;
					change.position = pastePos;
					change.oldTileId = oldTileId;
					change.newTileId = clipTile.tileId;
					changes.push_back(change);

					tilemapSystem.SetTile(registry, layer, pastePos, clipTile.tileId);
				}
			}
		}

		// Create command for undo/redo
		if (!changes.empty() && g_commandHistory) {
			auto command = std::make_unique<PaintTilesCommand>(registry, layer, changes);
			g_commandHistory->ExecuteCommand(std::move(command));
		}

		spdlog::debug("[TileLayerManager] Pasted {} tiles at ({}, {})",
			changes.size(), position.x, position.y);
	}

	// ═════════════════════════════════════════════════════════════════════
	// LAYER BLENDING AND COMPOSITING
	// ═════════════════════════════════════════════════════════════════════

	void TileLayerManager::BlendLayers(entt::registry& registry, entt::entity baseLayer, entt::entity overlayLayer,
		const glm::ivec2& min, const glm::ivec2& max, float opacity) {

		if (!IsLayerValid(registry, baseLayer) || !IsLayerValid(registry, overlayLayer)) {
			spdlog::error("[TileLayerManager] Invalid layers for blend operation");
			return;
		}

		if (IsLayerLocked(registry, baseLayer)) {
			spdlog::warn("[TileLayerManager] Base layer is locked, cannot blend");
			return;
		}

		auto& tilemapSystem = TilemapSystem::GetInstance();
		std::vector<PaintTilesCommand::TileChange> changes;
		float clampedOpacity = std::clamp(opacity, 0.0f, 1.0f);

		for (int y = min.y; y <= max.y; ++y) {
			for (int x = min.x; x <= max.x; ++x) {
				glm::ivec2 pos{ x, y };

				int overlayTile = tilemapSystem.GetTile(registry, overlayLayer, pos);
				if (overlayTile != -1) {
					int baseTile = tilemapSystem.GetTile(registry, baseLayer, pos);
					int resultTile = BlendTiles(baseTile, overlayTile, clampedOpacity);

					if (baseTile != resultTile) {
						PaintTilesCommand::TileChange change;
						change.position = pos;
						change.oldTileId = baseTile;
						change.newTileId = resultTile;
						changes.push_back(change);

						tilemapSystem.SetTile(registry, baseLayer, pos, resultTile);
					}
				}
			}
		}

		// Create command for undo/redo
		if (!changes.empty() && g_commandHistory) {
			auto command = std::make_unique<PaintTilesCommand>(registry, baseLayer, changes);
			g_commandHistory->ExecuteCommand(std::move(command));
		}

		spdlog::debug("[TileLayerManager] Blended {} tiles in region ({},{}) to ({},{}) with opacity {:.3f}",
			changes.size(), min.x, min.y, max.x, max.y, clampedOpacity);
	}

	void TileLayerManager::MergeLayers(entt::registry& registry, entt::entity targetLayer,
		const std::vector<entt::entity>& sourceLayers, const glm::ivec2& min, const glm::ivec2& max) {

		if (!IsLayerValid(registry, targetLayer) || IsLayerLocked(registry, targetLayer)) {
			spdlog::error("[TileLayerManager] Invalid or locked target layer for merge");
			return;
		}

		auto& tilemapSystem = TilemapSystem::GetInstance();
		std::vector<PaintTilesCommand::TileChange> changes;

		for (int y = min.y; y <= max.y; ++y) {
			for (int x = min.x; x <= max.x; ++x) {
				glm::ivec2 pos{ x, y };
				int targetTile = tilemapSystem.GetTile(registry, targetLayer, pos);
				int resultTile = targetTile;

				// Merge from all source layers in order
				for (entt::entity sourceLayer : sourceLayers) {
					if (!IsLayerValid(registry, sourceLayer)) continue;

					int sourceTile = tilemapSystem.GetTile(registry, sourceLayer, pos);
					if (sourceTile != -1) {
						resultTile = MergeTiles(resultTile, sourceTile);
					}
				}

				if (targetTile != resultTile) {
					PaintTilesCommand::TileChange change;
					change.position = pos;
					change.oldTileId = targetTile;
					change.newTileId = resultTile;
					changes.push_back(change);

					tilemapSystem.SetTile(registry, targetLayer, pos, resultTile);
				}
			}
		}

		// Create command for undo/redo
		if (!changes.empty() && g_commandHistory) {
			auto command = std::make_unique<PaintTilesCommand>(registry, targetLayer, changes);
			g_commandHistory->ExecuteCommand(std::move(command));
		}

		spdlog::info("[TileLayerManager] Merged {} source layers into target layer, {} tiles changed",
			sourceLayers.size(), changes.size());
	}

	// ═════════════════════════════════════════════════════════════════════
	// LAYER ANALYSIS AND UTILITIES
	// ═════════════════════════════════════════════════════════════════════

	std::vector<entt::entity> TileLayerManager::GetPaintableLayers(entt::registry& registry) const {
		std::vector<entt::entity> paintableLayers;

		auto layerView = registry.view<TilemapLayerComponent>();
		for (entt::entity layer : layerView) {
			if (IsLayerValid(registry, layer) && !IsLayerLocked(registry, layer) && IsLayerVisible(registry, layer)) {
				paintableLayers.push_back(layer);
			}
		}

		// Sort by sorting order
		std::sort(paintableLayers.begin(), paintableLayers.end(),
			[&registry](entt::entity a, entt::entity b) {
				const auto& layerA = registry.get<TilemapLayerComponent>(a);
				const auto& layerB = registry.get<TilemapLayerComponent>(b);
				return layerA.sortingOrder < layerB.sortingOrder;
			});

		return paintableLayers;
	}

	std::vector<entt::entity> TileLayerManager::GetAllLayers(entt::registry& registry) const {
		std::vector<entt::entity> allLayers;

		auto layerView = registry.view<TilemapLayerComponent>();
		for (entt::entity layer : layerView) {
			if (IsLayerValid(registry, layer)) {
				allLayers.push_back(layer);
			}
		}

		// Sort by sorting order
		std::sort(allLayers.begin(), allLayers.end(),
			[&registry](entt::entity a, entt::entity b) {
				const auto& layerA = registry.get<TilemapLayerComponent>(a);
				const auto& layerB = registry.get<TilemapLayerComponent>(b);
				return layerA.sortingOrder < layerB.sortingOrder;
			});

		return allLayers;
	}

	std::vector<entt::entity> TileLayerManager::GetLayersInTilemap(entt::registry& registry, entt::entity tilemap) const {
		std::vector<entt::entity> layers;

		if (!registry.valid(tilemap)) return layers;

		auto* tilemapNode = registry.try_get<SceneNodeComponent>(tilemap);
		if (!tilemapNode) return layers;

		for (entt::entity child : tilemapNode->children) {
			if (IsLayerValid(registry, child)) {
				layers.push_back(child);
			}
		}

		// Sort by sorting order
		std::sort(layers.begin(), layers.end(),
			[&registry](entt::entity a, entt::entity b) {
				const auto& layerA = registry.get<TilemapLayerComponent>(a);
				const auto& layerB = registry.get<TilemapLayerComponent>(b);
				return layerA.sortingOrder < layerB.sortingOrder;
			});

		return layers;
	}

	LayerInfo TileLayerManager::GetLayerInfo(entt::registry& registry, entt::entity layer) const {
		LayerInfo info;

		if (!IsLayerValid(registry, layer)) {
			return info; // Returns default-constructed (invalid) info
		}

		const auto& layerComp = registry.get<TilemapLayerComponent>(layer);
		const auto* nodeComp = registry.try_get<SceneNodeComponent>(layer);

		info.entity = layer;
		info.name = nodeComp ? nodeComp->name : "Unknown Layer";
		info.visible = layerComp.visible;
		info.locked = layerComp.locked;
		info.opacity = layerComp.opacity;
		info.sortingOrder = layerComp.sortingOrder;
		info.hasCollision = layerComp.hasCollision;
		info.materialName = layerComp.materialName;

		return info;
	}

	bool TileLayerManager::IsPositionInBounds(entt::registry& registry, entt::entity layer,
		const glm::ivec2& position) const {

		// For now, we don't have explicit bounds checking
		// In a full implementation, you might check against chunk boundaries or world limits
		return true;
	}

	// ═════════════════════════════════════════════════════════════════════
	// CALLBACK SYSTEM
	// ═════════════════════════════════════════════════════════════════════

	void TileLayerManager::RegisterLayerChangedCallback(LayerChangedCallback callback) {
		layerChangedCallbacks.push_back(std::move(callback));
	}

	void TileLayerManager::RegisterPropertyChangedCallback(PropertyChangedCallback callback) {
		propertyChangedCallbacks.push_back(std::move(callback));
	}

	// ═════════════════════════════════════════════════════════════════════
	// PRIVATE HELPER METHODS
	// ═════════════════════════════════════════════════════════════════════

	int TileLayerManager::BlendTiles(int baseTile, int overlayTile, float opacity) const {
		if (opacity <= 0.0f) return baseTile;
		if (opacity >= 1.0f) return overlayTile != -1 ? overlayTile : baseTile;

		// For now, simple implementation: use overlay if opacity > 0.5, otherwise base
		// In a full implementation, you might have more sophisticated blending
		return opacity > 0.5f ? overlayTile : baseTile;
	}

	int TileLayerManager::MergeTiles(int baseTile, int overlayTile) const {
		// Simple merge: overlay takes precedence if not empty
		return overlayTile != -1 ? overlayTile : baseTile;
	}

	void TileLayerManager::NotifyActiveLayerChanged(entt::entity oldLayer, entt::entity newLayer) {
		for (auto& callback : layerChangedCallbacks) {
			try {
				callback(oldLayer, newLayer);
			}
			catch (const std::exception& e) {
				spdlog::error("[TileLayerManager] Layer changed callback exception: {}", e.what());
			}
		}
	}

	void TileLayerManager::NotifyLayerPropertyChanged(entt::entity layer, LayerProperty property) {
		for (auto& callback : propertyChangedCallbacks) {
			try {
				callback(layer, property);
			}
			catch (const std::exception& e) {
				spdlog::error("[TileLayerManager] Property changed callback exception: {}", e.what());
			}
		}
	}

} // namespace WanderSpire