#pragma once
#include <vector>
#include <string>
#include <functional>
#include <glm/glm.hpp>
#include <entt/entt.hpp>
#include "WanderSpire/Editor/TilePaint/TilePalette.h"
#include "WanderSpire/Editor/TilePaint/TileBrush.h"
#include "WanderSpire/Editor/TilePaint/AutoTiling.h"

namespace WanderSpire {

	class TilePaintingManager {
	public:
		static TilePaintingManager& GetInstance();

		// Palette management
		void LoadPalette(const std::string& palettePath);
		void SavePalette(const std::string& palettePath, const TilePalette& palette);
		void CreatePalette(const std::string& name, const std::string& atlasPath);

		// Brush management
		void SetActiveBrush(const TileBrush& brush);
		const TileBrush& GetActiveBrush() const;

		// Painting operations
		void BeginPaint(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position);
		void ContinuePaint(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position);
		void EndPaint(entt::registry& registry, entt::entity tilemapLayer);

		void FloodFillWithBrush(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& startPos);

		void PaintLine(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& start, const glm::ivec2& end);
		void PaintRectangle(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& min, const glm::ivec2& max, bool filled = true);
		void PaintCircle(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& center, int radius, bool filled = true);

		// Preview & sampling
		std::vector<glm::ivec2> GetPaintPreview(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position) const;
		void ClearPreview();
		int SampleTile(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position);
		void SetSelectedTile(int tileId);

		// Auto-tiling
		void ApplyAutoTiling(entt::registry& registry, entt::entity tilemapLayer, const std::vector<glm::ivec2>& positions);
		void RegisterAutoTileSet(const AutoTileSet& tileSet);

		// Patterns
		void LoadPattern(const std::string& patternPath);
		void SavePattern(const std::string& patternPath, const std::vector<std::vector<int>>& pattern);
		void PaintPattern(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position, const std::vector<std::vector<int>>& pattern);

		// Callback/event support
		using PaintCallback = std::function<void(const std::vector<glm::ivec2>&, const std::vector<int>&)>;
		void RegisterPaintCallback(PaintCallback callback);

	private:
		TileBrush activeBrush;
		std::vector<AutoTileSet> autoTileSets;
		std::vector<TilePalette> loadedPalettes;
		std::vector<PaintCallback> paintCallbacks;
		std::vector<glm::ivec2> currentStroke;
		std::vector<glm::ivec2> previewPositions;
		bool isPainting = false;

		int selectedTileId = -1;
		int selectedTileIndex = -1;
		glm::ivec2 paintStartPosition = { 0, 0 };

		// --- Helper and private methods ---
		std::vector<glm::ivec2> GetBrushPositions(const glm::ivec2& center) const;
		std::vector<glm::ivec2> GetLinePositions(const glm::ivec2& start, const glm::ivec2& end) const;
		std::vector<glm::ivec2> GetCirclePositions(const glm::ivec2& center, int radius, bool filled) const;
		std::vector<glm::ivec2> GetPatternPositions(const glm::ivec2& center) const;

		int SelectTileVariant(int baseTileId) const;
		void ApplyPaintToPositions(entt::registry& registry, entt::entity tilemapLayer, const std::vector<glm::ivec2>& positions, int baseTileId);
		void NotifyPaintCallbacks(const std::vector<glm::ivec2>& positions, const std::vector<int>& tileIds);

		bool MatchesAutoTileRule(entt::registry& registry, entt::entity tilemapLayer, const glm::ivec2& position, const AutoTileRule& rule) const;

		int GetSelectedTileId() const;
		uint64_t HashPosition(const glm::ivec2& pos) const;
		glm::ivec2 UnhashPosition(uint64_t hash) const;
	};

} // namespace WanderSpire
