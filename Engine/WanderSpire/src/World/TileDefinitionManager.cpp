#include "WanderSpire/World/TileDefinitionManager.h"
#include <WanderSpire/Editor/TilePaint/TilePalette.h>
#include "WanderSpire/Editor/EditorGlobals.h"


#include <spdlog/spdlog.h>
#include <shared_mutex>

namespace WanderSpire {

	TileDefinitionManager& TileDefinitionManager::GetInstance() {
		static TileDefinitionManager instance;
		return instance;
	}

	void TileDefinitionManager::RegisterTile(int tileId, const std::string& atlasName,
		const std::string& frameName, bool walkable, int collisionType) {
		std::unique_lock lock(m_mutex);

		TileDefinition def;
		def.atlasName = atlasName;
		def.frameName = frameName;
		def.walkable = walkable;
		def.collisionType = collisionType;

		m_definitions[tileId] = std::move(def);

		spdlog::debug("[TileDefinitionManager] Registered tile {} -> {}:{}",
			tileId, atlasName, frameName);
	}

	const TileDefinitionManager::TileDefinition* TileDefinitionManager::GetTileDefinition(int tileId) const {
		std::shared_lock lock(m_mutex);

		auto it = m_definitions.find(tileId);
		if (it != m_definitions.end()) {
			return &it->second;
		}

		// Return default for unknown tiles
		return &m_defaultDefinition;
	}

	void TileDefinitionManager::Clear() {
		std::unique_lock lock(m_mutex);
		m_definitions.clear();
		spdlog::info("[TileDefinitionManager] Cleared all tile definitions");
	}

	void TileDefinitionManager::LoadFromPalette(int paletteId) {
		auto it = g_tilePalettes.find(paletteId);
		if (it == g_tilePalettes.end()) {
			spdlog::warn("[TileDefinitionManager] Palette {} not found", paletteId);
			return;
		}

		const auto& palette = it->second;
		std::unique_lock lock(m_mutex);

		// Extract atlas name from palette
		std::string atlasName = palette.atlasPath;
		if (atlasName.empty()) {
			atlasName = "terrain"; // fallback
		}
		else {
			// Extract filename without extension
			size_t lastSlash = atlasName.find_last_of("/\\");
			if (lastSlash != std::string::npos) {
				atlasName = atlasName.substr(lastSlash + 1);
			}
			size_t lastDot = atlasName.find_last_of('.');
			if (lastDot != std::string::npos) {
				atlasName = atlasName.substr(0, lastDot);
			}
		}

		// Register all tiles from the palette
		for (const auto& tileEntry : palette.tiles) {
			TileDefinition def;
			def.atlasName = atlasName;
			def.frameName = tileEntry.name;
			def.walkable = tileEntry.walkable;
			def.collisionType = tileEntry.collisionType;

			m_definitions[tileEntry.tileId] = std::move(def);
		}

		spdlog::info("[TileDefinitionManager] Loaded {} tile definitions from palette '{}'",
			palette.tiles.size(), palette.name);
	}

	void TileDefinitionManager::SetDefaultDefinition(const std::string& atlasName, const std::string& frameName) {
		std::unique_lock lock(m_mutex);
		m_defaultDefinition.atlasName = atlasName;
		m_defaultDefinition.frameName = frameName;

		spdlog::info("[TileDefinitionManager] Set default tile definition to {}:{}",
			atlasName, frameName);
	}

	size_t TileDefinitionManager::GetTileCount() const {
		std::shared_lock lock(m_mutex);
		return m_definitions.size();
	}

} // namespace WanderSpire