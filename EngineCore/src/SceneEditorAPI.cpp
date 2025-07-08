//=============================================================================
// SceneEditorAPI.cpp - Editor Helper Functions and Utilities
// 
// This file contains editor-specific helper functions and utilities that
// support the main Scene Editor API implemented in EngineCore.cpp.
// All main API functions have been consolidated into EngineCore.cpp to
// eliminate duplication and improve maintainability.
//=============================================================================

#include "EngineAPI.h"
#include "EngineCore.h"

#include <WanderSpire/Components/AllComponents.h>
#include <WanderSpire/Editor/EditorSystems.h>
#include <WanderSpire/Editor/TilePaint/TilePaintSystems.h>
#include <WanderSpire/World/TilemapSystem.h>
#include <spdlog/spdlog.h>

using namespace WanderSpire;
using namespace WanderSpire::EditorAPI;

namespace WanderSpire {
	namespace EditorAPI {

		//=============================================================================
		// VALIDATION HELPERS
		//=============================================================================

		bool ValidateEntityExists(entt::registry& registry, entt::entity entity) {
			if (entity == entt::null || !registry.valid(entity)) {
				spdlog::warn("[EditorAPI] Invalid entity: {}", entt::to_integral(entity));
				return false;
			}
			return true;
		}

		bool ValidateStringParameter(const char* parameter, const char* paramName) {
			if (!parameter || strlen(parameter) == 0) {
				spdlog::warn("[EditorAPI] Invalid or empty string parameter: {}", paramName ? paramName : "unknown");
				return false;
			}
			return true;
		}

		bool ValidateOutputBuffer(void* buffer, int size, const char* bufferName) {
			if (!buffer || size <= 0) {
				spdlog::warn("[EditorAPI] Invalid output buffer: {} (ptr={}, size={})",
					bufferName ? bufferName : "unknown", (void*)buffer, size);
				return false;
			}
			return true;
		}

		//=============================================================================
		// TILEMAP HELPERS
		//=============================================================================

		entt::entity FindMainTilemapInRegistry(entt::registry& registry) {
			// Find the first tilemap entity that has tilemap layers
			auto tilemapView = registry.view<SceneNodeComponent>();

			for (auto entity : tilemapView) {
				// Check if this entity has tilemap layers as children
				auto layerView = registry.view<TilemapLayerComponent>();
				for (auto layerEntity : layerView) {
					if (auto* node = registry.try_get<SceneNodeComponent>(layerEntity)) {
						if (node->parent == entity) {
							spdlog::debug("[EditorAPI] Found main tilemap: {}", entt::to_integral(entity));
							return entity;
						}
					}
				}
			}

			spdlog::warn("[EditorAPI] No main tilemap found in registry");
			return entt::null;
		}

		std::vector<entt::entity> GetAllTilemapLayers(entt::registry& registry, entt::entity tilemap) {
			std::vector<entt::entity> layers;

			if (!ValidateEntityExists(registry, tilemap)) {
				return layers;
			}

			auto layerView = registry.view<TilemapLayerComponent, SceneNodeComponent>();
			for (auto layerEntity : layerView) {
				auto& node = layerView.get<SceneNodeComponent>(layerEntity);
				if (node.parent == tilemap) {
					layers.push_back(layerEntity);
				}
			}

			// Sort by layer index
			std::sort(layers.begin(), layers.end(), [&registry](entt::entity a, entt::entity b) {
				auto* layerA = registry.try_get<TilemapLayerComponent>(a);
				auto* layerB = registry.try_get<TilemapLayerComponent>(b);
				if (!layerA || !layerB) return false;
				return layerA->layerIndex < layerB->layerIndex;
				});

			return layers;
		}

		bool IsTilePositionValid(const glm::ivec2& position, int minX, int minY, int maxX, int maxY) {
			return position.x >= minX && position.x <= maxX &&
				position.y >= minY && position.y <= maxY;
		}

		//=============================================================================
		// SELECTION HELPERS
		//=============================================================================

		bool IsEntitySelectable(entt::registry& registry, entt::entity entity) {
			if (!ValidateEntityExists(registry, entity)) {
				return false;
			}

			// Check if entity has a SelectableComponent and is marked as selectable
			if (auto* selectable = registry.try_get<SelectableComponent>(entity)) {
				return selectable->selectable;
			}

			// Default: entities without SelectableComponent are selectable
			return true;
		}

		glm::vec2 GetEntityScreenPosition(entt::registry& registry, entt::entity entity, float tileSize) {
			if (!ValidateEntityExists(registry, entity)) {
				return glm::vec2(0.0f);
			}

			// Try GridPositionComponent first
			if (auto* gridPos = registry.try_get<GridPositionComponent>(entity)) {
				return glm::vec2(gridPos->tile) * tileSize + glm::vec2(tileSize * 0.5f);
			}

			// Fall back to TransformComponent
			if (auto* transform = registry.try_get<TransformComponent>(entity)) {
				return transform->worldPosition;
			}

			return glm::vec2(0.0f);
		}

		//=============================================================================
		// LAYER MANAGEMENT HELPERS
		//=============================================================================

		entt::entity GetActiveLayer(entt::registry& registry) {
			return TileLayerManager::GetInstance().GetActiveLayer();
		}

		bool IsLayerPaintable(entt::registry& registry, entt::entity layer) {
			if (!ValidateLayer(registry, layer)) {
				return false;
			}

			auto layerInfo = TileLayerManager::GetInstance().GetLayerInfo(registry, layer);
			return layerInfo.visible && !layerInfo.locked;
		}

		int GetNextLayerSortOrder(entt::registry& registry, entt::entity tilemap) {
			auto layers = GetAllTilemapLayers(registry, tilemap);
			int maxOrder = 0;

			for (auto layer : layers) {
				if (auto* layerComp = registry.try_get<TilemapLayerComponent>(layer)) {
					maxOrder = std::max(maxOrder, layerComp->sortingOrder);
				}
			}

			return maxOrder + 1;
		}

		//=============================================================================
		// COMMAND SYSTEM HELPERS  
		//=============================================================================

		bool CanExecuteEditorCommand(entt::registry& registry) {
			// Check if there are any selected entities or active operations
			const auto& selected = SelectionManager::GetInstance().GetSelectedEntities();
			return !selected.empty();
		}

		void LogEditorAction(const std::string& action, const std::string& details) {
			spdlog::info("[EditorAction] {}: {}", action, details);
		}

		//=============================================================================
		// COORDINATE CONVERSION HELPERS
		//=============================================================================

		glm::ivec2 ScreenToTile(const glm::vec2& screenPos, const glm::vec2& cameraPos,
			float zoom, const glm::ivec2& screenSize, float tileSize) {
			// Convert screen coordinates to world coordinates
			glm::vec2 worldPos = cameraPos + (screenPos - glm::vec2(screenSize) * 0.5f) / zoom;

			// Convert world coordinates to tile coordinates
			return glm::ivec2(std::floor(worldPos.x / tileSize), std::floor(worldPos.y / tileSize));
		}

		glm::vec2 TileToScreen(const glm::ivec2& tilePos, const glm::vec2& cameraPos,
			float zoom, const glm::ivec2& screenSize, float tileSize) {
			// Convert tile coordinates to world coordinates (center of tile)
			glm::vec2 worldPos = (glm::vec2(tilePos) + 0.5f) * tileSize;

			// Convert world coordinates to screen coordinates
			return (worldPos - cameraPos) * zoom + glm::vec2(screenSize) * 0.5f;
		}

		glm::vec4 GetTileBounds(const glm::ivec2& tilePos, float tileSize) {
			glm::vec2 worldPos = glm::vec2(tilePos) * tileSize;
			return glm::vec4(worldPos.x, worldPos.y, worldPos.x + tileSize, worldPos.y + tileSize);
		}

		//=============================================================================
		// EDITOR STATE HELPERS
		//=============================================================================

		bool IsEditorInPaintMode() {
			// Check if tile painting is currently active
			return TilePaintingManager::GetInstance().GetActiveBrush().showPreview;
		}

		bool IsEditorInSelectionMode() {
			// Check if selection tool is active
			return SelectionManager::GetInstance().GetSelectionCount() > 0;
		}

		void ResetEditorState() {
			// Clear all transient editor state
			SelectionManager::GetInstance().DeselectAll(*(entt::registry*)nullptr); // This needs proper registry
			TilePaintingManager::GetInstance().ClearPreview();

			spdlog::info("[EditorAPI] Editor state reset");
		}

		//=============================================================================
		// DEBUG AND DIAGNOSTICS
		//=============================================================================

		void DumpEntityInfo(entt::registry& registry, entt::entity entity) {
			if (!ValidateEntityExists(registry, entity)) {
				return;
			}

			spdlog::info("[EditorAPI] Entity {} info:", entt::to_integral(entity));

			if (registry.all_of<IDComponent>(entity)) {
				auto& id = registry.get<IDComponent>(entity);
				spdlog::info("    IDComponent: uuid={}", id.uuid);
			}

			if (registry.all_of<TagComponent>(entity)) {
				auto& tag = registry.get<TagComponent>(entity);
				spdlog::info("    TagComponent: tag='{}'", tag.tag);
			}

			if (registry.all_of<GridPositionComponent>(entity)) {
				auto& pos = registry.get<GridPositionComponent>(entity);
				spdlog::info("    GridPositionComponent: tile=({}, {})", pos.tile.x, pos.tile.y);
			}

			if (registry.all_of<TilemapLayerComponent>(entity)) {
				auto& layer = registry.get<TilemapLayerComponent>(entity);
				spdlog::info("    TilemapLayerComponent: index={}, name='{}', visible={}",
					layer.layerIndex, layer.layerName, layer.visible);
			}

			if (registry.all_of<SelectableComponent>(entity)) {
				auto& sel = registry.get<SelectableComponent>(entity);
				spdlog::info("    SelectableComponent: selectable={}, selected={}",
					sel.selectable, sel.selected);
			}
		}

		void DumpRegistryStats(entt::registry& registry) {
			spdlog::info("[EditorAPI] Registry statistics:");
			size_t count = registry.storage<entt::entity>().size();
			spdlog::info("  Total entities: {}", count);



			// Count entities by component type
			auto tilemapLayers = registry.view<TilemapLayerComponent>().size();
			auto selectableEntities = registry.view<SelectableComponent>().size();
			auto gridEntities = registry.view<GridPositionComponent>().size();
			auto spriteEntities = registry.view<SpriteComponent>().size();

			spdlog::info("  Tilemap layers: {}", tilemapLayers);
			spdlog::info("  Selectable entities: {}", selectableEntities);
			spdlog::info("  Grid entities: {}", gridEntities);
			spdlog::info("  Sprite entities: {}", spriteEntities);
		}

	} // namespace EditorAPI
} // namespace WanderSpire

//=============================================================================
// C API WRAPPERS FOR EDITOR HELPERS
//=============================================================================

extern "C" {

	// These are additional helper functions that wrap the C++ helpers above
	// All main API functions are implemented in EngineCore.cpp

	ENGINE_API int EditorHelper_ValidateEntity(EngineContextHandle ctx, EntityId entity) {
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(ctx);
		if (!w) return 0;

		auto& registry = w->reg();
		entt::entity ent = static_cast<entt::entity>(entity.id);
		return ValidateEntityExists(registry, ent) ? 1 : 0;
	}

	ENGINE_API EntityId EditorHelper_FindMainTilemap(EngineContextHandle ctx) {
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(ctx);
		if (!w) return { WS_INVALID_ENTITY };

		auto& registry = w->reg();
		entt::entity tilemap = FindMainTilemapInRegistry(registry);
		return { entt::to_integral(tilemap) };
	}

	ENGINE_API int EditorHelper_IsEntitySelectable(EngineContextHandle ctx, EntityId entity) {
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(ctx);
		if (!w) return 0;

		auto& registry = w->reg();
		entt::entity ent = static_cast<entt::entity>(entity.id);
		return IsEntitySelectable(registry, ent) ? 1 : 0;
	}

	ENGINE_API int EditorHelper_IsLayerPaintable(EngineContextHandle ctx, EntityId layer) {
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(ctx);
		if (!w) return 0;

		auto& registry = w->reg();
		entt::entity layerEnt = static_cast<entt::entity>(layer.id);
		return IsLayerPaintable(registry, layerEnt) ? 1 : 0;
	}

	ENGINE_API void EditorHelper_DumpEntityInfo(EngineContextHandle ctx, EntityId entity) {
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity ent = static_cast<entt::entity>(entity.id);
		DumpEntityInfo(registry, ent);
	}

	ENGINE_API void EditorHelper_DumpRegistryStats(EngineContextHandle ctx) {
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(ctx);
		if (!w) return;

		auto& registry = w->reg();
		DumpRegistryStats(registry);
	}

	ENGINE_API void EditorHelper_ScreenToTile(
		EngineContextHandle ctx,
		float screenX, float screenY,
		float cameraX, float cameraY,
		float zoom,
		int screenWidth, int screenHeight,
		int* outTileX, int* outTileY)
	{
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(ctx);
		if (!w || !outTileX || !outTileY) return;

		float tileSize = w->tileSize();
		glm::ivec2 tilePos = ScreenToTile(
			{ screenX, screenY },
			{ cameraX, cameraY },
			zoom,
			{ screenWidth, screenHeight },
			tileSize
		);

		*outTileX = tilePos.x;
		*outTileY = tilePos.y;
	}

	ENGINE_API void EditorHelper_TileToScreen(
		EngineContextHandle ctx,
		int tileX, int tileY,
		float cameraX, float cameraY,
		float zoom,
		int screenWidth, int screenHeight,
		float* outScreenX, float* outScreenY)
	{
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(ctx);
		if (!w || !outScreenX || !outScreenY) return;

		float tileSize = w->tileSize();
		glm::vec2 screenPos = TileToScreen(
			{ tileX, tileY },
			{ cameraX, cameraY },
			zoom,
			{ screenWidth, screenHeight },
			tileSize
		);

		*outScreenX = screenPos.x;
		*outScreenY = screenPos.y;
	}

} // extern "C"