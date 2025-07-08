/* ================================================================
   Public C interface exported by EngineCore.dll
   ================================================================ */
#pragma once

#ifdef _WIN32
# ifdef ENGINECORE_EXPORTS
#   define ENGINE_API __declspec(dllexport)
# else
#   define ENGINE_API __declspec(dllimport)
# endif
#else
# define ENGINE_API __attribute__((visibility("default")))
#endif

   /* -----------------------------------------------------------------
	  EnTT tombstone helper
	  ----------------------------------------------------------------- */
#ifndef WS_INVALID_ENTITY
#  include <entt/entt.hpp>                                   
	  // 0xFFFFFFFF is the canonical 'null' value for the default entt::entity
#  define WS_INVALID_ENTITY (static_cast<uint32_t>(entt::entity{entt::null}))
#endif

#include <stdint.h>
#include <SDL3/SDL.h>

#ifdef __cplusplus
#include <WanderSpire/Components/AllComponents.h>
#include <glm/ext/vector_int2.hpp>
extern "C" {
#endif

	/* ── opaque handle ─────────────────────────────────────────── */
	typedef void* EngineContextHandle;

	typedef void* (*WS_GetProcAddress)(const char*);
	typedef void (*WS_RunInContext)(void (*fn)(void*), void* user);

	/* ── entity handle ─────────────────────────────────────────── */
	typedef struct EntityId { uint32_t id; } EntityId;

	/// Standard render layers - use custom values between these for fine control
	typedef enum {
		RENDER_LAYER_BACKGROUND = -1000,  ///< Clear operations, skybox  
		RENDER_LAYER_TERRAIN = 0,      ///< Ground tiles, background elements
		RENDER_LAYER_ENTITIES = 100,    ///< Game objects, sprites, characters
		RENDER_LAYER_EFFECTS = 200,    ///< Particles, visual effects  
		RENDER_LAYER_UI = 1000,   ///< User interface elements
		RENDER_LAYER_DEBUG = 2000,   ///< Debug overlays, gizmos
		RENDER_LAYER_POST_PROCESS = 3000   ///< Screen effects, filters
	} RenderLayer;

	/// Frame rendering statistics
	typedef struct {
		float frameTime;
		float renderTime;
		float updateTime;
		int drawCalls;
		int triangles;
		int entities;
		long memoryUsed;
	} FrameStats;

	/// Performance metrics
	typedef struct {
		float avgFrameTime;
		float minFrameTime;
		float maxFrameTime;
		float avgFPS;
		int totalDrawCalls;
		int totalTriangles;
		long totalMemoryUsed;
		long peakMemoryUsed;
	} PerformanceMetrics;

	/// Profiling section result
	typedef struct {
		char name[64];
		float totalTime;
		float avgTime;
		float minTime;
		float maxTime;
		int callCount;
	} ProfileSection;

	/// Texture information
	typedef struct {
		int width;
		int height;
		int channels;
		int format;
		long memorySize;
		char path[256];
	} TextureInfo;

	/// Editor render flags
	typedef enum {
		EDITOR_RENDER_NONE = 0,
		EDITOR_RENDER_SHOW_GRID = 1 << 0,
		EDITOR_RENDER_SHOW_GIZMOS = 1 << 1,
		EDITOR_RENDER_SHOW_BOUNDS = 1 << 2,
		EDITOR_RENDER_SHOW_WIREFRAME = 1 << 3,
		EDITOR_RENDER_SHOW_NORMALS = 1 << 4,
		EDITOR_RENDER_SHOW_COLLIDERS = 1 << 5,
		EDITOR_RENDER_SHOW_LIGHTS = 1 << 6,
		EDITOR_RENDER_SHOW_CAMERAS = 1 << 7,
		EDITOR_RENDER_SHOW_AUDIO = 1 << 8,
		EDITOR_RENDER_SHOW_PARTICLES = 1 << 9,
		EDITOR_RENDER_SHOW_UI = 1 << 10,
		EDITOR_RENDER_ALL = 0x7FF
	} EditorRenderFlags;

	/// Debug render flags
	typedef enum {
		DEBUG_RENDER_NONE = 0,
		DEBUG_RENDER_ENTITY_BOUNDS = 1 << 0,
		DEBUG_RENDER_COLLISION_SHAPES = 1 << 1,
		DEBUG_RENDER_PATHFINDING = 1 << 2,
		DEBUG_RENDER_PHYSICS = 1 << 3,
		DEBUG_RENDER_AUDIO = 1 << 4,
		DEBUG_RENDER_LIGHTING = 1 << 5,
		DEBUG_RENDER_PERFORMANCE = 1 << 6,
		DEBUG_RENDER_ALL = 0x7F
	} DebugRenderFlags;

	/// Gizmo types
	typedef enum {
		GIZMO_TRANSLATION = 0,
		GIZMO_ROTATION = 1,
		GIZMO_SCALE = 2,
		GIZMO_UNIVERSAL = 3
	} GizmoType;

	//=============================================================================
	// CORE ENGINE LIFECYCLE
	//=============================================================================

	/* ── context lifecycle ─────────────────────────────────────── */
	ENGINE_API EngineContextHandle CreateEngineContext(void);
	ENGINE_API void                DestroyEngineContext(EngineContextHandle ctx);

	/* ── application lifecycle ─────────────────────────────────── */
	ENGINE_API int   EngineInit(EngineContextHandle ctx, int argc, char* argv[]);
	ENGINE_API int   EngineIterate(EngineContextHandle ctx);
	ENGINE_API void  EngineQuit(EngineContextHandle ctx);
	ENGINE_API int   EngineEvent(EngineContextHandle ctx, void* rawSDL_Event);

	/* ── engine info helpers ──────────────────────────────────── */
	ENGINE_API float       Engine_GetTileSize(EngineContextHandle ctx);
	ENGINE_API SDL_Window* Engine_GetWindow(EngineContextHandle ctx);
	ENGINE_API void        Engine_GetWindowSize(EngineContextHandle ctx, int* outWidth, int* outHeight);
	ENGINE_API void        Engine_GetMouseTile(EngineContextHandle ctx, int* outX, int* outY);
	ENGINE_API float       Engine_GetTickInterval(EngineContextHandle ctx);

	//=============================================================================
	// RENDER PIPELINE API
	//=============================================================================

	/// Submit a sprite for rendering at the specified layer and order
	ENGINE_API void Render_SubmitSprite(
		EngineContextHandle ctx,
		uint32_t textureID,
		float posX, float posY,        ///< World position (center)
		float sizeX, float sizeY,      ///< World size
		float rotation,                ///< Rotation in radians
		float colorR, float colorG, float colorB,  ///< Tint color
		float uvOffsetX, float uvOffsetY,          ///< UV offset
		float uvSizeX, float uvSizeY,              ///< UV size
		int layer,                     ///< Render layer (use RenderLayer enum values)
		int order                      ///< Order within layer (lower = earlier)
	);

	/// Submit a custom rendering callback at the specified layer
	ENGINE_API void Render_SubmitCustom(
		EngineContextHandle ctx,
		void (*callback)(void* userData),  ///< Custom render function
		void* userData,                    ///< User data passed to callback
		int layer,                         ///< Render layer
		int order                          ///< Order within layer
	);

	/// Submit a screen clear operation
	ENGINE_API void Render_SubmitClear(
		EngineContextHandle ctx,
		float r, float g, float b          ///< Clear color
	);

	/// Get the total number of render commands queued for this frame
	ENGINE_API int Render_GetCommandCount(EngineContextHandle ctx);

	/// Clear all pending render commands without executing them
	ENGINE_API void Render_ClearCommands(EngineContextHandle ctx);

	/// Execute all queued render commands immediately (advanced usage)
	ENGINE_API void Render_ExecuteFrame(EngineContextHandle ctx);

	/// Get the current view-projection matrix
	ENGINE_API void Render_GetViewProjectionMatrix(
		EngineContextHandle ctx,
		float outMatrix[16]  ///< 4x4 matrix in column-major order
	);

	/// Get the current camera bounds in world space  
	ENGINE_API void Render_GetCameraBounds(
		EngineContextHandle ctx,
		float* outMinX, float* outMinY,
		float* outMaxX, float* outMaxY
	);

	//=============================================================================
	// ENTITY MANAGEMENT API
	//=============================================================================

	ENGINE_API EntityId CreateEntity(EngineContextHandle ctx);
	ENGINE_API void     DestroyEntity(EngineContextHandle ctx, EntityId e);
	ENGINE_API int      Engine_GetAllEntities(EngineContextHandle ctx, uint32_t* outArr, int maxCount);

	ENGINE_API void Engine_GetEntityWorldPosition(
		EngineContextHandle ctx,
		EntityId eid,
		float* outX,
		float* outY
	);

	//=============================================================================
	// COMPONENT REFLECTION API
	//=============================================================================

	ENGINE_API int HasComponent(EngineContextHandle ctx, EntityId e, const char* comp);
	ENGINE_API int GetComponentField(EngineContextHandle ctx, EntityId e, const char* c, const char* f, void* out, int size);
	ENGINE_API int SetComponentField(EngineContextHandle ctx, EntityId e, const char* c, const char* f, const void* data, int size);
	ENGINE_API int SetComponentJson(EngineContextHandle ctx, EntityId e, const char* c, const char* json);
	ENGINE_API int GetComponentJson(EngineContextHandle ctx, EntityId e, const char* comp, char* outJson, int outSize);
	ENGINE_API int RemoveComponent(EngineContextHandle ctx, EntityId e, const char* comp);

	//=============================================================================
	// SCRIPT DATA API
	//=============================================================================

	ENGINE_API int GetScriptDataValue(EngineContextHandle ctx, EntityId e, const char* key, char* outJson, int outSize);
	ENGINE_API int SetScriptDataValue(EngineContextHandle ctx, EntityId e, const char* key, const char* json);
	ENGINE_API int RemoveScriptDataValue(EngineContextHandle ctx, EntityId e, const char* key);

	//=============================================================================
	// PREFAB SYSTEM API
	//=============================================================================

	ENGINE_API EntityId Prefab_InstantiateAtTile(EngineContextHandle ctx, const char* prefab, int tx, int ty);
	ENGINE_API EntityId InstantiatePrefab(EngineContextHandle ctx, const char* prefab, float wx, float wy);

	//=============================================================================
	// EVENT SYSTEM API
	//=============================================================================

	typedef void (*ScriptEventCallback)(const char* evt, const void* payload, int size, void* user);
	ENGINE_API void Script_SubscribeEvent(EngineContextHandle ctx, const char* evt, ScriptEventCallback cb, void* user);
	ENGINE_API void Script_PublishEvent(EngineContextHandle ctx, const char* evt, const void* payload, int size);

	//=============================================================================
	// CAMERA API
	//=============================================================================

	ENGINE_API void Engine_SetPlayerEntity(EngineContextHandle ctx, EntityId player);
	ENGINE_API void Engine_SetCameraTarget(EngineContextHandle ctx, EntityId target);
	ENGINE_API void Engine_ClearCameraTarget(EngineContextHandle ctx);
	ENGINE_API void Engine_SetCameraPosition(EngineContextHandle ctx, float wx, float wy);

	//=============================================================================
	// OVERLAY RENDERING API
	//=============================================================================

	ENGINE_API void Engine_OverlayClear(EngineContextHandle ctx);
	ENGINE_API void Engine_OverlayRect(EngineContextHandle ctx, float wx, float wy, float w, float h, uint32_t colourRGBA);
	ENGINE_API void Engine_OverlayPresent(void);

	//=============================================================================
	// PATHFINDING API
	//=============================================================================

	ENGINE_API char* Engine_FindPath(
		EngineContextHandle h,
		int startX, int startY,
		int targetX, int targetY,
		int maxRange
	);

	ENGINE_API char* Engine_FindPathAdvanced(
		EngineContextHandle h,
		int startX, int startY,
		int targetX, int targetY,
		int maxRange,
		EntityId tilemapLayer
	);

	ENGINE_API void Engine_FreeString(char* str);

	//=============================================================================
	// SCENE MANAGEMENT API
	//=============================================================================

	ENGINE_API void SceneManager_SaveScene(EngineContextHandle wrapperHandle, const char* path);

	ENGINE_API bool SceneManager_LoadScene(
		EngineContextHandle wrapperHandle,
		const char* path,
		uint32_t* outPlayer,
		float* outPlayerX,
		float* outPlayerY,
		uint32_t* outMainTilemap
	);

	ENGINE_API bool SceneManager_LoadTilemap(
		EngineContextHandle wrapperHandle,
		const char* path,
		float positionX,
		float positionY,
		uint32_t* outTilemap
	);

	ENGINE_API bool SceneManager_SaveTilemap(
		EngineContextHandle wrapperHandle,
		const char* path,
		uint32_t tilemapEntity
	);

	ENGINE_API int SceneManager_GetSupportedFormatsCount(
		EngineContextHandle wrapperHandle,
		bool forLoading
	);

	//=============================================================================
	// IMGUI INTEGRATION API
	//=============================================================================

	ENGINE_API int ImGui_Initialize(EngineContextHandle ctx);
	ENGINE_API void ImGui_Shutdown(EngineContextHandle ctx);
	ENGINE_API int ImGui_ProcessEvent(EngineContextHandle ctx, void* sdlEvent);
	ENGINE_API void ImGui_NewFrame(EngineContextHandle ctx);
	ENGINE_API void ImGui_Render(EngineContextHandle ctx);
	ENGINE_API int ImGui_WantCaptureMouse(EngineContextHandle ctx);
	ENGINE_API int ImGui_WantCaptureKeyboard(EngineContextHandle ctx);
	ENGINE_API void ImGui_SetDisplaySize(EngineContextHandle ctx, float width, float height);
	ENGINE_API void ImGui_SetDockingEnabled(int enabled);
	ENGINE_API void* ImGui_GetFontAwesome();

	//=============================================================================
	// SCENE HIERARCHY API
	//=============================================================================

	ENGINE_API EntityId SceneHierarchy_CreateGameObject(
		EngineContextHandle ctx,
		const char* name,
		EntityId parent  ///< Pass {WS_INVALID_ENTITY} for root object
	);

	ENGINE_API int SceneHierarchy_SetParent(
		EngineContextHandle ctx,
		EntityId child,
		EntityId parent  ///< Pass {WS_INVALID_ENTITY} to make root object
	);

	ENGINE_API int SceneHierarchy_GetChildren(
		EngineContextHandle ctx,
		EntityId parent,
		uint32_t* outChildren,
		int maxCount
	);

	ENGINE_API EntityId SceneHierarchy_GetParent(
		EngineContextHandle ctx,
		EntityId child
	);

	ENGINE_API int SceneHierarchy_GetRootObjects(
		EngineContextHandle ctx,
		uint32_t* outRoots,
		int maxCount
	);

	//=============================================================================
	// SELECTION API
	//=============================================================================

	ENGINE_API void Selection_SelectEntity(EngineContextHandle ctx, EntityId entity);
	ENGINE_API void Selection_AddToSelection(EngineContextHandle ctx, EntityId entity);
	ENGINE_API void Selection_DeselectAll(EngineContextHandle ctx);

	ENGINE_API int Selection_GetSelectedEntities(
		EngineContextHandle ctx,
		uint32_t* outEntities,
		int maxCount
	);

	ENGINE_API int Selection_SelectInBounds(
		EngineContextHandle ctx,
		float minX, float minY,
		float maxX, float maxY
	);

	//=============================================================================
	// LAYER MANAGEMENT API
	//=============================================================================

	ENGINE_API int Layer_Create(EngineContextHandle ctx, const char* name);
	ENGINE_API void Layer_Remove(EngineContextHandle ctx, int layerId);
	ENGINE_API void Layer_SetVisible(EngineContextHandle ctx, int layerId, int visible);
	ENGINE_API void Layer_SetEntityLayer(EngineContextHandle ctx, EntityId entity, int layerId);
	ENGINE_API int Layer_GetEntityLayer(EngineContextHandle ctx, EntityId entity);

	//=============================================================================
	// TILEMAP API
	//=============================================================================

	ENGINE_API EntityId Tilemap_Create(EngineContextHandle ctx, const char* name);
	ENGINE_API EntityId Tilemap_CreateLayer(
		EngineContextHandle ctx,
		EntityId tilemap,
		const char* layerName
	);

	ENGINE_API void Tilemap_SetTile(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY,
		int tileId
	);

	ENGINE_API int Tilemap_GetTile(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY
	);

	ENGINE_API int Tilemap_FloodFill(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int startX, int startY,
		int newTileId
	);

	//=============================================================================
	// TILE PALETTE API
	//=============================================================================

	ENGINE_API int TilePalette_Create(
		EngineContextHandle ctx,
		const char* paletteName,
		const char* atlasPath,
		int tileWidth, int tileHeight
	);

	ENGINE_API void TilePalette_SetActive(EngineContextHandle ctx, int paletteId);
	ENGINE_API int TilePalette_GetActive(EngineContextHandle ctx);

	ENGINE_API int TilePalette_AddTile(
		EngineContextHandle ctx,
		int paletteId,
		int tileId,
		const char* tileName,
		const char* assetPath,
		int atlasX, int atlasY,
		int walkable,
		int collisionType
	);

	ENGINE_API int TilePalette_GetInfo(
		EngineContextHandle ctx,
		int paletteId,
		char* outName, int nameBufferSize,
		char* outAtlasPath, int atlasPathBufferSize,
		int* outTileWidth,
		int* outTileHeight,
		int* outColumns
	);

	ENGINE_API int TilePalette_GetTileCount(EngineContextHandle ctx, int paletteId);

	ENGINE_API int TilePalette_GetTileInfo(
		EngineContextHandle ctx,
		int paletteId,
		int tileIndex,
		int* outTileId,
		char* outTileName, int nameBufferSize,
		int* outAtlasX, int* outAtlasY,
		int* outWalkable,
		int* outCollisionType
	);

	ENGINE_API int TilePalette_Load(EngineContextHandle ctx, const char* palettePath);
	ENGINE_API int TilePalette_Save(EngineContextHandle ctx, int paletteId, const char* palettePath);
	ENGINE_API int TilePalette_GetSelectedTile(EngineContextHandle ctx);
	ENGINE_API void TilePalette_SetSelectedTile(EngineContextHandle ctx, int tileId);

	//=============================================================================
	// TILE BRUSH API
	//=============================================================================

	typedef enum {
		BRUSH_SINGLE = 0,
		BRUSH_RECTANGLE = 1,
		BRUSH_CIRCLE = 2,
		BRUSH_LINE = 3,
		BRUSH_PATTERN = 4,
		BRUSH_MULTI = 5
	} BrushType;

	typedef enum {
		BLEND_REPLACE = 0,
		BLEND_ADD = 1,
		BLEND_SUBTRACT = 2,
		BLEND_OVERLAY = 3
	} BlendMode;

	ENGINE_API void TileBrush_SetType(EngineContextHandle ctx, int brushType);
	ENGINE_API void TileBrush_SetSize(EngineContextHandle ctx, int size);
	ENGINE_API void TileBrush_SetBlendMode(EngineContextHandle ctx, int blendMode);
	ENGINE_API void TileBrush_SetRandomization(EngineContextHandle ctx, int enabled, float strength);
	ENGINE_API void TileBrush_SetOpacity(EngineContextHandle ctx, float opacity);

	ENGINE_API int TileBrush_GetSettings(
		EngineContextHandle ctx,
		int* outType,
		int* outSize,
		int* outBlendMode,
		float* outOpacity,
		int* outRandomEnabled,
		float* outRandomStrength
	);

	ENGINE_API int TileBrush_LoadPattern(EngineContextHandle ctx, const char* patternPath);
	ENGINE_API int TileBrush_SavePattern(EngineContextHandle ctx, const char* patternPath);

	//=============================================================================
	// TILE PAINTING API
	//=============================================================================

	ENGINE_API void TilePaint_Begin(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY
	);

	ENGINE_API void TilePaint_Continue(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY
	);

	ENGINE_API void TilePaint_End(EngineContextHandle ctx, EntityId tilemapLayer);

	ENGINE_API void TilePaint_PaintWithBrush(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY
	);

	ENGINE_API int TilePaint_GetPreview(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY,
		int* outTilePositions,
		int maxPositions
	);

	ENGINE_API int TilePaint_GetBrushPreview(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY,
		int* outTilePositions,
		int maxPositions
	);

	ENGINE_API void TilePaint_PaintLine(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int startX, int startY,
		int endX, int endY
	);

	ENGINE_API void TilePaint_PaintRectangle(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int minX, int minY,
		int maxX, int maxY,
		int filled
	);

	ENGINE_API void TilePaint_PaintCircle(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int centerX, int centerY,
		int radius,
		int filled
	);

	ENGINE_API int TilePaint_SampleTile(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY
	);

	//=============================================================================
	// TILE LAYER OPERATIONS API
	//=============================================================================

	ENGINE_API int TilemapLayer_GetAllInTilemap(
		EngineContextHandle ctx,
		EntityId tilemap,
		uint32_t* outLayers,
		int maxCount
	);

	ENGINE_API int TilemapLayer_GetInfo(
		EngineContextHandle ctx,
		EntityId layer,
		char* outName, int nameBufferSize,
		int* outVisible,
		int* outLocked,
		float* outOpacity,
		int* outSortOrder
	);

	ENGINE_API void TilemapLayer_SetVisible(EngineContextHandle ctx, EntityId layer, int visible);
	ENGINE_API void TilemapLayer_SetLocked(EngineContextHandle ctx, EntityId layer, int locked);
	ENGINE_API void TilemapLayer_SetOpacity(EngineContextHandle ctx, EntityId layer, float opacity);
	ENGINE_API void TilemapLayer_SetSortOrder(EngineContextHandle ctx, EntityId layer, int sortOrder);
	ENGINE_API void TilemapLayer_Reorder(EngineContextHandle ctx, EntityId layer, int newSortOrder);

	ENGINE_API int TilemapLayer_GetPaintable(
		EngineContextHandle ctx,
		uint32_t* outLayers,
		int maxCount
	);

	ENGINE_API void TileLayer_CopyRegion(
		EngineContextHandle ctx,
		EntityId srcLayer,
		EntityId dstLayer,
		int srcMinX, int srcMinY,
		int srcMaxX, int srcMaxY,
		int dstX, int dstY
	);

	ENGINE_API void TileLayer_CopyToClipboard(
		EngineContextHandle ctx,
		EntityId layer,
		int minX, int minY,
		int maxX, int maxY
	);

	ENGINE_API void TileLayer_PasteFromClipboard(
		EngineContextHandle ctx,
		EntityId layer,
		int x, int y
	);

	ENGINE_API void TileLayer_BlendLayers(
		EngineContextHandle ctx,
		EntityId baseLayer,
		EntityId overlayLayer,
		int minX, int minY,
		int maxX, int maxY,
		float opacity
	);

	ENGINE_API void TileLayer_SetPalette(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int paletteId
	);

	ENGINE_API int TileLayer_GetPalette(
		EngineContextHandle ctx,
		EntityId tilemapLayer
	);

	ENGINE_API void TileLayer_RefreshDefinitions(
		EngineContextHandle ctx,
		EntityId tilemapLayer
	);

	//=============================================================================
	// COMMAND SYSTEM API
	//=============================================================================

	ENGINE_API void Command_Execute(EngineContextHandle ctx, const char* commandJson);
	ENGINE_API int Command_Undo(EngineContextHandle ctx);
	ENGINE_API int Command_Redo(EngineContextHandle ctx);
	ENGINE_API int Command_CanUndo(EngineContextHandle ctx);
	ENGINE_API int Command_CanRedo(EngineContextHandle ctx);

	ENGINE_API int Command_GetUndoDescription(
		EngineContextHandle ctx,
		char* outDescription,
		int bufferSize
	);

	ENGINE_API int Command_GetRedoDescription(
		EngineContextHandle ctx,
		char* outDescription,
		int bufferSize
	);

	ENGINE_API int Command_GetHistorySize(EngineContextHandle ctx);
	ENGINE_API void Command_SetMaxHistorySize(EngineContextHandle ctx, int maxSize);
	ENGINE_API void Command_ClearHistory(EngineContextHandle ctx);

	ENGINE_API void Command_MoveSelection(
		EngineContextHandle ctx,
		float deltaX, float deltaY
	);

	ENGINE_API void Command_DeleteSelection(EngineContextHandle ctx);

	//=============================================================================
	// GRID OPERATIONS API
	//=============================================================================

	ENGINE_API void Grid_SnapPosition(
		EngineContextHandle ctx,
		float inX, float inY,
		float* outX, float* outY
	);

	ENGINE_API float Grid_GetTileSize(EngineContextHandle ctx);

	//=============================================================================
	// AUTO-TILING API
	//=============================================================================

	ENGINE_API int AutoTile_CreateRuleSet(EngineContextHandle ctx, const char* name);

	ENGINE_API void AutoTile_AddRule(
		EngineContextHandle ctx,
		int ruleSetId,
		const int* neighbors,
		int resultTileId,
		int priority
	);

	ENGINE_API void AutoTile_SetEnabled(EngineContextHandle ctx, int ruleSetId, int enabled);

	ENGINE_API void AutoTile_ApplyToRegion(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int minX, int minY,
		int maxX, int maxY
	);

	//=============================================================================
	// TILE DEFINITION API
	//=============================================================================

	ENGINE_API void TileDef_Register(
		EngineContextHandle ctx,
		int tileId,
		const char* atlasName,
		const char* frameName,
		int walkable,
		int collisionType
	);

	ENGINE_API void TileDef_SetDefault(
		EngineContextHandle ctx,
		const char* atlasName,
		const char* frameName
	);

	ENGINE_API int TileDef_GetCount(EngineContextHandle ctx);
	ENGINE_API void TileDef_Clear(EngineContextHandle ctx);

	ENGINE_API void TileDef_RegisterTile(
		EngineContextHandle ctx,
		int tileId,
		const char* atlasName,
		const char* frameName,
		int walkable,
		int collisionType
	);

	ENGINE_API int TileDef_GetTileInfo(
		EngineContextHandle ctx,
		int tileId,
		char* outAtlasName, int atlasNameSize,
		char* outFrameName, int frameNameSize,
		int* outWalkable,
		int* outCollisionType
	);

	ENGINE_API void TileDef_LoadFromPalette(
		EngineContextHandle ctx,
		int paletteId
	);

	//=============================================================================
	// TILEMAP ANALYSIS API
	//=============================================================================

	ENGINE_API int Tilemap_GetBounds(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int* outMinX, int* outMinY,
		int* outMaxX, int* outMaxY
	);

	ENGINE_API int Tilemap_CountTilesInRegion(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int minX, int minY,
		int maxX, int maxY
	);

	ENGINE_API int Tilemap_FindTilePositions(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileId,
		int* outPositions,
		int maxPositions
	);

	ENGINE_API int Tilemap_ReplaceTiles(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int oldTileId,
		int newTileId,
		int minX, int minY,
		int maxX, int maxY
	);

	//=============================================================================
	// COORDINATE CONVERSION API
	//=============================================================================

	ENGINE_API void Coord_WorldToTile(
		EngineContextHandle ctx,
		float worldX, float worldY,
		int* outTileX, int* outTileY
	);

	ENGINE_API void Coord_TileToWorld(
		EngineContextHandle ctx,
		int tileX, int tileY,
		float* outWorldX, float* outWorldY
	);

	ENGINE_API float Coord_GetTileSize(EngineContextHandle ctx);
	ENGINE_API void Coord_SetTileSize(EngineContextHandle ctx, float tileSize);

	/// Initialize engine in editor mode with specific settings
	ENGINE_API int Engine_InitializeEditor(EngineContextHandle ctx, int width, int height, int flags);

	/// Set viewport size for embedded rendering
	ENGINE_API void Engine_SetViewportSize(EngineContextHandle ctx, int width, int height);

	/// Get current viewport size
	ENGINE_API void Engine_GetViewportSize(EngineContextHandle ctx, int* outWidth, int* outHeight);

	/// Enable/disable editor-specific rendering features
	ENGINE_API void Engine_SetEditorRenderFlags(EngineContextHandle ctx, int flags);

	/// Render frame to specific target (for embedded viewport)
	ENGINE_API void Engine_RenderToFramebuffer(EngineContextHandle ctx, uint32_t framebuffer, int width, int height);

	/// Get frame statistics
	ENGINE_API void Engine_GetFrameStats(EngineContextHandle ctx, FrameStats* outStats);

	//=============================================================================
	// ENTITY PICKING AND SELECTION
	//=============================================================================

	/// Pick entity at screen coordinates
	ENGINE_API EntityId Engine_PickEntity(EngineContextHandle ctx, int screenX, int screenY);

	/// Pick multiple entities in a rectangular region
	ENGINE_API int Engine_PickEntitiesInRect(EngineContextHandle ctx, int x1, int y1, int x2, int y2,
		uint32_t* outEntities, int maxEntities);

	/// Get entity bounding box in screen space
	ENGINE_API int Engine_GetEntityScreenBounds(EngineContextHandle ctx, EntityId entity,
		float* outMinX, float* outMinY, float* outMaxX, float* outMaxY);

	/// Get entity bounding box in world space
	ENGINE_API int Engine_GetEntityWorldBounds(EngineContextHandle ctx, EntityId entity,
		float* outMinX, float* outMinY, float* outMaxX, float* outMaxY);

	//=============================================================================
	// CAMERA AND VIEWPORT CONTROLS
	//=============================================================================

	/// Set camera zoom level
	ENGINE_API void Engine_SetCameraZoom(EngineContextHandle ctx, float zoom);

	/// Get current camera zoom level
	ENGINE_API float Engine_GetCameraZoom(EngineContextHandle ctx);

	/// Convert screen coordinates to world coordinates
	ENGINE_API void Engine_ScreenToWorld(EngineContextHandle ctx, int screenX, int screenY,
		float* outWorldX, float* outWorldY);

	/// Convert world coordinates to screen coordinates
	ENGINE_API void Engine_WorldToScreen(EngineContextHandle ctx, float worldX, float worldY,
		int* outScreenX, int* outScreenY);

	/// Get camera view matrix
	ENGINE_API void Engine_GetCameraViewMatrix(EngineContextHandle ctx, float* outMatrix);

	/// Get camera projection matrix
	ENGINE_API void Engine_GetCameraProjectionMatrix(EngineContextHandle ctx, float* outMatrix);

	//=============================================================================
	// GRID AND GIZMO RENDERING
	//=============================================================================

	/// Enable/disable grid rendering
	ENGINE_API void Engine_SetGridVisible(EngineContextHandle ctx, int visible);

	/// Set grid properties
	ENGINE_API void Engine_SetGridProperties(EngineContextHandle ctx, float size, int subdivisions,
		float colorR, float colorG, float colorB, float alpha);

	/// Render selection outline around entity
	ENGINE_API void Engine_RenderSelectionOutline(EngineContextHandle ctx, EntityId entity,
		float colorR, float colorG, float colorB, float width);

	/// Render transformation gizmo at position
	ENGINE_API void Engine_RenderTransformGizmo(EngineContextHandle ctx, float worldX, float worldY,
		float scale, int gizmoType);

	//=============================================================================
	// DEBUG AND VISUALIZATION
	//=============================================================================

	/// Enable/disable debug rendering
	ENGINE_API void Engine_SetDebugRenderEnabled(EngineContextHandle ctx, int enabled);

	/// Set debug render flags
	ENGINE_API void Engine_SetDebugRenderFlags(EngineContextHandle ctx, int flags);

	/// Draw debug line in world space
	ENGINE_API void Engine_DrawDebugLine(EngineContextHandle ctx, float x1, float y1, float x2, float y2,
		float colorR, float colorG, float colorB, float width);

	/// Draw debug circle in world space
	ENGINE_API void Engine_DrawDebugCircle(EngineContextHandle ctx, float centerX, float centerY, float radius,
		float colorR, float colorG, float colorB, int segments);

	/// Draw debug rectangle in world space
	ENGINE_API void Engine_DrawDebugRect(EngineContextHandle ctx, float x, float y, float width, float height,
		float colorR, float colorG, float colorB, int filled);

	//=============================================================================
	// PERFORMANCE AND PROFILING
	//=============================================================================

	/// Get detailed performance metrics
	ENGINE_API void Engine_GetPerformanceMetrics(EngineContextHandle ctx, PerformanceMetrics* outMetrics);

	/// Start performance profiling section
	ENGINE_API void Engine_BeginProfileSection(EngineContextHandle ctx, const char* name);

	/// End performance profiling section
	ENGINE_API void Engine_EndProfileSection(EngineContextHandle ctx, const char* name);

	/// Get profiling results
	ENGINE_API int Engine_GetProfilingResults(EngineContextHandle ctx, ProfileSection* outSections, int maxSections);

	//=============================================================================
	// ASSET MANAGEMENT
	//=============================================================================

	/// Load texture and return handle
	ENGINE_API uint32_t Engine_LoadTexture(EngineContextHandle ctx, const char* path);

	/// Unload texture by handle
	ENGINE_API void Engine_UnloadTexture(EngineContextHandle ctx, uint32_t textureHandle);

	/// Get texture info
	ENGINE_API int Engine_GetTextureInfo(EngineContextHandle ctx, uint32_t textureHandle, TextureInfo* outInfo);

	/// Reload asset by path
	ENGINE_API int Engine_ReloadAsset(EngineContextHandle ctx, const char* path);

	//=============================================================================
	// ENTITY MANIPULATION
	//=============================================================================

	/// Clone entity with all components
	ENGINE_API EntityId Engine_CloneEntity(EngineContextHandle ctx, EntityId source);

	/// Move entity in hierarchy
	ENGINE_API int Engine_MoveEntityInHierarchy(EngineContextHandle ctx, EntityId entity, EntityId newParent, int siblingIndex);

	/// Get entity depth in hierarchy
	ENGINE_API int Engine_GetEntityDepth(EngineContextHandle ctx, EntityId entity);

	/// Check if entity is ancestor of another
	ENGINE_API int Engine_IsEntityAncestorOf(EngineContextHandle ctx, EntityId ancestor, EntityId descendant);

	//=============================================================================
// OPENGL CONTEXT AND FRAMEBUFFER MANAGEMENT
//=============================================================================

/// Initialize OpenGL context sharing with external context
	ENGINE_API int Engine_InitializeSharedGL(void* ctx, void* sharedContext);

	/// Create render target framebuffer with color and depth textures
	ENGINE_API uint32_t Engine_CreateRenderTarget(void* ctx, int width, int height,
		uint32_t* outColorTexture, uint32_t* outDepthTexture);

	/// Destroy render target and associated textures
	ENGINE_API void Engine_DestroyRenderTarget(void* ctx, uint32_t framebuffer,
		uint32_t colorTexture, uint32_t depthTexture);

	/// Resize render target framebuffer
	ENGINE_API int Engine_ResizeRenderTarget(void* ctx, uint32_t framebuffer,
		uint32_t colorTexture, uint32_t depthTexture, int newWidth, int newHeight);

	/// Set engine to render to specific framebuffer
	ENGINE_API void Engine_SetRenderTarget(void* ctx, uint32_t framebuffer, int width, int height);

	/// Restore default framebuffer (screen)
	ENGINE_API void Engine_RestoreDefaultFramebuffer(void* ctx);

	/// Render one frame to current render target
	ENGINE_API void Engine_RenderToTarget(void* ctx, void* nativeWindow, int width, int height);

	/// Render frame to specific framebuffer
	ENGINE_API void Engine_RenderToFramebuffer(void* ctx, uint32_t framebuffer, int width, int height);

	/// Blit framebuffer to screen or another framebuffer
	ENGINE_API void Engine_BlitFramebuffer(void* ctx, uint32_t srcFBO, uint32_t dstFBO,
		int srcX0, int srcY0, int srcX1, int srcY1,
		int dstX0, int dstY0, int dstX1, int dstY1, uint32_t mask, uint32_t filter);

	//=============================================================================
	// OPENGL STATE MANAGEMENT
	//=============================================================================

	/// Get current OpenGL context from engine
	ENGINE_API void* Engine_GetGLContext(void* ctx);

	/// Make engine's OpenGL context current
	ENGINE_API int Engine_MakeGLContextCurrent(void* ctx);

	/// Share OpenGL resources with external context
	ENGINE_API int Engine_ShareGLContext(void* ctx, void* externalContext);

	/// Sync OpenGL state between contexts
	ENGINE_API void Engine_SyncGLState(void* ctx);

	/// Get OpenGL texture handle from engine texture ID
	ENGINE_API uint32_t Engine_GetGLTextureHandle(void* ctx, uint32_t engineTextureId);

	//=============================================================================
	// TEXTURE MANAGEMENT
	//=============================================================================

	/// Create OpenGL texture with specific format
	ENGINE_API uint32_t Engine_CreateGLTexture(void* ctx, int width, int height,
		uint32_t internalFormat, uint32_t format, uint32_t type);

	/// Update texture data
	ENGINE_API void Engine_UpdateTextureData(void* ctx, uint32_t textureId,
		int width, int height, uint32_t format, uint32_t type, void* data);

	/// Get texture data (for readback)
	ENGINE_API int Engine_GetTextureData(void* ctx, uint32_t textureId,
		uint32_t format, uint32_t type, void* outData, int bufferSize);

	//=============================================================================
	// EDITOR-SPECIFIC RENDERING
	//=============================================================================

	/// Begin editor frame rendering
	ENGINE_API void Engine_BeginEditorFrame(void* ctx);

	/// End editor frame rendering
	ENGINE_API void Engine_EndEditorFrame(void* ctx);

	/// Set editor viewport transformation
	ENGINE_API void Engine_SetEditorViewport(void* ctx, int x, int y, int width, int height);

	/// Render scene with editor overlays
	ENGINE_API void Engine_RenderSceneWithOverlays(void* ctx);

	/// Check if engine supports external OpenGL context
	ENGINE_API int Engine_SupportsExternalGL(EngineContextHandle ctx);

	/// Get engine OpenGL capabilities as JSON string
	ENGINE_API int Engine_GetGLCapabilities(EngineContextHandle ctx, char* capabilities, int bufferSize);

	/// Validate that shared context is compatible
	ENGINE_API int Engine_ValidateSharedContext(EngineContextHandle ctx, EngineContextHandle externalContext);

	/// Get last OpenGL error from engine  
	ENGINE_API uint32_t Engine_GetLastGLError(EngineContextHandle ctx);

	/// Set engine to use immediate mode rendering (for fallback)
	ENGINE_API void Engine_SetImmediateModeRendering(EngineContextHandle ctx, int enabled);

	/// Check if engine is running in headless mode
	ENGINE_API int Engine_IsHeadless(EngineContextHandle ctx);

	ENGINE_API void Engine_BindOpenGLContext(WS_GetProcAddress getProc);

	ENGINE_API void Engine_RegisterRunInContext(WS_RunInContext cb);
	ENGINE_API void Engine_RunInContext(void (*fn)(void*), void* user);

#ifdef __cplusplus
}   /* extern "C" */

namespace WanderSpire {
	namespace EditorAPI {
		/// Helper to validate layer entity
		inline bool ValidateLayer(entt::registry& registry, entt::entity layer) {
			return registry.valid(layer) && registry.any_of<TilemapLayerComponent>(layer);
		}

		/// Helper to validate tilemap entity
		inline bool ValidateTilemap(entt::registry& registry, entt::entity tilemap) {
			return registry.valid(tilemap) && registry.any_of<SceneNodeComponent>(tilemap);
		}

		/// Helper to get safe string copy
		inline void SafeStringCopy(const std::string& source, char* dest, int destSize) {
			if (dest && destSize > 0) {
				size_t copySize = std::min(static_cast<size_t>(destSize - 1), source.size());
				source.copy(dest, copySize);
				dest[copySize] = '\0';
			}
		}

		/// Helper to convert positions to array
		inline int ConvertPositionsToArray(const std::vector<glm::ivec2>& positions,
			int* outArray, int maxCount) {
			int count = std::min(static_cast<int>(positions.size()), maxCount / 2);
			for (int i = 0; i < count; ++i) {
				outArray[i * 2] = positions[i].x;
				outArray[i * 2 + 1] = positions[i].y;
			}
			return count;
		}
	} // namespace EditorAPI
} // namespace WanderSpire
#endif