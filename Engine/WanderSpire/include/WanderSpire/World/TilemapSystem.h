#pragma once
#include <string>
#include <glm/glm.hpp>
#include <entt/entt.hpp>
#include <vector>
#include <unordered_set>

namespace WanderSpire {

	/**
	 * Modern ECS-based tilemap system - the single source of truth for chunk management.
	 * Replaces legacy GridMap2D and ChunkManager systems.
	 */
	class TilemapSystem {
	public:
		static TilemapSystem& GetInstance();

		// ═════════════════════════════════════════════════════════════════════
		// TILEMAP & LAYER MANAGEMENT
		// ═════════════════════════════════════════════════════════════════════

		/// Create a new tilemap entity
		entt::entity CreateTilemap(entt::registry& registry, const std::string& name = "Tilemap");

		/// Create a new tilemap layer within a tilemap
		entt::entity CreateTilemapLayer(entt::registry& registry, entt::entity tilemap, const std::string& layerName);

		// ═════════════════════════════════════════════════════════════════════
		// TILE OPERATIONS
		// ═════════════════════════════════════════════════════════════════════

		/// Set a tile at the given position
		void SetTile(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position, int tileId);

		/// Get a tile at the given position (-1 if no tile)
		int GetTile(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position);

		/// Remove a tile at the given position
		void RemoveTile(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position);

		// ═════════════════════════════════════════════════════════════════════
		// CHUNK MANAGEMENT (replaces ChunkManager functionality)
		// ═════════════════════════════════════════════════════════════════════

		/// Load/create a chunk at the given chunk coordinates
		void LoadChunk(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& chunkCoords);

		/// Unload a chunk at the given chunk coordinates
		void UnloadChunk(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& chunkCoords);

		/// Check if a chunk is loaded
		bool IsChunkLoaded(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& chunkCoords);

		/// Ensure all chunks overlapping the given world bounds are loaded
		void EnsureChunksLoaded(entt::registry& registry, const glm::vec2& minWorldBound, const glm::vec2& maxWorldBound);

		/// Update tilemap streaming based on camera position
		void UpdateTilemapStreaming(entt::registry& registry, const glm::vec2& viewCenter, float viewRadius);

		// ═════════════════════════════════════════════════════════════════════
		// CONFIGURATION
		// ═════════════════════════════════════════════════════════════════════

		/// Set the chunk size (tiles per chunk)
		void SetChunkSize(int size);

		/// Get the current chunk size
		int GetChunkSize() const;

		/// Set the streaming radius for automatic chunk loading/unloading
		void SetStreamingRadius(float radius);

		/// Get the current streaming radius
		float GetStreamingRadius() const;

		// ═════════════════════════════════════════════════════════════════════
		// BULK OPERATIONS
		// ═════════════════════════════════════════════════════════════════════

		/// Flood fill starting from the given position
		void FloodFill(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& startPos, int newTileId);

		/// Fill a rectangular area with the given tile
		void FloodFillArea(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& min, const glm::ivec2& max, int tileId);

		// ═════════════════════════════════════════════════════════════════════
		// COORDINATE CONVERSION
		// ═════════════════════════════════════════════════════════════════════

		/// Convert world position to tile position
		glm::ivec2 WorldToTilePosition(const glm::vec2& worldPos, float tileSize) const;

		/// Convert tile position to world position (center of tile)
		glm::vec2 TileToWorldPosition(const glm::ivec2& tilePos, float tileSize) const;

		/// Get chunk coordinates for the given tile position
		glm::ivec2 GetChunkCoords(const glm::ivec2& tilePos) const;

		/// Convert world bounds to tile bounds
		void WorldToTileBounds(const glm::vec2& minWorld, const glm::vec2& maxWorld, float tileSize,
			glm::ivec2& outMinTile, glm::ivec2& outMaxTile) const;

		/// Convert world bounds to chunk bounds
		void WorldToChunkBounds(const glm::vec2& minWorld, const glm::vec2& maxWorld, float tileSize,
			glm::ivec2& outMinChunk, glm::ivec2& outMaxChunk) const;

		// ═════════════════════════════════════════════════════════════════════
		// QUERY OPERATIONS
		// ═════════════════════════════════════════════════════════════════════

		/// Get all tilemap entities in the registry
		std::vector<entt::entity> GetAllTilemaps(entt::registry& registry) const;

		/// Get all layer entities for a given tilemap
		std::vector<entt::entity> GetTilemapLayers(entt::registry& registry, entt::entity tilemap) const;

		/// Find the first tilemap layer with collision enabled
		entt::entity FindCollisionLayer(entt::registry& registry, entt::entity tilemap) const;

	private:
		int chunkSize = 32;
		float streamingRadius = 1000.0f;

		/// Get or create a chunk at the given chunk coordinates
		entt::entity GetOrCreateChunk(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& chunkCoords);

		/// Optimize a chunk's rendering data
		void OptimizeChunk(entt::registry& registry, entt::entity chunk);

		/// Calculate required chunks for a given view area
		std::unordered_set<uint64_t> CalculateRequiredChunks(const glm::vec2& viewCenter, float viewRadius, float tileSize) const;

		/// Convert chunk coordinates to a unique key
		uint64_t ChunkCoordsToKey(const glm::ivec2& chunkCoords) const;

		/// Convert a unique key back to chunk coordinates
		glm::ivec2 KeyToChunkCoords(uint64_t key) const;
	};

} // namespace WanderSpire