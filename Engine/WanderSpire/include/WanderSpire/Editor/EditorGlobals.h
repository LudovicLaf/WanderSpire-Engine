#pragma once
#include <unordered_map>
#include "WanderSpire/Editor/TilePaint/TilePalette.h"
#include <memory>
#include <WanderSpire/Editor/CommandHistory.h>

namespace WanderSpire {
	extern std::unordered_map<int, TilePalette> g_tilePalettes;
	extern int g_nextPaletteId;
	extern int g_activePaletteId;
	extern std::unique_ptr<CommandHistory> g_commandHistory;
}
