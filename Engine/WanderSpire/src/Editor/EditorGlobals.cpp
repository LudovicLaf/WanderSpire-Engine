#include <unordered_map>
#include "WanderSpire/Editor/TilePaint/TilePalette.h"
#include "WanderSpire/Editor/EditorGlobals.h"
#include <memory>
#include <WanderSpire/Editor/CommandHistory.h>

namespace WanderSpire {
	std::unordered_map<int, TilePalette> g_tilePalettes;
	int g_nextPaletteId = 1;
	int g_activePaletteId = 0;
	std::unique_ptr<CommandHistory> g_commandHistory;
}
