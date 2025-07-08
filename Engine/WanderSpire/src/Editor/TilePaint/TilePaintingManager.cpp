#include "WanderSpire/Editor/TilePaint/TilePaintingManager.h"
#include "WanderSpire/World/TilemapSystem.h"
#include "WanderSpire/World/TileDefinitionManager.h"
#include "WanderSpire/Editor/CommandHistory.h"
#include "WanderSpire/Editor/Commands/TilemapCommands.h"
#include "WanderSpire/Components/TilePaletteComponent.h"
#include "WanderSpire/Components/TileBrushComponent.h"
#include "WanderSpire/Components/AutoTilingComponent.h"
#include "WanderSpire/Editor/EditorGlobals.h"


#include <fstream>
#include <algorithm>
#include <queue>
#include <unordered_set>
#include <random>
#include <nlohmann/json.hpp>
#include <spdlog/spdlog.h>
#include <filesystem>

namespace WanderSpire {

	TilePaintingManager& TilePaintingManager::GetInstance() {
		static TilePaintingManager instance;
		return instance;
	}

	// ═════════════════════════════════════════════════════════════════════
	// PALETTE MANAGEMENT
	// ═════════════════════════════════════════════════════════════════════

	void TilePaintingManager::LoadPalette(const std::string& palettePath) {
		namespace fs = std::filesystem;

		if (!fs::exists(palettePath)) {
			spdlog::error("[TilePainting] Palette file not found: {}", palettePath);
			return;
		}

		std::ifstream file(palettePath);
		if (!file.is_open()) {
			spdlog::error("[TilePainting] Failed to open palette file: {}", palettePath);
			return;
		}

		try {
			nlohmann::json j;
			file >> j;

			TilePalette palette;
			palette.name = j.value("name", "Unnamed Palette");
			palette.atlasPath = j.value("atlasPath", "");

			// Support both old and new format
			if (j.contains("tileWidth") && j.contains("tileHeight")) {
				palette.tileSize = glm::ivec2{ j.value("tileWidth", 32), j.value("tileHeight", 32) };
			}
			else {
				palette.tileSize = glm::ivec2{ j.value("tileSize", 32) };
			}

			palette.columns = j.value("columns", 8);
			palette.categories = j.value("categories", std::vector<std::string>{"Default"});

			// Load tile definitions
			if (j.contains("tiles")) {
				for (const auto& tileJson : j["tiles"]) {
					TilePalette::TileEntry tile;
					tile.tileId = tileJson.value("id", 0);
					tile.name = tileJson.value("name", "Tile_" + std::to_string(tile.tileId));
					tile.assetPath = tileJson.value("assetPath", "");

					// Handle atlas position
					if (tileJson.contains("atlasPosition")) {
						auto pos = tileJson["atlasPosition"];
						tile.atlasPosition = glm::ivec2{ pos.value("x", 0), pos.value("y", 0) };
					}
					else {
						tile.atlasPosition = glm::ivec2{
							tileJson.value("atlasX", 0),
							tileJson.value("atlasY", 0)
						};
					}

					tile.walkable = tileJson.value("walkable", true);
					tile.collisionType = tileJson.value("collisionType", 0);
					tile.weight = tileJson.value("weight", 1.0f);
					tile.canRotate = tileJson.value("canRotate", false);
					tile.canFlip = tileJson.value("canFlip", false);

					// Load variants
					if (tileJson.contains("variants")) {
						tile.variants = tileJson["variants"].get<std::vector<int>>();
					}

					palette.tiles.push_back(tile);

					// Register with TileDefinitionManager for rendering
					auto& tileDefManager = TileDefinitionManager::GetInstance();

					// Extract atlas name from path
					std::string atlasName = palette.atlasPath;
					if (!atlasName.empty()) {
						size_t lastSlash = atlasName.find_last_of("/\\");
						if (lastSlash != std::string::npos) {
							atlasName = atlasName.substr(lastSlash + 1);
						}
						size_t lastDot = atlasName.find_last_of('.');
						if (lastDot != std::string::npos) {
							atlasName = atlasName.substr(0, lastDot);
						}
					}
					else {
						atlasName = "terrain"; // fallback
					}

					tileDefManager.RegisterTile(tile.tileId, atlasName, tile.name,
						tile.walkable, tile.collisionType);
				}
			}

			// Add to loaded palettes and replace if exists
			auto it = std::find_if(loadedPalettes.begin(), loadedPalettes.end(),
				[&palette](const TilePalette& p) { return p.name == palette.name; });

			if (it != loadedPalettes.end()) {
				*it = std::move(palette);
			}
			else {
				loadedPalettes.push_back(std::move(palette));
			}

			spdlog::info("[TilePainting] Loaded palette '{}' with {} tiles from {}",
				palette.name, palette.tiles.size(), palettePath);

		}
		catch (const std::exception& e) {
			spdlog::error("[TilePainting] Failed to parse palette file '{}': {}", palettePath, e.what());
		}
	}

	void TilePaintingManager::SavePalette(const std::string& palettePath, const TilePalette& palette) {
		namespace fs = std::filesystem;

		// Ensure directory exists
		fs::path path(palettePath);
		fs::create_directories(path.parent_path());

		nlohmann::json j;
		j["name"] = palette.name;
		j["atlasPath"] = palette.atlasPath;
		j["tileWidth"] = palette.tileSize.x;
		j["tileHeight"] = palette.tileSize.y;
		j["columns"] = palette.columns;
		j["categories"] = palette.categories;
		j["version"] = "1.0";
		j["created"] = std::time(nullptr);

		nlohmann::json tilesJson = nlohmann::json::array();
		for (const auto& tile : palette.tiles) {
			nlohmann::json tileJson;
			tileJson["id"] = tile.tileId;
			tileJson["name"] = tile.name;
			tileJson["assetPath"] = tile.assetPath;
			tileJson["atlasPosition"] = {
				{"x", tile.atlasPosition.x},
				{"y", tile.atlasPosition.y}
			};
			tileJson["walkable"] = tile.walkable;
			tileJson["collisionType"] = tile.collisionType;
			tileJson["weight"] = tile.weight;
			tileJson["canRotate"] = tile.canRotate;
			tileJson["canFlip"] = tile.canFlip;

			if (!tile.variants.empty()) {
				tileJson["variants"] = tile.variants;
			}

			tilesJson.push_back(tileJson);
		}
		j["tiles"] = tilesJson;

		std::ofstream file(palettePath);
		if (file.is_open()) {
			file << j.dump(2);
			spdlog::info("[TilePainting] Saved palette '{}' to {}", palette.name, palettePath);
		}
		else {
			spdlog::error("[TilePainting] Failed to save palette to {}", palettePath);
		}
	}

	void TilePaintingManager::CreatePalette(const std::string& name, const std::string& atlasPath) {
		TilePalette palette;
		palette.name = name;
		palette.atlasPath = atlasPath;
		palette.tileSize = glm::ivec2{ 32, 32 };
		palette.columns = 8;
		palette.categories.push_back("Default");

		loadedPalettes.push_back(std::move(palette));
		spdlog::info("[TilePainting] Created new palette '{}'", name);
	}

	// ═════════════════════════════════════════════════════════════════════
	// BRUSH MANAGEMENT
	// ═════════════════════════════════════════════════════════════════════

	void TilePaintingManager::SetActiveBrush(const TileBrush& brush) {
		activeBrush = brush;

		// Validate brush settings
		activeBrush.size = std::max(1, activeBrush.size);
		activeBrush.opacity = std::clamp(activeBrush.opacity, 0.0f, 1.0f);
		activeBrush.randomStrength = std::clamp(activeBrush.randomStrength, 0.0f, 1.0f);

		spdlog::debug("[TilePainting] Set active brush - type: {}, size: {}, blend: {}",
			static_cast<int>(brush.type), brush.size, static_cast<int>(brush.blendMode));
	}

	const TileBrush& TilePaintingManager::GetActiveBrush() const {
		return activeBrush;
	}

	// ═════════════════════════════════════════════════════════════════════
	// PAINTING OPERATIONS
	// ═════════════════════════════════════════════════════════════════════

	void TilePaintingManager::BeginPaint(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position) {
		if (isPainting) {
			EndPaint(registry, tilemapLayer);
		}

		if (!registry.valid(tilemapLayer)) {
			spdlog::warn("[TilePainting] Invalid tilemap layer for begin paint");
			return;
		}

		isPainting = true;
		currentStroke.clear();
		currentStroke.push_back(position);
		paintStartPosition = position;

		// Get selected tile from active palette
		int selectedTileId = GetSelectedTileId();
		if (selectedTileId == -1) {
			spdlog::warn("[TilePainting] No tile selected for painting");
			return;
		}

		// Apply initial paint based on brush type
		std::vector<glm::ivec2> positions;
		switch (activeBrush.type) {
		case TileBrush::BrushType::Single:
			positions = GetBrushPositions(position);
			break;
		case TileBrush::BrushType::Circle:
			positions = GetCirclePositions(position, activeBrush.size, true);
			break;
		case TileBrush::BrushType::Pattern:
			positions = GetPatternPositions(position);
			break;
		default:
			positions.push_back(position);
			break;
		}

		// Apply paint to positions
		ApplyPaintToPositions(registry, tilemapLayer, positions, selectedTileId);

		spdlog::debug("[TilePainting] Began paint operation at ({}, {}) with {} positions",
			position.x, position.y, positions.size());
	}

	void TilePaintingManager::ContinuePaint(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position) {
		if (!isPainting || !registry.valid(tilemapLayer)) {
			return;
		}

		// Check if position changed significantly
		if (!currentStroke.empty()) {
			glm::ivec2 lastPos = currentStroke.back();
			int distance = std::abs(position.x - lastPos.x) + std::abs(position.y - lastPos.y);
			if (distance < 1) {
				return; // Too close to last position
			}
		}

		currentStroke.push_back(position);

		int selectedTileId = GetSelectedTileId();
		if (selectedTileId == -1) return;

		std::vector<glm::ivec2> positions;

		switch (activeBrush.type) {
		case TileBrush::BrushType::Single:
		case TileBrush::BrushType::Circle:
			positions = GetBrushPositions(position);
			break;
		case TileBrush::BrushType::Line:
			// For line brush, paint from start to current position
			positions = GetLinePositions(paintStartPosition, position);
			break;
		case TileBrush::BrushType::Pattern:
			positions = GetPatternPositions(position);
			break;
		default:
			positions.push_back(position);
			break;
		}

		ApplyPaintToPositions(registry, tilemapLayer, positions, selectedTileId);
	}

	void TilePaintingManager::EndPaint(entt::registry& registry, entt::entity tilemapLayer) {
		if (!isPainting) return;

		isPainting = false;

		// Apply auto-tiling if enabled
		if (!autoTileSets.empty() && !currentStroke.empty()) {
			ApplyAutoTiling(registry, tilemapLayer, currentStroke);
		}

		// Create command for undo/redo
		if (!currentStroke.empty()) {
			// This would need to be integrated with the command system
			// For now, just notify callbacks
			std::vector<int> tileIds(currentStroke.size(), GetSelectedTileId());
			NotifyPaintCallbacks(currentStroke, tileIds);
		}

		spdlog::debug("[TilePainting] Ended paint operation, stroke length: {}", currentStroke.size());
		currentStroke.clear();
	}

	// ═════════════════════════════════════════════════════════════════════
	// SPECIALIZED PAINTING OPERATIONS
	// ═════════════════════════════════════════════════════════════════════

	void TilePaintingManager::PaintLine(entt::registry& registry, entt::entity tilemapLayer,
		const glm::ivec2& start, const glm::ivec2& end) {

		auto positions = GetLinePositions(start, end);
		int tileId = GetSelectedTileId();

		if (tileId != -1) {
			ApplyPaintToPositions(registry, tilemapLayer, positions, tileId);

			// Apply auto-tiling
			if (!autoTileSets.empty()) {
				ApplyAutoTiling(registry, tilemapLayer, positions);
			}
		}

		spdlog::debug("[TilePainting] Painted line from ({}, {}) to ({}, {}) with {} positions",
			start.x, start.y, end.x, end.y, positions.size());
	}

	void TilePaintingManager::PaintRectangle(entt::registry& registry, entt::entity tilemapLayer,
		const glm::ivec2& min, const glm::ivec2& max, bool filled) {

		std::vector<glm::ivec2> positions;
		int tileId = GetSelectedTileId();

		if (tileId == -1) return;

		if (filled) {
			for (int y = min.y; y <= max.y; ++y) {
				for (int x = min.x; x <= max.x; ++x) {
					positions.push_back({ x, y });
				}
			}
		}
		else {
			// Paint outline only
			for (int x = min.x; x <= max.x; ++x) {
				positions.push_back({ x, min.y });
				positions.push_back({ x, max.y });
			}
			for (int y = min.y + 1; y < max.y; ++y) {
				positions.push_back({ min.x, y });
				positions.push_back({ max.x, y });
			}
		}

		ApplyPaintToPositions(registry, tilemapLayer, positions, tileId);

		if (!autoTileSets.empty()) {
			ApplyAutoTiling(registry, tilemapLayer, positions);
		}

		spdlog::debug("[TilePainting] Painted {} rectangle from ({}, {}) to ({}, {}) with {} positions",
			filled ? "filled" : "outline", min.x, min.y, max.x, max.y, positions.size());
	}

	void TilePaintingManager::PaintCircle(entt::registry& registry, entt::entity tilemapLayer,
		const glm::ivec2& center, int radius, bool filled) {

		auto positions = GetCirclePositions(center, radius, filled);
		int tileId = GetSelectedTileId();

		if (tileId != -1) {
			ApplyPaintToPositions(registry, tilemapLayer, positions, tileId);

			if (!autoTileSets.empty()) {
				ApplyAutoTiling(registry, tilemapLayer, positions);
			}
		}

		spdlog::debug("[TilePainting] Painted {} circle at ({}, {}) radius {} with {} positions",
			filled ? "filled" : "outline", center.x, center.y, radius, positions.size());
	}

	void TilePaintingManager::FloodFillWithBrush(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& startPos) {
		int newTileId = GetSelectedTileId();
		if (newTileId == -1) return;

		TilemapSystem::GetInstance().FloodFill(registry, tilemapLayer, startPos, newTileId);

		spdlog::debug("[TilePainting] Flood fill at ({}, {}) with tile {}",
			startPos.x, startPos.y, newTileId);
	}

	// ═════════════════════════════════════════════════════════════════════
	// PREVIEW AND SAMPLING
	// ═════════════════════════════════════════════════════════════════════

	std::vector<glm::ivec2> TilePaintingManager::GetPaintPreview(entt::registry& registry,
		entt::entity tilemapLayer, const glm::ivec2& position) const {

		if (!activeBrush.showPreview) {
			return {};
		}

		switch (activeBrush.type) {
		case TileBrush::BrushType::Single:
			return GetBrushPositions(position);
		case TileBrush::BrushType::Circle:
			return GetCirclePositions(position, activeBrush.size, true);
		case TileBrush::BrushType::Pattern:
			return GetPatternPositions(position);
		case TileBrush::BrushType::Line:
			if (isPainting && !currentStroke.empty()) {
				return GetLinePositions(currentStroke[0], position);
			}
			return { position };
		case TileBrush::BrushType::Rectangle:
			if (isPainting && !currentStroke.empty()) {
				glm::ivec2 start = currentStroke[0];
				glm::ivec2 min = { std::min(start.x, position.x), std::min(start.y, position.y) };
				glm::ivec2 max = { std::max(start.x, position.x), std::max(start.y, position.y) };

				std::vector<glm::ivec2> preview;
				for (int y = min.y; y <= max.y; ++y) {
					for (int x = min.x; x <= max.x; ++x) {
						preview.push_back({ x, y });
					}
				}
				return preview;
			}
			return { position };
		default:
			return { position };
		}
	}

	void TilePaintingManager::ClearPreview() {
		previewPositions.clear();
	}

	int TilePaintingManager::SampleTile(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position) {
		int tileId = TilemapSystem::GetInstance().GetTile(registry, tilemapLayer, position);

		// Update selected tile in active palette if valid
		if (tileId != -1) {
			SetSelectedTile(tileId);
		}

		spdlog::debug("[TilePainting] Sampled tile {} at ({}, {})", tileId, position.x, position.y);
		return tileId;
	}

	void TilePaintingManager::SetSelectedTile(int tileId) {
		selectedTileId = tileId;

		// Find this tile in the active palette and update selection
		if (g_activePaletteId > 0) {
			auto it = g_tilePalettes.find(g_activePaletteId);
			if (it != g_tilePalettes.end()) {
				auto& palette = it->second;
				for (size_t i = 0; i < palette.tiles.size(); ++i) {
					if (palette.tiles[i].tileId == tileId) {
						selectedTileIndex = static_cast<int>(i);
						break;
					}
				}
			}
		}

		spdlog::debug("[TilePainting] Set selected tile to {}", tileId);
	}

	// ═════════════════════════════════════════════════════════════════════
	// AUTO-TILING SYSTEM
	// ═════════════════════════════════════════════════════════════════════

	void TilePaintingManager::ApplyAutoTiling(entt::registry& registry, entt::entity tilemapLayer,
		const std::vector<glm::ivec2>& positions) {

		if (autoTileSets.empty()) return;

		// Expand affected area to include neighbors
		std::unordered_set<uint64_t> affectedPositions;

		for (const auto& pos : positions) {
			affectedPositions.insert(HashPosition(pos));

			// Add 8-connected neighbors
			for (int dy = -1; dy <= 1; ++dy) {
				for (int dx = -1; dx <= 1; ++dx) {
					glm::ivec2 neighbor = pos + glm::ivec2{ dx, dy };
					affectedPositions.insert(HashPosition(neighbor));
				}
			}
		}

		// Apply auto-tiling rules with priority sorting
		for (const auto& tileSet : autoTileSets) {
			if (!tileSet.enabled) continue;

			// Sort rules by priority (higher first)
			auto sortedRules = tileSet.rules;
			std::sort(sortedRules.begin(), sortedRules.end(),
				[](const AutoTileRule& a, const AutoTileRule& b) {
					return a.priority > b.priority;
				});

			for (uint64_t key : affectedPositions) {
				glm::ivec2 pos = UnhashPosition(key);

				for (const auto& rule : sortedRules) {
					if (MatchesAutoTileRule(registry, tilemapLayer, pos, rule)) {
						TilemapSystem::GetInstance().SetTile(registry, tilemapLayer, pos, rule.resultTileId);
						break; // First matching rule wins
					}
				}
			}
		}

		spdlog::debug("[TilePainting] Applied auto-tiling to {} positions", affectedPositions.size());
	}

	void TilePaintingManager::RegisterAutoTileSet(const AutoTileSet& tileSet) {
		// Replace existing set with same name or add new
		auto it = std::find_if(autoTileSets.begin(), autoTileSets.end(),
			[&tileSet](const AutoTileSet& existing) {
				return existing.name == tileSet.name;
			});

		if (it != autoTileSets.end()) {
			*it = tileSet;
		}
		else {
			autoTileSets.push_back(tileSet);
		}

		spdlog::info("[TilePainting] Registered auto-tile set '{}' with {} rules",
			tileSet.name, tileSet.rules.size());
	}

	// ═════════════════════════════════════════════════════════════════════
	// PATTERN SUPPORT
	// ═════════════════════════════════════════════════════════════════════

	void TilePaintingManager::LoadPattern(const std::string& patternPath) {
		std::ifstream file(patternPath);
		if (!file.is_open()) {
			spdlog::error("[TilePainting] Failed to open pattern file: {}", patternPath);
			return;
		}

		try {
			nlohmann::json j;
			file >> j;

			if (j.contains("pattern") && j["pattern"].is_array()) {
				std::vector<std::vector<int>> pattern;
				for (const auto& row : j["pattern"]) {
					if (row.is_array()) {
						pattern.push_back(row.get<std::vector<int>>());
					}
				}

				activeBrush.pattern = std::move(pattern);
				activeBrush.patternSize = {
					static_cast<int>(activeBrush.pattern.empty() ? 0 : activeBrush.pattern[0].size()),
					static_cast<int>(activeBrush.pattern.size())
				};

				spdlog::info("[TilePainting] Loaded pattern {}x{} from {}",
					activeBrush.patternSize.x, activeBrush.patternSize.y, patternPath);
			}
		}
		catch (const std::exception& e) {
			spdlog::error("[TilePainting] Failed to parse pattern file: {}", e.what());
		}
	}

	void TilePaintingManager::SavePattern(const std::string& patternPath,
		const std::vector<std::vector<int>>& pattern) {

		nlohmann::json j;
		j["pattern"] = pattern;
		j["version"] = "1.0";
		j["created"] = std::time(nullptr);

		std::ofstream file(patternPath);
		if (file.is_open()) {
			file << j.dump(2);
			spdlog::info("[TilePainting] Saved pattern to {}", patternPath);
		}
		else {
			spdlog::error("[TilePainting] Failed to save pattern to {}", patternPath);
		}
	}

	void TilePaintingManager::PaintPattern(entt::registry& registry, entt::entity tilemapLayer,
		const glm::ivec2& position, const std::vector<std::vector<int>>& pattern) {

		if (pattern.empty()) return;

		std::vector<glm::ivec2> positions;
		std::vector<int> tileIds;

		for (int y = 0; y < static_cast<int>(pattern.size()); ++y) {
			const auto& row = pattern[y];
			for (int x = 0; x < static_cast<int>(row.size()); ++x) {
				if (row[x] != -1) { // -1 means skip this position
					positions.push_back(position + glm::ivec2{ x, y });
					tileIds.push_back(row[x]);
				}
			}
		}

		// Apply pattern
		for (size_t i = 0; i < positions.size(); ++i) {
			TilemapSystem::GetInstance().SetTile(registry, tilemapLayer, positions[i], tileIds[i]);
		}

		if (!autoTileSets.empty()) {
			ApplyAutoTiling(registry, tilemapLayer, positions);
		}

		spdlog::debug("[TilePainting] Applied pattern at ({}, {}) with {} tiles",
			position.x, position.y, positions.size());
	}

	// ═════════════════════════════════════════════════════════════════════
	// CALLBACKS AND EVENTS
	// ═════════════════════════════════════════════════════════════════════

	void TilePaintingManager::RegisterPaintCallback(PaintCallback callback) {
		paintCallbacks.push_back(std::move(callback));
	}

	// ═════════════════════════════════════════════════════════════════════
	// PRIVATE HELPER METHODS
	// ═════════════════════════════════════════════════════════════════════

	std::vector<glm::ivec2> TilePaintingManager::GetBrushPositions(const glm::ivec2& center) const {
		std::vector<glm::ivec2> positions;

		switch (activeBrush.type) {
		case TileBrush::BrushType::Single:
			positions.push_back(center);
			break;

		case TileBrush::BrushType::Circle:
			positions = GetCirclePositions(center, activeBrush.size, true);
			break;

		case TileBrush::BrushType::Rectangle: {
			int halfSize = activeBrush.size / 2;
			for (int y = -halfSize; y <= halfSize; ++y) {
				for (int x = -halfSize; x <= halfSize; ++x) {
					positions.push_back(center + glm::ivec2{ x, y });
				}
			}
			break;
		}

		case TileBrush::BrushType::Pattern:
			positions = GetPatternPositions(center);
			break;

		default:
			positions.push_back(center);
			break;
		}

		return positions;
	}

	std::vector<glm::ivec2> TilePaintingManager::GetLinePositions(const glm::ivec2& start, const glm::ivec2& end) const {
		std::vector<glm::ivec2> positions;

		// Bresenham's line algorithm
		int dx = std::abs(end.x - start.x);
		int dy = std::abs(end.y - start.y);
		int sx = (start.x < end.x) ? 1 : -1;
		int sy = (start.y < end.y) ? 1 : -1;
		int err = dx - dy;

		glm::ivec2 current = start;

		while (true) {
			positions.push_back(current);

			if (current == end) break;

			int e2 = 2 * err;
			if (e2 > -dy) {
				err -= dy;
				current.x += sx;
			}
			if (e2 < dx) {
				err += dx;
				current.y += sy;
			}
		}

		return positions;
	}

	std::vector<glm::ivec2> TilePaintingManager::GetCirclePositions(const glm::ivec2& center, int radius, bool filled) const {
		std::vector<glm::ivec2> positions;
		int r2 = radius * radius;

		if (filled) {
			for (int y = -radius; y <= radius; ++y) {
				for (int x = -radius; x <= radius; ++x) {
					if (x * x + y * y <= r2) {
						positions.push_back(center + glm::ivec2{ x, y });
					}
				}
			}
		}
		else {
			// Midpoint circle algorithm for outline
			int x = radius;
			int y = 0;
			int err = 0;

			while (x >= y) {
				positions.push_back(center + glm::ivec2{ x,  y });
				positions.push_back(center + glm::ivec2{ y,  x });
				positions.push_back(center + glm::ivec2{ -y,  x });
				positions.push_back(center + glm::ivec2{ -x,  y });
				positions.push_back(center + glm::ivec2{ -x, -y });
				positions.push_back(center + glm::ivec2{ -y, -x });
				positions.push_back(center + glm::ivec2{ y, -x });
				positions.push_back(center + glm::ivec2{ x, -y });

				if (err <= 0) {
					y += 1;
					err += 2 * y + 1;
				}
				if (err > 0) {
					x -= 1;
					err -= 2 * x + 1;
				}
			}
		}

		return positions;
	}

	std::vector<glm::ivec2> TilePaintingManager::GetPatternPositions(const glm::ivec2& center) const {
		std::vector<glm::ivec2> positions;

		if (activeBrush.pattern.empty()) {
			positions.push_back(center);
			return positions;
		}

		int offsetX = activeBrush.patternSize.x / 2;
		int offsetY = activeBrush.patternSize.y / 2;

		for (int y = 0; y < activeBrush.patternSize.y; ++y) {
			for (int x = 0; x < activeBrush.patternSize.x; ++x) {
				if (y < static_cast<int>(activeBrush.pattern.size()) &&
					x < static_cast<int>(activeBrush.pattern[y].size()) &&
					activeBrush.pattern[y][x] != -1) {
					positions.push_back(center + glm::ivec2{ x - offsetX, y - offsetY });
				}
			}
		}

		return positions;
	}

	int TilePaintingManager::SelectTileVariant(int baseTileId) const {
		if (!activeBrush.randomize || activeBrush.randomStrength <= 0.0f) {
			return baseTileId;
		}

		// Find tile in active palette and use its variants
		if (g_activePaletteId > 0) {
			auto it = g_tilePalettes.find(g_activePaletteId);
			if (it != g_tilePalettes.end()) {
				const auto& palette = it->second;

				// Find the base tile
				for (const auto& tile : palette.tiles) {
					if (tile.tileId == baseTileId && !tile.variants.empty()) {
						static std::random_device rd;
						static std::mt19937 gen(rd());
						std::uniform_real_distribution<float> dis(0.0f, 1.0f);

						if (dis(gen) < activeBrush.randomStrength) {
							std::uniform_int_distribution<int> variantDis(0, static_cast<int>(tile.variants.size() - 1));
							return tile.variants[variantDis(gen)];
						}
						break;
					}
				}
			}
		}

		return baseTileId;
	}

	void TilePaintingManager::ApplyPaintToPositions(entt::registry& registry, entt::entity tilemapLayer,
		const std::vector<glm::ivec2>& positions, int baseTileId) {

		auto& tilemapSystem = TilemapSystem::GetInstance();

		for (const auto& pos : positions) {
			int tileId = baseTileId;

			// Apply randomization if enabled
			if (activeBrush.randomize) {
				tileId = SelectTileVariant(baseTileId);
			}

			// Apply blend mode
			switch (activeBrush.blendMode) {
			case TileBrush::BlendMode::Replace:
				tilemapSystem.SetTile(registry, tilemapLayer, pos, tileId);
				break;
			case TileBrush::BlendMode::Add:
				// Only paint if current tile is empty
				if (tilemapSystem.GetTile(registry, tilemapLayer, pos) == -1) {
					tilemapSystem.SetTile(registry, tilemapLayer, pos, tileId);
				}
				break;
			case TileBrush::BlendMode::Subtract:
				// Remove tile (set to -1)
				tilemapSystem.SetTile(registry, tilemapLayer, pos, -1);
				break;
			case TileBrush::BlendMode::Overlay:
				// Apply with opacity (simplified - just replace for now)
				if (activeBrush.opacity >= 1.0f) {
					tilemapSystem.SetTile(registry, tilemapLayer, pos, tileId);
				}
				break;
			}
		}
	}

	void TilePaintingManager::NotifyPaintCallbacks(const std::vector<glm::ivec2>& positions, const std::vector<int>& tileIds) {
		for (auto& callback : paintCallbacks) {
			try {
				callback(positions, tileIds);
			}
			catch (const std::exception& e) {
				spdlog::error("[TilePainting] Paint callback exception: {}", e.what());
			}
		}
	}

	bool TilePaintingManager::MatchesAutoTileRule(entt::registry& registry, entt::entity tilemapLayer,
		const glm::ivec2& position, const AutoTileRule& rule) const {

		auto& tilemapSystem = TilemapSystem::GetInstance();

		// Check 3x3 neighborhood (9 positions)
		for (int i = 0; i < 9; ++i) {
			int dx = (i % 3) - 1;  // -1, 0, 1
			int dy = (i / 3) - 1;  // -1, 0, 1
			glm::ivec2 neighborPos = position + glm::ivec2{ dx, dy };

			int neighborTile = tilemapSystem.GetTile(registry, tilemapLayer, neighborPos);
			int centerTile = tilemapSystem.GetTile(registry, tilemapLayer, position);

			switch (rule.neighbors[i]) {
			case AutoTileRule::NeighborState::DontCare:
				break;
			case AutoTileRule::NeighborState::Empty:
				if (neighborTile != -1) return false;
				break;
			case AutoTileRule::NeighborState::Filled:
				if (neighborTile == -1) return false;
				break;
			case AutoTileRule::NeighborState::Different:
				if (neighborTile == centerTile) return false;
				break;
			}
		}

		return true;
	}

	int TilePaintingManager::GetSelectedTileId() const {
		if (selectedTileId != -1) {
			return selectedTileId;
		}

		// Fall back to active palette selection
		if (g_activePaletteId > 0) {
			auto it = g_tilePalettes.find(g_activePaletteId);
			if (it != g_tilePalettes.end()) {
				const auto& palette = it->second;
				if (selectedTileIndex >= 0 && selectedTileIndex < static_cast<int>(palette.tiles.size())) {
					return palette.tiles[selectedTileIndex].tileId;
				}
			}
		}

		return -1; // No valid selection
	}

	uint64_t TilePaintingManager::HashPosition(const glm::ivec2& pos) const {
		return (uint64_t(pos.x) << 32) | uint32_t(pos.y);
	}

	glm::ivec2 TilePaintingManager::UnhashPosition(uint64_t hash) const {
		return glm::ivec2{ int(hash >> 32), int(hash) };
	}

} // namespace WanderSpire