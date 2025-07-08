#include "WanderSpire/Scene/PostProcessors.h"
#include "WanderSpire/Components/AllComponents.h"
#include "WanderSpire/Components/GridPositionComponent.h"
#include "WanderSpire/Components/TransformComponent.h"
#include "WanderSpire/Graphics/RenderResourceManager.h"
#include "WanderSpire/Editor/SceneHierarchyManager.h"
#include <algorithm>
#include <cmath>

namespace WanderSpire::Scene {

	// ============================================================================
	// TilemapPostProcessor
	// ============================================================================

	void TilemapPostProcessor::ProcessLoadedEntities(entt::registry& registry,
		const std::vector<entt::entity>& entities) {

		RestoreTilemapHierarchy(registry);
		OptimizeChunks(registry);
	}

	void TilemapPostProcessor::RestoreTilemapHierarchy(entt::registry& registry) {
		auto tilemapLayers = registry.view<TilemapLayerComponent, SceneNodeComponent>();
		auto tilemapChunks = registry.view<TilemapChunkComponent, SceneNodeComponent>();
		auto allNodes = registry.view<SceneNodeComponent>();

		// Find tilemaps and assign orphaned layers
		for (auto entity : allNodes) {
			auto& node = allNodes.get<SceneNodeComponent>(entity);

			if (node.name.find("Tilemap") != std::string::npos &&
				!registry.any_of<TilemapLayerComponent>(entity)) {

				for (auto layer : tilemapLayers) {
					auto& layerNode = tilemapLayers.get<SceneNodeComponent>(layer);
					if (layerNode.parent == entt::null) {
						layerNode.parent = entity;
						node.children.push_back(layer);
					}
				}
			}
		}

		// Assign orphaned chunks to layers
		if (auto firstLayer = tilemapLayers.begin(); firstLayer != tilemapLayers.end()) {
			auto& layerNode = tilemapLayers.get<SceneNodeComponent>(*firstLayer);

			for (auto chunk : tilemapChunks) {
				auto& chunkNode = tilemapChunks.get<SceneNodeComponent>(chunk);
				if (chunkNode.parent == entt::null) {
					chunkNode.parent = *firstLayer;
					layerNode.children.push_back(chunk);
				}
			}
		}
	}

	void TilemapPostProcessor::OptimizeChunks(entt::registry& registry) {
		auto chunkView = registry.view<TilemapChunkComponent>();

		for (auto chunk : chunkView) {
			auto& chunkComponent = chunkView.get<TilemapChunkComponent>(chunk);

			// Count non-empty tiles
			int nonEmptyTiles = std::count_if(chunkComponent.tileIds.begin(),
				chunkComponent.tileIds.end(), [](int id) { return id != -1; });

			// Configure for rendering
			chunkComponent.instanceCount = nonEmptyTiles;
			chunkComponent.instanceVBO = 0;  // Force rebuild
			chunkComponent.dirty = true;
			chunkComponent.loaded = true;
			chunkComponent.visible = true;
		}
	}

	void TilemapPostProcessor::ValidateChunkData(entt::registry& registry, entt::entity chunk) {
		auto* chunkComponent = registry.try_get<TilemapChunkComponent>(chunk);
		if (!chunkComponent) return;

		size_t expectedSize = chunkComponent->chunkSize * chunkComponent->chunkSize;
		chunkComponent->tileIds.resize(expectedSize, -1);
		chunkComponent->tileData.resize(expectedSize, 0);
	}

	// ============================================================================
	// TexturePostProcessor
	// ============================================================================

	void TexturePostProcessor::ProcessLoadedEntities(entt::registry& registry,
		const std::vector<entt::entity>& entities) {
		RestoreAnimationTextures(registry);
	}

	void TexturePostProcessor::RestoreAnimationTextures(entt::registry& registry) {
		auto animView = registry.view<SpriteAnimationComponent, SpriteComponent>();

		for (auto entity : animView) {
			auto& sprite = animView.get<SpriteComponent>(entity);
			auto& anim = animView.get<SpriteAnimationComponent>(entity);

			if (sprite.frameName.empty()) {
				// Spritesheet reference
				auto tex = RenderResourceManager::Get().GetTexture(sprite.atlasName);
				if (tex) {
					anim.texture = tex;
					anim.columns = anim.texture->GetWidth() / anim.frameWidth;
					anim.rows = anim.texture->GetHeight() / anim.frameHeight;
				}
			}
			else {
				// Atlas reference
				if (auto* atlas = RenderResourceManager::Get().GetAtlas(sprite.atlasName)) {
					anim.texture = atlas->GetTexture();
					if (anim.texture) {
						anim.columns = anim.texture->GetWidth() / anim.frameWidth;
						anim.rows = anim.texture->GetHeight() / anim.frameHeight;
					}
				}
			}
		}
	}

	// ============================================================================
	// HierarchyPostProcessor
	// ============================================================================

	void HierarchyPostProcessor::ProcessLoadedEntities(entt::registry& registry,
		const std::vector<entt::entity>& entities) {
		ValidateParentChildRelationships(registry);
		UpdateWorldTransforms(registry);
	}

	void HierarchyPostProcessor::ValidateParentChildRelationships(entt::registry& registry) {
		auto nodeView = registry.view<SceneNodeComponent>();

		for (auto entity : nodeView) {
			auto& node = nodeView.get<SceneNodeComponent>(entity);

			// Clear invalid parent
			if (node.parent != entt::null && !registry.valid(node.parent)) {
				node.parent = entt::null;
			}

			// Remove invalid children
			node.children.erase(
				std::remove_if(node.children.begin(), node.children.end(),
					[&registry](entt::entity child) { return !registry.valid(child); }),
				node.children.end());
		}
	}

	void HierarchyPostProcessor::CleanupInvalidReferences(entt::registry& registry) {
		// Future cleanup logic if needed
	}

	void HierarchyPostProcessor::UpdateWorldTransforms(entt::registry& registry) {
		SceneHierarchyManager::GetInstance().UpdateWorldTransforms(registry);
	}

	// ============================================================================
	// AnimationPostProcessor - Don't reset states, just ensure systems work
	// ============================================================================

	void AnimationPostProcessor::ProcessLoadedEntities(entt::registry& registry,
		const std::vector<entt::entity>& entities) {
		// Don't reset animation states - managed systems will handle updates
		// Just make sure animation components are properly linked to textures
		RestoreAnimationSystems(registry);
	}

	void AnimationPostProcessor::RestoreAnimationSystems(entt::registry& registry) {
		// Ensure all animated entities have proper texture references
		// This is already handled by TexturePostProcessor, so just validate
		auto animView = registry.view<SpriteAnimationComponent>();
		int animatedEntities = 0;

		for (auto entity : animView) {
			auto& anim = animView.get<SpriteAnimationComponent>(entity);
			if (anim.texture) {
				animatedEntities++;
			}
		}

		// Animation systems will naturally update states on next tick
	}

	// ============================================================================
	// PositionPostProcessor - Fix entities stuck between tiles
	// ============================================================================

	void PositionPostProcessor::ProcessLoadedEntities(entt::registry& registry,
		const std::vector<entt::entity>& entities) {
		SnapStuckEntities(registry);
	}

	void PositionPostProcessor::SnapStuckEntities(entt::registry& registry) {
		// Find entities that have both GridPositionComponent and TransformComponent
		auto view = registry.view<GridPositionComponent, TransformComponent>();

		for (auto entity : view) {
			auto& gridPos = view.get<GridPositionComponent>(entity);
			auto& transform = view.get<TransformComponent>(entity);

			// Calculate expected world position from grid position
			float tileSize = 64.0f; // Default tile size - should get from context
			float expectedX = gridPos.tile.x * tileSize + tileSize * 0.5f;
			float expectedY = gridPos.tile.y * tileSize + tileSize * 0.5f;

			// Check if entity is significantly off from its grid position
			float deltaX = std::abs(transform.localPosition.x - expectedX);
			float deltaY = std::abs(transform.localPosition.y - expectedY);

			// If off by more than a quarter tile, snap to grid
			if (deltaX > tileSize * 0.25f || deltaY > tileSize * 0.25f) {
				transform.localPosition = glm::vec2(expectedX, expectedY);
				transform.worldPosition = transform.localPosition;
				transform.isDirty = true;
			}
		}
	}
} // namespace WanderSpire::Scene