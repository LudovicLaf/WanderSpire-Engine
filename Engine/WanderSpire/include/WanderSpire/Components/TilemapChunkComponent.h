// WanderSpire/Components/TilemapChunkComponent.h - IMPROVED VERSION
#pragma once
#include <glm/glm.hpp>
#include <vector>
#include <cstdint>
#include "WanderSpire/Core/ReflectionMacros.h"
#include <spdlog/spdlog.h>

#include <nlohmann/json.hpp>

namespace WanderSpire {

	struct TilemapChunkComponent {
		glm::ivec2 chunkCoords{ 0, 0 };
		int chunkSize = 32;          // Tiles per chunk

		// Chunk state
		bool loaded = false;
		bool dirty = false;
		bool visible = true;

		// Tile data
		std::vector<int> tileIds;    // Flat array of tile IDs
		std::vector<uint32_t> tileData; // Additional per-tile data

		// Rendering optimization
		uint32_t instanceVBO = 0;    // GPU buffer for instanced rendering
		int instanceCount = 0;
	};

	inline void to_json(nlohmann::json& j, const TilemapChunkComponent& chunk) {
		j = nlohmann::json{
			{"chunkCoords", {chunk.chunkCoords.x, chunk.chunkCoords.y}},
			{"chunkSize", chunk.chunkSize},
			{"loaded", chunk.loaded},
			{"dirty", chunk.dirty},
			{"visible", chunk.visible},
			{"instanceCount", chunk.instanceCount},
			{"tileIds", chunk.tileIds},
			{"tileData", chunk.tileData}
		};

		spdlog::debug("[TilemapChunkComponent::to_json] Serializing chunk ({},{}) with {} tiles, {} data entries",
			chunk.chunkCoords.x, chunk.chunkCoords.y,
			chunk.tileIds.size(), chunk.tileData.size());
	}

	inline void from_json(const nlohmann::json& j, TilemapChunkComponent& chunk) {
		spdlog::debug("[TilemapChunkComponent::from_json] Starting deserialization");

		try {
			// Load chunk coordinates
			if (j.contains("chunkCoords") && j["chunkCoords"].is_array() && j["chunkCoords"].size() >= 2) {
				chunk.chunkCoords.x = j["chunkCoords"][0].get<int>();
				chunk.chunkCoords.y = j["chunkCoords"][1].get<int>();
				spdlog::debug("[TilemapChunkComponent::from_json] Loaded chunk coords: ({}, {})",
					chunk.chunkCoords.x, chunk.chunkCoords.y);
			}
			else {
				spdlog::warn("[TilemapChunkComponent::from_json] Invalid or missing chunkCoords");
				chunk.chunkCoords = { 0, 0 };
			}

			// Load chunk size
			chunk.chunkSize = j.value("chunkSize", 32);
			chunk.loaded = j.value("loaded", false);
			chunk.dirty = j.value("dirty", false);
			chunk.visible = j.value("visible", true);
			chunk.instanceCount = j.value("instanceCount", 0);

			spdlog::debug("[TilemapChunkComponent::from_json] Basic properties loaded, chunkSize: {}", chunk.chunkSize);

			// Load tile data with validation
			if (j.contains("tileIds") && j["tileIds"].is_array()) {
				try {
					chunk.tileIds = j["tileIds"].get<std::vector<int>>();
					spdlog::debug("[TilemapChunkComponent::from_json] Loaded {} tile IDs from JSON", chunk.tileIds.size());

					// Sample some tiles for debugging
					if (!chunk.tileIds.empty()) {
						size_t sampleCount = std::min(chunk.tileIds.size(), size_t(5));
						std::string sampleStr = "First tiles: ";
						for (size_t i = 0; i < sampleCount; ++i) {
							sampleStr += std::to_string(chunk.tileIds[i]) + " ";
						}
						spdlog::debug("[TilemapChunkComponent::from_json] {}", sampleStr);
					}
				}
				catch (const std::exception& e) {
					spdlog::error("[TilemapChunkComponent::from_json] Failed to parse tileIds: {}", e.what());
					// Initialize empty tile array as fallback
					chunk.tileIds.clear();
					chunk.tileIds.resize(chunk.chunkSize * chunk.chunkSize, -1);
				}
			}
			else {
				spdlog::warn("[TilemapChunkComponent::from_json] No tileIds array found, creating empty");
				// Initialize empty tile array if not present
				chunk.tileIds.clear();
				chunk.tileIds.resize(chunk.chunkSize * chunk.chunkSize, -1);
			}

			// Load tile data
			if (j.contains("tileData") && j["tileData"].is_array()) {
				try {
					chunk.tileData = j["tileData"].get<std::vector<uint32_t>>();
					spdlog::debug("[TilemapChunkComponent::from_json] Loaded {} tile data entries", chunk.tileData.size());
				}
				catch (const std::exception& e) {
					spdlog::error("[TilemapChunkComponent::from_json] Failed to parse tileData: {}", e.what());
					chunk.tileData.clear();
					chunk.tileData.resize(chunk.chunkSize * chunk.chunkSize, 0);
				}
			}
			else {
				spdlog::debug("[TilemapChunkComponent::from_json] No tileData array found, creating empty");
				chunk.tileData.clear();
				chunk.tileData.resize(chunk.chunkSize * chunk.chunkSize, 0);
			}

			// Validate sizes
			size_t expectedSize = static_cast<size_t>(chunk.chunkSize * chunk.chunkSize);
			if (chunk.tileIds.size() != expectedSize) {
				spdlog::warn("[TilemapChunkComponent::from_json] TileIds size mismatch: expected {}, got {}, resizing",
					expectedSize, chunk.tileIds.size());
				chunk.tileIds.resize(expectedSize, -1);
			}

			if (chunk.tileData.size() != expectedSize) {
				spdlog::warn("[TilemapChunkComponent::from_json] TileData size mismatch: expected {}, got {}, resizing",
					expectedSize, chunk.tileData.size());
				chunk.tileData.resize(expectedSize, 0);
			}

			spdlog::debug("[TilemapChunkComponent::from_json] Successfully deserialized chunk ({},{}) with {} tiles",
				chunk.chunkCoords.x, chunk.chunkCoords.y, chunk.tileIds.size());

		}
		catch (const std::exception& e) {
			spdlog::error("[TilemapChunkComponent::from_json] Exception during deserialization: {}", e.what());

			// Create a valid fallback chunk
			chunk.chunkCoords = { 0, 0 };
			chunk.chunkSize = 32;
			chunk.loaded = false;
			chunk.dirty = true;
			chunk.visible = true;
			chunk.instanceCount = 0;

			size_t expectedSize = static_cast<size_t>(chunk.chunkSize * chunk.chunkSize);
			chunk.tileIds.clear();
			chunk.tileIds.resize(expectedSize, -1);
			chunk.tileData.clear();
			chunk.tileData.resize(expectedSize, 0);

			spdlog::error("[TilemapChunkComponent::from_json] Created fallback chunk with {} tiles", expectedSize);
			throw; // Re-throw to let the caller handle the error
		}
	}
} // namespace WanderSpire