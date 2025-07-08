#include "WanderSpire/World/TilemapSystem.h"
#include "WanderSpire/Components/TilemapChunkComponent.h"
#include "WanderSpire/Components/TilemapLayerComponent.h"
#include "WanderSpire/Components/SceneNodeComponent.h"
#include "WanderSpire/Components/TransformComponent.h"
#include "WanderSpire/Core/ConfigManager.h"

#include <algorithm>
#include <queue>
#include <unordered_set>
#include <cmath>
#include <spdlog/spdlog.h>

namespace WanderSpire {

	TilemapSystem& TilemapSystem::GetInstance() {
		static TilemapSystem instance;
		return instance;
	}

	// ═════════════════════════════════════════════════════════════════════
	// TILEMAP & LAYER MANAGEMENT
	// ═════════════════════════════════════════════════════════════════════

	entt::entity TilemapSystem::CreateTilemap(entt::registry& registry, const std::string& name) {
		entt::entity tilemap = registry.create();

		registry.emplace<SceneNodeComponent>(tilemap, SceneNodeComponent{
			.name = name
			});
		registry.emplace<TransformComponent>(tilemap);

		spdlog::debug("[TilemapSystem] Created tilemap '{}' with entity {}", name, entt::to_integral(tilemap));
		return tilemap;
	}

	entt::entity TilemapSystem::CreateTilemapLayer(entt::registry& registry, entt::entity tilemap, const std::string& layerName) {
		entt::entity layer = registry.create();

		registry.emplace<SceneNodeComponent>(layer, SceneNodeComponent{
			.parent = tilemap,
			.name = layerName
			});
		registry.emplace<TransformComponent>(layer);
		registry.emplace<TilemapLayerComponent>(layer, TilemapLayerComponent{
			.layerName = layerName
			});

		// Add to parent's children
		if (auto* tilemapNode = registry.try_get<SceneNodeComponent>(tilemap)) {
			tilemapNode->children.push_back(layer);
		}

		spdlog::debug("[TilemapSystem] Created layer '{}' with entity {} for tilemap {}",
			layerName, entt::to_integral(layer), entt::to_integral(tilemap));
		return layer;
	}

	// ═════════════════════════════════════════════════════════════════════
	// TILE OPERATIONS
	// ═════════════════════════════════════════════════════════════════════

	void TilemapSystem::SetTile(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position, int tileId) {
		glm::ivec2 chunkCoords = GetChunkCoords(position);
		entt::entity chunk = GetOrCreateChunk(registry, tilemapLayer, chunkCoords);

		auto& chunkComponent = registry.get<TilemapChunkComponent>(chunk);
		glm::ivec2 localPos = position - (chunkCoords * chunkSize);
		int index = localPos.y * chunkSize + localPos.x;

		if (index >= 0 && index < static_cast<int>(chunkComponent.tileIds.size())) {
			chunkComponent.tileIds[index] = tileId;
			chunkComponent.dirty = true;

			// Update instance count
			chunkComponent.instanceCount = std::count_if(chunkComponent.tileIds.begin(),
				chunkComponent.tileIds.end(), [](int id) { return id != -1; });
		}
	}

	int TilemapSystem::GetTile(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position) {
		glm::ivec2 chunkCoords = GetChunkCoords(position);

		// Check layer's children first
		if (auto* layerNode = registry.try_get<SceneNodeComponent>(tilemapLayer)) {
			for (entt::entity chunkEntity : layerNode->children) {
				if (auto* chunkComponent = registry.try_get<TilemapChunkComponent>(chunkEntity)) {
					if (chunkComponent->chunkCoords == chunkCoords) {
						glm::ivec2 localPos = position - (chunkCoords * chunkSize);
						int index = localPos.y * chunkSize + localPos.x;

						if (index >= 0 && index < static_cast<int>(chunkComponent->tileIds.size())) {
							return chunkComponent->tileIds[index];
						}
					}
				}
			}
		}

		return -1; // No tile found
	}

	void TilemapSystem::RemoveTile(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position) {
		SetTile(registry, tilemapLayer, position, -1);
	}

	// ═════════════════════════════════════════════════════════════════════
	// CHUNK MANAGEMENT (replaces ChunkManager functionality)
	// ═════════════════════════════════════════════════════════════════════

	void TilemapSystem::LoadChunk(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& chunkCoords) {
		GetOrCreateChunk(registry, tilemapLayer, chunkCoords);
	}

	void TilemapSystem::UnloadChunk(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& chunkCoords) {
		auto chunkView = registry.view<TilemapChunkComponent, SceneNodeComponent>();

		for (auto chunk : chunkView) {
			const auto& chunkComponent = chunkView.get<TilemapChunkComponent>(chunk);
			const auto& node = chunkView.get<SceneNodeComponent>(chunk);

			if (node.parent == tilemapLayer && chunkComponent.chunkCoords == chunkCoords) {
				// Remove from parent's children
				if (auto* parentNode = registry.try_get<SceneNodeComponent>(tilemapLayer)) {
					auto& children = parentNode->children;
					children.erase(std::remove(children.begin(), children.end(), chunk), children.end());
				}

				registry.destroy(chunk);
				spdlog::debug("[TilemapSystem] Unloaded chunk ({}, {}) from layer {}",
					chunkCoords.x, chunkCoords.y, entt::to_integral(tilemapLayer));
				return;
			}
		}
	}

	bool TilemapSystem::IsChunkLoaded(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& chunkCoords) {
		if (auto* layerNode = registry.try_get<SceneNodeComponent>(tilemapLayer)) {
			for (entt::entity chunkEntity : layerNode->children) {
				if (auto* chunkComponent = registry.try_get<TilemapChunkComponent>(chunkEntity)) {
					if (chunkComponent->chunkCoords == chunkCoords) {
						return chunkComponent->loaded;
					}
				}
			}
		}
		return false;
	}

	void TilemapSystem::EnsureChunksLoaded(entt::registry& registry, const glm::vec2& minWorldBound, const glm::vec2& maxWorldBound) {
		// Use a default tile size if we can't get it from context
		// TODO: Get actual tile size from EngineContext
		constexpr float defaultTileSize = 64.0f;

		glm::ivec2 minChunk, maxChunk;
		WorldToChunkBounds(minWorldBound, maxWorldBound, defaultTileSize, minChunk, maxChunk);

		// Find all tilemap layers and ensure chunks are loaded
		auto layerView = registry.view<TilemapLayerComponent>();
		for (auto layer : layerView) {
			for (int cy = minChunk.y; cy <= maxChunk.y; ++cy) {
				for (int cx = minChunk.x; cx <= maxChunk.x; ++cx) {
					glm::ivec2 chunkCoords{ cx, cy };
					if (!IsChunkLoaded(registry, layer, chunkCoords)) {
						LoadChunk(registry, layer, chunkCoords);
					}
				}
			}
		}

		spdlog::debug("[TilemapSystem] Ensured chunks loaded for world bounds ({:.1f},{:.1f}) to ({:.1f},{:.1f})",
			minWorldBound.x, minWorldBound.y, maxWorldBound.x, maxWorldBound.y);
	}

	void TilemapSystem::UpdateTilemapStreaming(entt::registry& registry, const glm::vec2& viewCenter, float viewRadius) {
		// Use a default tile size if we can't get it from context
		// TODO: Get actual tile size from EngineContext
		constexpr float defaultTileSize = 64.0f;

		auto requiredChunks = CalculateRequiredChunks(viewCenter, viewRadius, defaultTileSize);

		auto layerView = registry.view<TilemapLayerComponent>();
		for (auto layer : layerView) {
			// Load missing chunks
			for (uint64_t chunkKey : requiredChunks) {
				glm::ivec2 chunkCoords = KeyToChunkCoords(chunkKey);
				if (!IsChunkLoaded(registry, layer, chunkCoords)) {
					LoadChunk(registry, layer, chunkCoords);
				}
			}

			// Unload distant chunks
			auto chunkView = registry.view<TilemapChunkComponent, SceneNodeComponent>();
			std::vector<entt::entity> chunksToUnload;

			for (auto chunk : chunkView) {
				const auto& chunkComponent = chunkView.get<TilemapChunkComponent>(chunk);
				const auto& node = chunkView.get<SceneNodeComponent>(chunk);

				if (node.parent == layer) {
					uint64_t chunkKey = ChunkCoordsToKey(chunkComponent.chunkCoords);
					if (requiredChunks.find(chunkKey) == requiredChunks.end()) {
						chunksToUnload.push_back(chunk);
					}
				}
			}

			for (entt::entity chunk : chunksToUnload) {
				const auto& chunkComponent = chunkView.get<TilemapChunkComponent>(chunk);
				glm::ivec2 coords = chunkComponent.chunkCoords;
				UnloadChunk(registry, layer, coords);
			}
		}

		//if (!requiredChunks.empty()) {
		//	spdlog::debug("[TilemapSystem] Streaming update: {} chunks required for view center ({:.1f},{:.1f}), radius {:.1f}",
		//		requiredChunks.size(), viewCenter.x, viewCenter.y, viewRadius);
		//}
	}

	// ═════════════════════════════════════════════════════════════════════
	// CONFIGURATION
	// ═════════════════════════════════════════════════════════════════════

	void TilemapSystem::SetChunkSize(int size) {
		chunkSize = std::max(1, std::min(size, 256));
		spdlog::info("[TilemapSystem] Chunk size set to {}", chunkSize);
	}

	int TilemapSystem::GetChunkSize() const {
		return chunkSize;
	}

	void TilemapSystem::SetStreamingRadius(float radius) {
		streamingRadius = std::max(100.0f, radius);
		spdlog::info("[TilemapSystem] Streaming radius set to {:.1f}", streamingRadius);
	}

	float TilemapSystem::GetStreamingRadius() const {
		return streamingRadius;
	}

	// ═════════════════════════════════════════════════════════════════════
	// BULK OPERATIONS
	// ═════════════════════════════════════════════════════════════════════

	void TilemapSystem::FloodFill(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& startPos, int newTileId) {
		int originalTileId = GetTile(registry, tilemapLayer, startPos);
		if (originalTileId == newTileId) return;

		std::queue<glm::ivec2> toProcess;
		std::unordered_set<uint64_t> visited;
		toProcess.push(startPos);

		int tilesChanged = 0;
		while (!toProcess.empty()) {
			glm::ivec2 pos = toProcess.front();
			toProcess.pop();

			uint64_t key = (uint64_t(pos.x) << 32) | uint32_t(pos.y);
			if (visited.count(key)) continue;
			visited.insert(key);

			if (GetTile(registry, tilemapLayer, pos) == originalTileId) {
				SetTile(registry, tilemapLayer, pos, newTileId);
				tilesChanged++;

				toProcess.push(pos + glm::ivec2{ 1, 0 });
				toProcess.push(pos + glm::ivec2{ -1, 0 });
				toProcess.push(pos + glm::ivec2{ 0, 1 });
				toProcess.push(pos + glm::ivec2{ 0, -1 });
			}
		}

		spdlog::debug("[TilemapSystem] Flood fill changed {} tiles from {} to {} starting at ({}, {})",
			tilesChanged, originalTileId, newTileId, startPos.x, startPos.y);
	}

	void TilemapSystem::FloodFillArea(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& min, const glm::ivec2& max, int tileId) {
		int tilesSet = 0;
		for (int y = min.y; y <= max.y; ++y) {
			for (int x = min.x; x <= max.x; ++x) {
				SetTile(registry, tilemapLayer, { x, y }, tileId);
				tilesSet++;
			}
		}

		spdlog::debug("[TilemapSystem] Area fill set {} tiles to {} in area ({},{}) to ({},{})",
			tilesSet, tileId, min.x, min.y, max.x, max.y);
	}

	// ═════════════════════════════════════════════════════════════════════
	// COORDINATE CONVERSION
	// ═════════════════════════════════════════════════════════════════════

	glm::ivec2 TilemapSystem::WorldToTilePosition(const glm::vec2& worldPos, float tileSize) const {
		return {
			static_cast<int>(std::floor(worldPos.x / tileSize)),
			static_cast<int>(std::floor(worldPos.y / tileSize))
		};
	}

	glm::vec2 TilemapSystem::TileToWorldPosition(const glm::ivec2& tilePos, float tileSize) const {
		return {
			static_cast<float>(tilePos.x) * tileSize + tileSize * 0.5f,
			static_cast<float>(tilePos.y) * tileSize + tileSize * 0.5f
		};
	}

	glm::ivec2 TilemapSystem::GetChunkCoords(const glm::ivec2& tilePos) const {
		return {
			static_cast<int>(std::floor(static_cast<float>(tilePos.x) / chunkSize)),
			static_cast<int>(std::floor(static_cast<float>(tilePos.y) / chunkSize))
		};
	}

	void TilemapSystem::WorldToTileBounds(const glm::vec2& minWorld, const glm::vec2& maxWorld, float tileSize,
		glm::ivec2& outMinTile, glm::ivec2& outMaxTile) const {
		outMinTile = WorldToTilePosition(minWorld, tileSize);
		outMaxTile = WorldToTilePosition(maxWorld, tileSize);
	}

	void TilemapSystem::WorldToChunkBounds(const glm::vec2& minWorld, const glm::vec2& maxWorld, float tileSize,
		glm::ivec2& outMinChunk, glm::ivec2& outMaxChunk) const {
		glm::ivec2 minTile, maxTile;
		WorldToTileBounds(minWorld, maxWorld, tileSize, minTile, maxTile);
		outMinChunk = GetChunkCoords(minTile);
		outMaxChunk = GetChunkCoords(maxTile);
	}

	// ═════════════════════════════════════════════════════════════════════
	// QUERY OPERATIONS
	// ═════════════════════════════════════════════════════════════════════

	std::vector<entt::entity> TilemapSystem::GetAllTilemaps(entt::registry& registry) const {
		std::vector<entt::entity> tilemaps;

		auto nodeView = registry.view<SceneNodeComponent>();
		for (auto entity : nodeView) {
			const auto& node = nodeView.get<SceneNodeComponent>(entity);
			if (node.name.find("Tilemap") != std::string::npos && !node.children.empty()) {
				// Check if it has layer children
				bool hasLayerChildren = std::any_of(node.children.begin(), node.children.end(),
					[&registry](entt::entity child) {
						return registry.any_of<TilemapLayerComponent>(child);
					});

				if (hasLayerChildren) {
					tilemaps.push_back(entity);
				}
			}
		}

		return tilemaps;
	}

	std::vector<entt::entity> TilemapSystem::GetTilemapLayers(entt::registry& registry, entt::entity tilemap) const {
		std::vector<entt::entity> layers;

		if (auto* tilemapNode = registry.try_get<SceneNodeComponent>(tilemap)) {
			for (entt::entity child : tilemapNode->children) {
				if (registry.any_of<TilemapLayerComponent>(child)) {
					layers.push_back(child);
				}
			}
		}

		return layers;
	}

	entt::entity TilemapSystem::FindCollisionLayer(entt::registry& registry, entt::entity tilemap) const {
		auto layers = GetTilemapLayers(registry, tilemap);

		for (entt::entity layer : layers) {
			if (auto* layerComponent = registry.try_get<TilemapLayerComponent>(layer)) {
				if (layerComponent->hasCollision) {
					return layer;
				}
			}
		}

		return entt::null;
	}

	// ═════════════════════════════════════════════════════════════════════
	// PRIVATE HELPER METHODS
	// ═════════════════════════════════════════════════════════════════════

	entt::entity TilemapSystem::GetOrCreateChunk(entt::registry& registry,
		entt::entity     tilemapLayer,
		const glm::ivec2& chunkCoords)
	{
		// 1)  Return existing chunk if present
		if (auto* layerNode = registry.try_get<SceneNodeComponent>(tilemapLayer)) {
			for (entt::entity child : layerNode->children) {
				if (auto* cc = registry.try_get<TilemapChunkComponent>(child);
					cc && cc->chunkCoords == chunkCoords) {
					return child;
				}
			}
		}

		// 2)  Create new chunk entity
		entt::entity chunk = registry.create();

		registry.emplace<SceneNodeComponent>(chunk, SceneNodeComponent{
			.parent = tilemapLayer,
			.name = "Chunk_" + std::to_string(chunkCoords.x) + "_" + std::to_string(chunkCoords.y)
			});

		const float tileSize = ConfigManager::Get().tileSize;

		registry.emplace<TransformComponent>(chunk, TransformComponent{
			.localPosition = glm::vec2(chunkCoords) * static_cast<float>(chunkSize) * tileSize
			});

		// Build chunk data container
		TilemapChunkComponent comp{
			.chunkCoords = chunkCoords,
			.chunkSize = chunkSize,
			.loaded = true,
			.dirty = false,
			.visible = true
		};
		const size_t total = static_cast<size_t>(chunkSize) * static_cast<size_t>(chunkSize);
		comp.tileIds.resize(total, -1);
		comp.tileData.resize(total, 0);
		registry.emplace<TilemapChunkComponent>(chunk, std::move(comp));

		// Hook into parent node hierarchy
		if (auto* layerNode = registry.try_get<SceneNodeComponent>(tilemapLayer)) {
			layerNode->children.push_back(chunk);
		}

		spdlog::debug("[TilemapSystem] Created chunk ({}, {})", chunkCoords.x, chunkCoords.y);
		return chunk;
	}

	void TilemapSystem::OptimizeChunk(entt::registry& registry, entt::entity chunk) {
		auto* chunkComponent = registry.try_get<TilemapChunkComponent>(chunk);
		if (!chunkComponent || !chunkComponent->dirty) return;

		chunkComponent->instanceCount = std::count_if(chunkComponent->tileIds.begin(),
			chunkComponent->tileIds.end(), [](int id) { return id != -1; });
		chunkComponent->instanceVBO = 0;
		chunkComponent->dirty = false;
	}

	std::unordered_set<uint64_t> TilemapSystem::CalculateRequiredChunks(const glm::vec2& viewCenter, float viewRadius, float tileSize) const {
		std::unordered_set<uint64_t> requiredChunks;

		// Add some padding to the view radius for smooth streaming
		float paddedRadius = viewRadius + (chunkSize * tileSize * 0.5f);

		glm::ivec2 centerChunk = GetChunkCoords(WorldToTilePosition(viewCenter, tileSize));
		int chunkRadius = static_cast<int>(std::ceil(paddedRadius / (chunkSize * tileSize))) + 1;

		for (int cy = centerChunk.y - chunkRadius; cy <= centerChunk.y + chunkRadius; ++cy) {
			for (int cx = centerChunk.x - chunkRadius; cx <= centerChunk.x + chunkRadius; ++cx) {
				// Check if chunk is within the circular view area
				glm::vec2 chunkCenter = glm::vec2(cx * chunkSize + chunkSize / 2, cy * chunkSize + chunkSize / 2) * tileSize;
				float distanceToView = glm::length(chunkCenter - viewCenter);

				if (distanceToView <= paddedRadius) {
					requiredChunks.insert(ChunkCoordsToKey(glm::ivec2{ cx, cy }));
				}
			}
		}

		return requiredChunks;
	}

	uint64_t TilemapSystem::ChunkCoordsToKey(const glm::ivec2& chunkCoords) const {
		return (uint64_t(chunkCoords.x) << 32) | uint32_t(chunkCoords.y);
	}

	glm::ivec2 TilemapSystem::KeyToChunkCoords(uint64_t key) const {
		return glm::ivec2{ int(key >> 32), int(key) };
	}

} // namespace WanderSpire