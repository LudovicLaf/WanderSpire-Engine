#pragma once
#include <unordered_map>
#include <string>
#include <mutex>
#include <shared_mutex>

namespace WanderSpire {

	/**
	 * Manages tile ID to texture/atlas frame mappings for runtime rendering.
	 * Integrates with the existing TilePalette system to provide fast lookups
	 * during rendering without hardcoded values.
	 */
	class TileDefinitionManager {
	public:
		struct TileDefinition {
			std::string atlasName;      // e.g. "terrain"
			std::string frameName;      // e.g. "grass", "sand"
			bool walkable = true;
			int collisionType = 0;
		};

		static TileDefinitionManager& GetInstance();

		/// Register a tile definition (called from managed API)
		void RegisterTile(int tileId, const std::string& atlasName, const std::string& frameName,
			bool walkable = true, int collisionType = 0);

		/// Get tile definition for rendering
		const TileDefinition* GetTileDefinition(int tileId) const;

		/// Clear all definitions
		void Clear();

		/// Load definitions from a palette (integrates with existing TilePalette system)
		void LoadFromPalette(int paletteId);

		/// Set default fallback definition
		void SetDefaultDefinition(const std::string& atlasName, const std::string& frameName);

		/// Get registered tile count
		size_t GetTileCount() const;

	private:
		TileDefinitionManager() = default;

		mutable std::shared_mutex m_mutex;
		std::unordered_map<int, TileDefinition> m_definitions;
		TileDefinition m_defaultDefinition{ "terrain", "grass", true, 0 };
	};

} // namespace WanderSpire