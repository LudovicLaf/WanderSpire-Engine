#pragma once
#include <vector>
#include <string>
#include "WanderSpire/Editor/TilePaint/TilePalette.h"
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct TilePaletteComponent {
		std::vector<TilePalette> palettes;
		int activePaletteIndex = 0;
		int selectedTileIndex = 0;

		const TilePalette::TileEntry* GetSelectedTile() const {
			if (activePaletteIndex >= 0 && activePaletteIndex < palettes.size()) {
				const auto& palette = palettes[activePaletteIndex];
				if (selectedTileIndex >= 0 && selectedTileIndex < palette.tiles.size()) {
					return &palette.tiles[selectedTileIndex];
				}
			}
			return nullptr;
		}
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::TilePaletteComponent,
	FIELD(Int, activePaletteIndex, 0, 100, 1),
	FIELD(Int, selectedTileIndex, 0, 100, 1)
	// You can add FIELD(Container, palettes, ...) if your reflection system supports containers
)
