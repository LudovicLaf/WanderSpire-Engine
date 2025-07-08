#define ENGINECORE_EXPORTS
#include "EngineCore.h"
#include "EngineAPI.h"

#include <WanderSpire/Core/Application.h>
#include <WanderSpire/Core/AppState.h>
#include <WanderSpire/Core/Reflection.h>
#include <WanderSpire/World/TileDefinitionManager.h>
#include "WanderSpire/ECS/SerializableComponents.h" 
#include <WanderSpire/ECS/PrefabManager.h>
#include <WanderSpire/Scene/SceneManager.h>
#include "WanderSpire/Core/EventBus.h"
#include "WanderSpire/Core/Events.h"
#include "WanderSpire/Core/Reflection.h"
#include <WanderSpire/Graphics/RenderManager.h>
#include "WanderSpire/Editor/EditorSystems.h"
#include "WanderSpire/Editor/TilePaint/TilePaintSystems.h"
#include "WanderSpire/Editor/EditorGlobals.h"
#include "WanderSpire/World/TilemapSystem.h"
#include "WanderSpire/World/Pathfinder2D.h"
#include <WanderSpire/Components/AllComponents.h>
#include <WanderSpire/Components/ScriptDataComponent.h>
#include <WanderSpire/Graphics/SpriteRenderer.h>
#include "WanderSpire/Editor/SceneHierarchyManager.h"
#include "WanderSpire/Graphics/RenderManager.h"
#include "WanderSpire/Components/IDComponent.h"

#include <glm/vec2.hpp>
#include <fstream>    
#include <spdlog/spdlog.h>
#include <nlohmann/json.hpp>
#include <SDL3/SDL.h>
#include <entt/entt.hpp> 
#include <chrono>
#include <unordered_map>
#include <cstring>
#include <algorithm>
#include <cmath>
#include <limits> 


// ImGui includes
#include <imgui.h>
#include <WanderSpire/External/imgui_impl_sdl3.h>
#include <WanderSpire/External/imgui_impl_opengl3.h>
#include <WanderSpire/External/IconsFontAwesome5.h>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

#ifdef _WIN32
#include <direct.h>   // for _getcwd
#define GETCWD _getcwd
#else
#include <unistd.h>   // for getcwd
#define GETCWD getcwd
#endif
#include <WanderSpire/Components/SpriteRenderComponent.h>
#include <WanderSpire/Graphics/GLStateManager.h>
#include <AssetLoader.h>
#include <FileWatcher.h>

// Global state for editor features
struct EditorState {
	int editorRenderFlags = 0;
	int debugRenderFlags = 0;
	bool gridVisible = false;
	float gridSize = 32.0f;
	int gridSubdivisions = 4;
	glm::vec4 gridColor = { 0.5f, 0.5f, 0.5f, 0.3f };

	// Profiling
	std::unordered_map<std::string, std::chrono::high_resolution_clock::time_point> profileStarts;
	std::unordered_map<std::string, ProfileSection> profileData;

	// Viewport
	int viewportWidth = 800;
	int viewportHeight = 600;
};

struct OpenGLContextInfo {
	bool isOpenGLES = false;
	int majorVersion = 0;
	int minorVersion = 0;
	std::string version;
	std::string renderer;
	bool contextValidated = false;
} g_glContextInfo;

struct EditorCameraState {
	WanderSpire::Camera2D camera{ 800.0f, 600.0f };
	bool initialized = false;
};

// Global OpenGL state management
struct OpenGLState {
	SDL_GLContext primaryContext = nullptr;
	SDL_GLContext sharedContext = nullptr;
	std::unordered_map<uint32_t, GLuint> framebuffers;
	std::unordered_map<uint32_t, GLuint> colorTextures;
	std::unordered_map<uint32_t, GLuint> depthTextures;
	uint32_t nextFBOId = 1;
	GLuint currentFramebuffer = 0;
	int viewportWidth = 800;
	int viewportHeight = 600;
	GLenum lastGLError = GL_NO_ERROR;
	bool contextValidated = false;
};

using namespace EngineCoreInternal;
using json = nlohmann::json;
static uint64_t g_nextUuid = 1;
static ImFont* g_FontAwesome = nullptr;
static std::vector<unsigned char> s_FaData;
static entt::entity g_cameraFollow = entt::null;
static EditorState g_editorState;
static OpenGLState g_glState;
static WS_RunInContext g_runInCtx = nullptr;
static EditorCameraState g_editorCamera;

using EngineCoreInternal::OverlayRect;
static std::mutex                     gOverlayMx;
static std::vector<OverlayRect>       gOverlays;

//──────────────── helper ────────────────────────────────────────────────
static inline WanderSpire::AppState* asAppState(Wrapper* w) {
	return static_cast<WanderSpire::AppState*>(w->appState);
}

inline uint32_t entity_to_uint32(entt::entity e) {
	return static_cast<uint32_t>(e);
}

static inline bool is_valid(entt::registry& reg, entt::entity e) noexcept
{
	return reg.valid(e);
}

static inline bool is_null(uint32_t raw) noexcept
{
	return raw == WS_INVALID_ENTITY;
}

static void DetectOpenGLContext() {
	if (g_glContextInfo.contextValidated) return;

	try {
		const char* versionStr = reinterpret_cast<const char*>(glGetString(GL_VERSION));
		if (versionStr) {
			g_glContextInfo.version = versionStr;
			g_glContextInfo.isOpenGLES = (g_glContextInfo.version.find("OpenGL ES") != std::string::npos);

			// Parse version numbers
			if (g_glContextInfo.isOpenGLES) {
				// Example: "OpenGL ES 3.0.0 (ANGLE 2.1.25606...)"
				size_t esPos = g_glContextInfo.version.find("OpenGL ES ");
				if (esPos != std::string::npos) {
					std::string verPart = g_glContextInfo.version.substr(esPos + 10);
					sscanf(verPart.c_str(), "%d.%d", &g_glContextInfo.majorVersion, &g_glContextInfo.minorVersion);
				}
			}
			else {
				// Regular OpenGL version parsing
				sscanf(versionStr, "%d.%d", &g_glContextInfo.majorVersion, &g_glContextInfo.minorVersion);
			}

			const char* rendererStr = reinterpret_cast<const char*>(glGetString(GL_RENDERER));
			if (rendererStr) {
				g_glContextInfo.renderer = rendererStr;
			}

			g_glContextInfo.contextValidated = true;

			spdlog::info("[OpenGL] Detected context: {} ({})",
				g_glContextInfo.version, g_glContextInfo.renderer);
			spdlog::info("[OpenGL] OpenGL ES: {}, Version: {}.{}",
				g_glContextInfo.isOpenGLES, g_glContextInfo.majorVersion, g_glContextInfo.minorVersion);
		}
	}
	catch (const std::exception& ex) {
		spdlog::error("[OpenGL] Context detection failed: {}", ex.what());
	}
}

// Helper to get wrapper
static inline Wrapper* GetWrapper(EngineContextHandle ctx) {
	return static_cast<Wrapper*>(ctx);
}

static bool LoadFontAwesomeFromFile(const char* relPath)
{
	std::ifstream fs(relPath, std::ios::binary | std::ios::ate);
	if (!fs) return false;

	std::streamsize sz = fs.tellg();
	if (sz <= 0) return false;

	fs.seekg(0, std::ios::beg);
	s_FaData.resize(static_cast<size_t>(sz));
	return fs.read(reinterpret_cast<char*>(s_FaData.data()), sz).good();
}

//──────────────── Reflection helper ─────────────────────────────────────
static const Reflect::FieldInfo*
findField(const char* comp, const char* field, const Reflect::TypeInfo** outTI = nullptr) {
	auto& map = Reflect::TypeRegistry::Get().GetNameMap();
	auto it = map.find(comp);
	if (it == map.end()) return nullptr;
	const auto& ti = it->second;
	if (outTI) *outTI = &ti;
	for (auto& f : ti.fields)
		if (f.name == field) return &f;
	return nullptr;
}

/* ─────────────── Overlay helpers ─────────────────────────────────────── */

/* helper – decode premultiplied 0xAARRGGBB into linear vec3 */
static glm::vec3 DecodeColour(uint32_t rgba)
{
	const float a = ((rgba >> 24) & 0xFF) / 255.f;
	const float r = ((rgba >> 16) & 0xFF) / 255.f * a;
	const float g = ((rgba >> 8) & 0xFF) / 255.f * a;
	const float b = (rgba & 0xFF) / 255.f * a;
	return { r,g,b };
}

static void FlushOverlayBatch()
{
	/* copy under lock */
	std::vector<OverlayRect> snapshot;
	{
		std::scoped_lock lk(gOverlayMx);
		if (gOverlays.empty())
			return;
		snapshot = gOverlays;            // deep-copy, leave the source intact
	}

	auto& renderer = WanderSpire::SpriteRenderer::Get();
	auto& cam = WanderSpire::Application::GetCamera();

	renderer.BeginFrame(cam.GetViewProjectionMatrix());
	for (auto const& r : snapshot)
		renderer.DrawSprite(
			/*textureID*/ 0,
			/*centre   */{ r.x, r.y },
			/*size     */{ r.w, r.h },
			/*rot      */ 0.f,
			/*colour   */ DecodeColour(r.colour),
			/*UVs      */{ 0.f, 0.f }, { 1.f, 1.f });
	renderer.EndFrame();
}

// Helper to convert an std::vector<glm::ivec2> → JSON string.
static char* _marshalPathToJson(const std::vector<glm::ivec2>& checkpoints)
{
	// Build a JSON array of [x,y] pairs
	nlohmann::json j = nlohmann::json::array();
	for (const auto& p : checkpoints) {
		j.push_back(nlohmann::json::array({ p.x, p.y }));
	}
	std::string s = j.dump();
	// Allocate a C‐string via malloc (so C# can free it)
	char* out = (char*)std::malloc(s.size() + 1);
	if (!out) return nullptr;
	std::memcpy(out, s.c_str(), s.size() + 1);
	return out;
}

// Forward events into managed ScriptEvent bus…
static void WireUpScriptEventForwarding(Wrapper* w, EngineContextHandle vmHandle) {
	using namespace WanderSpire;
	auto& bus = EventBus::Get();

	// ── forward native LogicTickEvent into the script-side bus ─────────
	w->scriptEventSubscriptions.emplace_back(
		bus.Subscribe<LogicTickEvent>(
			[vmHandle](auto const& ev) {
				Script_PublishEvent(vmHandle, "LogicTickEvent", &ev, sizeof(ev));
			}
		)
	);

	w->scriptEventSubscriptions.emplace_back(
		bus.Subscribe<MoveStartedEvent>(
			[vmHandle](auto const& ev) {
				Script_PublishEvent(vmHandle, "MoveStartedEvent", &ev, sizeof(ev));
			}));
	w->scriptEventSubscriptions.emplace_back(
		bus.Subscribe<MoveCompletedEvent>(
			[vmHandle](auto const& ev) {
				Script_PublishEvent(vmHandle, "MoveCompletedEvent", &ev, sizeof(ev));
			}));
	w->scriptEventSubscriptions.emplace_back(
		bus.Subscribe<PathAppliedEvent>(
			[vmHandle](auto const& ev) {
				Script_PublishEvent(vmHandle, "PathAppliedEvent", &ev, sizeof(ev));
			}));
	w->scriptEventSubscriptions.emplace_back(
		bus.Subscribe<AnimationFinishedEvent>(
			[vmHandle](auto const& ev) {
				Script_PublishEvent(vmHandle, "AnimationFinishedEvent", &ev, sizeof(ev));
			}));
	w->scriptEventSubscriptions.emplace_back(
		bus.Subscribe<StateEnteredEvent>(
			[vmHandle](auto const& ev) {
				Script_PublishEvent(vmHandle, "StateEnteredEvent", &ev, sizeof(ev));
			}));
	w->scriptEventSubscriptions.emplace_back(
		bus.Subscribe<FrameRenderEvent>(
			[vmHandle](auto const& ev) {
				Script_PublishEvent(vmHandle, "FrameRenderEvent", &ev, sizeof(ev));
			}));
}

// Helper function for safe string copying
static void SafeStringCopy(const std::string& source, char* dest, int destSize) {
	if (dest && destSize > 0) {
		size_t copySize = std::min(static_cast<size_t>(destSize - 1), source.size());
		source.copy(dest, copySize);
		dest[copySize] = '\0';
	}
}

// Helper function to convert positions to array
static int ConvertPositionsToArray(const std::vector<glm::ivec2>& positions,
	int* outArray, int maxCount) {
	int count = std::min(static_cast<int>(positions.size()), maxCount / 2);
	for (int i = 0; i < count; ++i) {
		outArray[i * 2] = positions[i].x;
		outArray[i * 2 + 1] = positions[i].y;
	}
	return count;
}

// Helper to validate layer entity
static bool ValidateLayer(entt::registry& registry, entt::entity layer) {
	return registry.valid(layer) && registry.any_of<WanderSpire::TilemapLayerComponent>(layer);
}

// Helper to validate tilemap entity  
static bool ValidateTilemap(entt::registry& registry, entt::entity tilemap) {
	return registry.valid(tilemap) && registry.any_of<WanderSpire::SceneNodeComponent>(tilemap);
}

extern "C" {

	//=============================================================================
	// CORE ENGINE LIFECYCLE
	//=============================================================================

	ENGINE_API EngineContextHandle CreateEngineContext(void) {
		return new Wrapper{};
	}

	ENGINE_API void DestroyEngineContext(EngineContextHandle h) {
		delete static_cast<Wrapper*>(h);
	}

	ENGINE_API int EngineInit(EngineContextHandle h, int argc, char** argv) {
		auto* w = static_cast<Wrapper*>(h);
		int ret = WanderSpire::Application::AppInit(&w->appState, argc, argv);
		if (ret != SDL_APP_CONTINUE || !w->appState) return ret;
		auto* st = static_cast<WanderSpire::AppState*>(w->appState);
		w->world = &st->world;
		w->ctx = &st->ctx;
		WireUpScriptEventForwarding(w, h);
		return ret;
	}

	ENGINE_API void EngineQuit(EngineContextHandle h) {
		auto* w = static_cast<Wrapper*>(h);
		WanderSpire::Application::AppQuit(w->appState, SDL_APP_SUCCESS);
	}

	ENGINE_API int EngineIterate(EngineContextHandle h)
	{
		int rc = WanderSpire::Application::AppIterate(
			static_cast<Wrapper*>(h)->appState);
		return rc;
	}

	ENGINE_API int EngineEvent(EngineContextHandle h, void* raw) {
		return WanderSpire::Application::AppEvent(
			static_cast<Wrapper*>(h)->appState,
			static_cast<SDL_Event*>(raw));
	}

	ENGINE_API float Engine_GetTileSize(EngineContextHandle h) {
		return static_cast<Wrapper*>(h)->tileSize();
	}

	ENGINE_API SDL_Window* Engine_GetWindow(EngineContextHandle) {
		return SDL_GL_GetCurrentWindow();
	}

	ENGINE_API void Engine_GetWindowSize(EngineContextHandle h, int* outWidth, int* outHeight)
	{
		if (!h || !outWidth || !outHeight) {
			*outWidth = *outHeight = 0;
			return;
		}

		SDL_Window* window = SDL_GL_GetCurrentWindow();
		if (window) {
			SDL_GetWindowSizeInPixels(window, outWidth, outHeight);
		}
		else {
			*outWidth = *outHeight = 0;
		}
	}

	ENGINE_API void Engine_GetMouseTile(
		EngineContextHandle ctx,
		int* outTileX,
		int* outTileY)
	{
		if (!ctx || !outTileX || !outTileY) return;

		auto* w = static_cast<Wrapper*>(ctx);

		// 1) Window-space mouse position (SDL3 returns floats here)
		float px = 0.f, py = 0.f;
		SDL_GetMouseState(&px, &py);

		// 2) Window dimensions (pixels)
		int winW = 0, winH = 0;
		SDL_Window* win = SDL_GL_GetCurrentWindow();
		SDL_GetWindowSizeInPixels(win, &winW, &winH);

		// 3) Camera parameters
		auto& cam = WanderSpire::Application::GetCamera();
		float zoom = cam.GetZoom();
		glm::vec2 cp = cam.GetPosition();

		// 4) Screen → world
		float worldX = cp.x + (px - winW * 0.5f) / zoom;
		float worldY = cp.y + (py - winH * 0.5f) / zoom;

		// 5) World → grid tile
		const float ts = w->tileSize();
		*outTileX = static_cast<int>(std::floor(worldX / ts));
		*outTileY = static_cast<int>(std::floor(worldY / ts));
	}

	ENGINE_API float Engine_GetTickInterval(EngineContextHandle h)
	{
		auto* w = static_cast<Wrapper*>(h);
		return w->ctx->settings.tickInterval;
	}



	//=============================================================================
	// RENDER PIPELINE API
	//=============================================================================

	ENGINE_API void Render_SubmitSprite(
		EngineContextHandle ctx,
		uint32_t textureID,
		float posX, float posY,
		float sizeX, float sizeY,
		float rotation,
		float colorR, float colorG, float colorB,
		float uvOffsetX, float uvOffsetY,
		float uvSizeX, float uvSizeY,
		int layer,
		int order)
	{
		if (!ctx) return;

		auto& renderMgr = WanderSpire::RenderManager::Get();
		renderMgr.SubmitSprite(
			static_cast<GLuint>(textureID),
			{ posX, posY },
			{ sizeX, sizeY },
			rotation,
			{ colorR, colorG, colorB },
			{ uvOffsetX, uvOffsetY },
			{ uvSizeX, uvSizeY },
			static_cast<WanderSpire::RenderLayer>(layer),
			order
		);
	}

	ENGINE_API void Render_SubmitCustom(
		EngineContextHandle ctx,
		void (*callback)(void* userData),
		void* userData,
		int layer,
		int order)
	{
		if (!ctx || !callback) return;

		auto& renderMgr = WanderSpire::RenderManager::Get();
		renderMgr.SubmitCustom(
			[callback, userData]() { callback(userData); },
			static_cast<WanderSpire::RenderLayer>(layer),
			order
		);
	}

	ENGINE_API void Render_SubmitClear(
		EngineContextHandle ctx,
		float r, float g, float b)
	{
		if (!ctx) return;

		auto& renderMgr = WanderSpire::RenderManager::Get();
		renderMgr.SubmitClear({ r, g, b });
	}

	ENGINE_API int Render_GetCommandCount(EngineContextHandle ctx)
	{
		if (!ctx) return 0;

		auto& renderMgr = WanderSpire::RenderManager::Get();
		return static_cast<int>(renderMgr.GetCommandCount());
	}

	ENGINE_API void Render_ClearCommands(EngineContextHandle ctx)
	{
		if (!ctx) return;

		auto& renderMgr = WanderSpire::RenderManager::Get();
		renderMgr.Clear();
	}

	ENGINE_API void Render_ExecuteFrame(EngineContextHandle ctx)
	{
		if (!ctx) return;

		auto& renderMgr = WanderSpire::RenderManager::Get();
		renderMgr.ExecuteFrame();
	}

	ENGINE_API void Render_GetViewProjectionMatrix(
		EngineContextHandle ctx,
		float outMatrix[16])
	{
		if (!ctx || !outMatrix) return;

		const auto& cam = WanderSpire::Application::GetCamera();
		const auto& vp = cam.GetViewProjectionMatrix();

		// Copy matrix in column-major order (OpenGL format)
		for (int i = 0; i < 16; ++i) {
			outMatrix[i] = vp[i / 4][i % 4];
		}
	}

	ENGINE_API void Render_GetCameraBounds(
		EngineContextHandle ctx,
		float* outMinX, float* outMinY,
		float* outMaxX, float* outMaxY)
	{
		if (!ctx || !outMinX || !outMinY || !outMaxX || !outMaxY) return;

		const auto& cam = WanderSpire::Application::GetCamera();
		const float halfW = cam.GetWidth() * 0.5f / cam.GetZoom();
		const float halfH = cam.GetHeight() * 0.5f / cam.GetZoom();
		const glm::vec2 center = cam.GetPosition();

		*outMinX = center.x - halfW;
		*outMinY = center.y - halfH;
		*outMaxX = center.x + halfW;
		*outMaxY = center.y + halfH;
	}

	//=============================================================================
	// ENTITY MANAGEMENT API
	//=============================================================================

	ENGINE_API EntityId CreateEntity(EngineContextHandle h) {
		if (!h) {
			spdlog::error("[CreateEntity] Invalid context handle");
			return { WS_INVALID_ENTITY };
		}

		auto* w = static_cast<Wrapper*>(h);
		auto& reg = w->reg();

		try {
			entt::entity e = reg.create();

			// Always add an IDComponent with a unique UUID
			uint64_t uuid = g_nextUuid++;
			reg.emplace<WanderSpire::IDComponent>(e, uuid);

			// Add basic GridPositionComponent so the entity has a valid position
			reg.emplace<WanderSpire::GridPositionComponent>(e, glm::ivec2{ 0, 0 });

			// Add basic TransformComponent for world-space positioning
			WanderSpire::TransformComponent transform;
			transform.localPosition = glm::vec2{ 0.0f, 0.0f };
			transform.localRotation = 0.0f;
			transform.localScale = glm::vec2{ 1.0f, 1.0f };
			transform.worldPosition = transform.localPosition;
			transform.worldRotation = transform.localRotation;
			transform.worldScale = transform.localScale;
			transform.isDirty = true;
			reg.emplace<WanderSpire::TransformComponent>(e, transform);

			uint32_t raw = entt::to_integral(e);

			spdlog::debug("[CreateEntity] Created entity {} with UUID {}", raw, uuid);

			return { raw };
		}
		catch (const std::exception& ex) {
			spdlog::error("[CreateEntity] Exception: {}", ex.what());
			return { WS_INVALID_ENTITY };
		}
	}

	ENGINE_API void DestroyEntity(EngineContextHandle h, EntityId eid)
	{
		if (!h || is_null(eid.id)) return;
		auto& reg = static_cast<Wrapper*>(h)->reg();
		const entt::entity ent = entt::entity{ eid.id };
		if (!reg.valid(ent)) return;
		reg.destroy(ent);
	}

	ENGINE_API int Engine_GetAllEntities(
		EngineContextHandle h,
		uint32_t* outArr,
		int maxCount)
	{
		if (!h || !outArr || maxCount <= 0) return 0;
		auto& reg = static_cast<Wrapper*>(h)->reg();
		int count = 0;
		for (auto ent : reg.view<entt::entity>()) {
			if (count >= maxCount) break;
			uint32_t raw = entt::to_integral(ent);
			outArr[count++] = raw;
		}
		return count;
	}

	ENGINE_API void Engine_GetEntityWorldPosition(
		EngineContextHandle h,
		EntityId            eid,
		float* outX,
		float* outY)
	{
		if (!h || !outX || !outY) return;
		auto* w = static_cast<Wrapper*>(h);
		auto& reg = w->reg();
		auto ent = static_cast<entt::entity>(eid.id);

		if (!reg.valid(ent)) {
			*outX = *outY = 0.f;
			return;
		}

		// Try GridPositionComponent first
		if (auto* gp = reg.try_get<WanderSpire::GridPositionComponent>(ent)) {
			float tileSz = w->tileSize();
			glm::vec2 centre = glm::vec2(gp->tile) * tileSz + glm::vec2(tileSz * 0.5f);
			*outX = centre.x;
			*outY = centre.y;
			return;
		}

		// Fall back to TransformComponent if available
		if (auto* transform = reg.try_get<WanderSpire::TransformComponent>(ent)) {
			*outX = transform->localPosition.x;
			*outY = transform->localPosition.y;
			return;
		}

		// No position components found, return origin
		*outX = *outY = 0.f;
	}

	//=============================================================================
	// COMPONENT REFLECTION API
	//=============================================================================

	ENGINE_API int HasComponent(EngineContextHandle h,
		EntityId          eid,
		const char* compName)
	{
		if (!h || !compName || is_null(eid.id))
			return 0;

		auto& reg = static_cast<Wrapper*>(h)->reg();
		const entt::entity ent = static_cast<entt::entity>(eid.id);
		if (!is_valid(reg, ent))
			return 0;

		const Reflect::TypeInfo* ti = nullptr;
		findField(compName, "", &ti);
		if (!ti || !ti->saveFn)
			return 0;

		json j; ti->saveFn(reg, ent, j);
		return j.contains(ti->name) ? 1 : 0;
	}

	ENGINE_API int GetComponentField(EngineContextHandle h, EntityId eid,
		const char* comp, const char* field,
		void* outBuf, int bufSize)
	{
		if (!h || !comp || !field || !outBuf || bufSize <= 0 || is_null(eid.id))
			return -1;

		auto& reg = static_cast<Wrapper*>(h)->reg();
		const entt::entity ent = static_cast<entt::entity>(eid.id);
		if (!is_valid(reg, ent))
			return -2;

		const Reflect::TypeInfo* ti = nullptr;
		auto fi = findField(comp, field, &ti);
		if (!fi || !ti->saveFn)
			return -3;

		json j; ti->saveFn(reg, ent, j);
		if (!j.contains(ti->name) || !j[ti->name].contains(field))
			return -4;

		const auto& val = j[ti->name][field];
		if (fi->type == Reflect::FieldType::Float && bufSize >= sizeof(float))
		{
			*reinterpret_cast<float*>(outBuf) = val.get<float>();  return sizeof(float);
		}

		if (fi->type == Reflect::FieldType::Int && bufSize >= sizeof(int))
		{
			*reinterpret_cast<int*>(outBuf) = val.get<int>();    return sizeof(int);
		}

		if (fi->type == Reflect::FieldType::Bool && bufSize >= sizeof(int))
		{
			*reinterpret_cast<int*>(outBuf) = val.get<bool>() ? 1 : 0; return sizeof(int);
		}

		return -5;
	}

	ENGINE_API int SetComponentField(EngineContextHandle h, EntityId eid,
		const char* comp, const char* field,
		const void* data, int dataSize)
	{
		if (!h || !comp || !field || !data || dataSize <= 0 || is_null(eid.id))
			return -1;

		auto& reg = static_cast<Wrapper*>(h)->reg();
		const entt::entity ent = static_cast<entt::entity>(eid.id);
		if (!is_valid(reg, ent))
			return -2;

		const Reflect::TypeInfo* ti = nullptr;
		auto fi = findField(comp, field, &ti);
		if (!fi || !ti->loadFn)
			return -3;

		json node;
		switch (fi->type)
		{
		case Reflect::FieldType::Float:
			if (dataSize < sizeof(float)) return -4;
			node = *reinterpret_cast<const float*>(data); break;
		case Reflect::FieldType::Int:
			if (dataSize < sizeof(int))   return -4;
			node = *reinterpret_cast<const int*>(data);   break;
		case Reflect::FieldType::Bool:
			if (dataSize < sizeof(int))   return -4;
			node = (*reinterpret_cast<const int*>(data) != 0); break;
		default: return -5;
		}

		json wrapper; wrapper[ti->name][field] = std::move(node);
		ti->loadFn(reg, ent, wrapper);
		return 0;
	}

	ENGINE_API int SetComponentJson(EngineContextHandle h, EntityId eid,
		const char* comp, const char* jsonStr)
	{
		if (!h || !comp || !jsonStr || is_null(eid.id))
			return -1;

		auto& reg = static_cast<Wrapper*>(h)->reg();
		const entt::entity ent = static_cast<entt::entity>(eid.id);
		if (!is_valid(reg, ent))
			return -2;

		const Reflect::TypeInfo* ti = nullptr;
		findField(comp, "", &ti);
		if (!ti || !ti->loadFn)
			return -3;

		json node = json::parse(jsonStr, nullptr, false);
		if (!node.is_object()) return -4;

		json wrapper; wrapper[ti->name] = std::move(node);
		ti->loadFn(reg, ent, wrapper);
		return 0;
	}

	ENGINE_API int GetComponentJson(
		EngineContextHandle h,
		EntityId e,
		const char* compName,
		char* outJson,
		int outJsonSize
	) {
		if (!h || !compName || !outJson || outJsonSize <= 0) return -1;

		auto& reg = static_cast<Wrapper*>(h)->reg();

		entt::entity ent = static_cast<entt::entity>(e.id);
		if (!reg.valid(ent))
			return -1;

		const Reflect::TypeInfo* ti = nullptr;
		findField(compName, "", &ti);
		if (!ti || !ti->saveFn) return -1;

		nlohmann::json j;
		ti->saveFn(reg, static_cast<entt::entity>(e.id), j);

		if (!j.contains(ti->name)) return -2; // No such component
		auto& node = j[ti->name];
		std::string jsonStr = node.dump();

		int len = int(jsonStr.size());
		if (len + 1 > outJsonSize) return -3; // Buffer too small

		std::memcpy(outJson, jsonStr.c_str(), len + 1);
		return len;
	}

	ENGINE_API int RemoveComponent(
		EngineContextHandle h,
		EntityId            eid,
		const char* compName)
	{
		if (!h || !compName || is_null(eid.id))
			return -1;

		auto& reg = static_cast<Wrapper*>(h)->reg();
		const entt::entity ent = static_cast<entt::entity>(eid.id);

		if (!reg.valid(ent))
			return 0;

		// Helper macro – expanded once per reflected component
#define X(COMP)                                                                   \
    if (std::strcmp(compName, #COMP) == 0)                                        \
    {                                                                             \
        if (reg.all_of<WanderSpire::COMP>(ent))                                   \
            reg.remove<WanderSpire::COMP>(ent);                                   \
        return 0;                                                                 \
    }

		SERIALIZABLE_COMPONENTS       // expands to a block of X(COMP) lines
#undef  X

			return -3;                                 // unknown component
	}

	//=============================================================================
	// SCRIPT DATA API
	//=============================================================================

	ENGINE_API int GetScriptDataValue(EngineContextHandle h,
		EntityId          eid,
		const char* key,
		char* outJson,
		int outJsonSize)
	{
		if (!h || !key || !outJson || outJsonSize <= 0 || is_null(eid.id))
			return -1;

		auto& reg = static_cast<Wrapper*>(h)->reg();
		const entt::entity ent = static_cast<entt::entity>(eid.id);
		if (!is_valid(reg, ent))
			return -2;

		if (!reg.all_of<WanderSpire::ScriptDataComponent>(ent))
			return -3;

		auto& comp = reg.get<WanderSpire::ScriptDataComponent>(ent);

		nlohmann::json j;
		try {
			j = nlohmann::json::parse(comp.data, nullptr, false);
			if (j.is_discarded()) return -4;
		}
		catch (...) { return -4; }

		if (!j.contains(key)) return -5;
		std::string valStr = j[key].dump();
		if (int(valStr.size()) + 1 > outJsonSize) return -6;

		std::memcpy(outJson, valStr.c_str(), valStr.size() + 1);
		return static_cast<int>(valStr.size());
	}

	ENGINE_API int SetScriptDataValue(EngineContextHandle h,
		EntityId          eid,
		const char* key,
		const char* jsonValue)
	{
		if (!h || !key || !jsonValue || is_null(eid.id))
			return -1;

		auto& reg = static_cast<Wrapper*>(h)->reg();
		const entt::entity ent = static_cast<entt::entity>(eid.id);
		if (!is_valid(reg, ent))
			return -2;

		WanderSpire::ScriptDataComponent* comp;
		if (auto ptr = reg.try_get<WanderSpire::ScriptDataComponent>(ent))
			comp = ptr;
		else
			comp = &reg.emplace<WanderSpire::ScriptDataComponent>(ent);

		nlohmann::json j;
		try {
			if (!comp->data.empty())
				j = nlohmann::json::parse(comp->data, nullptr, false);
			if (j.is_discarded() || !j.is_object())
				j = nlohmann::json::object();
		}
		catch (...) { j = nlohmann::json::object(); }

		nlohmann::json val = nlohmann::json::parse(jsonValue, nullptr, false);
		if (val.is_discarded())
			return -3;

		j[key] = std::move(val);
		comp->data = j.dump();
		return 0;
	}

	ENGINE_API int RemoveScriptDataValue(EngineContextHandle h,
		EntityId          eid,
		const char* key)
	{
		if (!h || !key || is_null(eid.id))
			return -1;

		auto& reg = static_cast<Wrapper*>(h)->reg();
		const entt::entity ent = static_cast<entt::entity>(eid.id);
		if (!is_valid(reg, ent))
			return -2;

		if (!reg.all_of<WanderSpire::ScriptDataComponent>(ent))
			return -3;

		auto& comp = reg.get<WanderSpire::ScriptDataComponent>(ent);

		nlohmann::json j;
		try {
			j = nlohmann::json::parse(comp.data, nullptr, false);
			if (j.is_discarded()) return -4;
		}
		catch (...) { return -4; }

		if (!j.contains(key)) return -5;
		j.erase(key);
		comp.data = j.dump();
		return 0;
	}

	//=============================================================================
	// PREFAB SYSTEM API
	//=============================================================================

	ENGINE_API EntityId Prefab_InstantiateAtTile(
		EngineContextHandle h,
		const char* prefabName,
		int tileX,
		int tileY)
	{
		auto* w = static_cast<Wrapper*>(h);
		auto& reg = w->reg();

		entt::entity e = WanderSpire::PrefabManager::GetInstance()
			.Instantiate(prefabName, reg, glm::vec2{ float(tileX), float(tileY) });

		if (e == entt::null) {
			spdlog::error("[Prefab_InstantiateAtTile] Failed to instantiate prefab '{}'", prefabName);
			return { WS_INVALID_ENTITY };
		}

		// Update grid position if the component exists
		if (auto* gp = reg.try_get<WanderSpire::GridPositionComponent>(e)) {
			gp->tile[0] = tileX;
			gp->tile[1] = tileY;
		}

		// Ensure the entity has an IDComponent (use emplace_or_replace to avoid conflicts)
		uint64_t uuid = g_nextUuid++;
		reg.emplace_or_replace<WanderSpire::IDComponent>(e, uuid);

		uint32_t raw = entt::to_integral(e);
		spdlog::debug("[Prefab_InstantiateAtTile] Created entity {} from prefab '{}' at tile ({}, {})",
			raw, prefabName, tileX, tileY);

		return { raw };
	}

	ENGINE_API EntityId InstantiatePrefab(
		EngineContextHandle h,
		const char* prefabName,
		float worldX,
		float worldY)
	{
		auto* w = static_cast<Wrapper*>(h);
		auto& reg = w->reg();
		entt::entity e = WanderSpire::PrefabManager::GetInstance()
			.Instantiate(prefabName, reg, glm::vec2{ worldX, worldY });
		uint64_t uuid = g_nextUuid++;
		reg.emplace<WanderSpire::IDComponent>(e, uuid);
		uint32_t raw = entt::to_integral(e);
		return { raw };
	}

	//=============================================================================
	// EVENT SYSTEM API
	//=============================================================================

	ENGINE_API void Script_SubscribeEvent(
		EngineContextHandle h,
		const char* eventName,
		ScriptEventCallback cb,
		void* userData)
	{
		if (!h || !eventName || !cb) return;
		auto* w = static_cast<Wrapper*>(h);
		std::scoped_lock lk(w->scriptSlotsMx);
		w->scriptSlots[eventName].push_back({ cb, userData });
	}

	ENGINE_API void Script_PublishEvent(
		EngineContextHandle h,
		const char* eventName,
		const void* payload,
		int payloadSize)
	{
		if (!h || !eventName) return;
		auto* w = static_cast<Wrapper*>(h);

		// Copy slot list to avoid issues with modification during iteration
		std::vector<Wrapper::ScriptSlot> listeners;
		{
			std::scoped_lock lk(w->scriptSlotsMx);
			// 1) exact‐match subscribers
			if (auto it = w->scriptSlots.find(eventName); it != w->scriptSlots.end())
				listeners.insert(listeners.end(), it->second.begin(), it->second.end());
			// 2) wildcard "*" subscribers
			if (auto it2 = w->scriptSlots.find("*"); it2 != w->scriptSlots.end())
				listeners.insert(listeners.end(), it2->second.begin(), it2->second.end());
		}

		// Call all script listeners
		for (auto& slot : listeners)
			slot.fn(eventName, payload, payloadSize, slot.user);
	}

	//=============================================================================
	// CAMERA API
	//=============================================================================

	ENGINE_API void Engine_SetPlayerEntity(
		EngineContextHandle h,
		EntityId player)
	{
		auto* w = static_cast<Wrapper*>(h);
		auto* state = static_cast<WanderSpire::AppState*>(w->appState);
		state->player = static_cast<entt::entity>(player.id);
	}

	ENGINE_API void Engine_SetCameraTarget(
		EngineContextHandle h,
		EntityId target)
	{
		auto* w = static_cast<Wrapper*>(h);
		auto* state = static_cast<WanderSpire::AppState*>(w->appState);
		state->cameraTarget = static_cast<entt::entity>(target.id);
	}

	ENGINE_API void Engine_ClearCameraTarget(EngineContextHandle h) {
		auto* w = static_cast<Wrapper*>(h);
		auto* state = static_cast<WanderSpire::AppState*>(w->appState);
		state->cameraTarget = entt::null;
	}

	ENGINE_API void Engine_SetCameraPosition(
		EngineContextHandle /*h*/,
		float worldX,
		float worldY)
	{
		WanderSpire::Application::GetCamera().SetPosition({ worldX, worldY });
	}

	//=============================================================================
	// OVERLAY RENDERING API
	//=============================================================================

	ENGINE_API void Engine_OverlayClear(EngineContextHandle /*ctx*/)
	{
		std::scoped_lock lk(gOverlayMx);
		gOverlays.clear();
	}

	ENGINE_API void Engine_OverlayRect(EngineContextHandle /*ctx*/,
		float wx, float wy,
		float w, float h,
		uint32_t colour)
	{
		std::scoped_lock lk(gOverlayMx);
		gOverlays.push_back({ wx,wy,w,h,colour });
	}

	ENGINE_API void Engine_OverlayPresent(void)
	{
		FlushOverlayBatch();
	}

	//=============================================================================
	// PATHFINDING API
	//=============================================================================

	ENGINE_API char* Engine_FindPath(
		EngineContextHandle h,
		int startX, int startY,
		int targetX, int targetY,
		int maxRange)
	{
		try {
			auto* w = static_cast<Wrapper*>(h);
			if (!w) {
				nlohmann::json pathJson = nlohmann::json::array();
				pathJson.push_back({ startX, startY });
				if (startX != targetX || startY != targetY) {
					pathJson.push_back({ targetX, targetY });
				}
				std::string jsonStr = pathJson.dump();
				char* cStr = static_cast<char*>(malloc(jsonStr.length() + 1));
				if (cStr) {
					std::strcpy(cStr, jsonStr.c_str());
				}
				return cStr;
			}

			auto& registry = w->reg();

			auto result = WanderSpire::Pathfinder2D::FindPath(
				glm::ivec2{ startX, startY },
				glm::ivec2{ targetX, targetY },
				maxRange,
				registry,
				entt::null  // Auto-find tilemap layer
			);

			if (result.fullPath.empty()) {
				result.fullPath.push_back(glm::ivec2{ startX, startY });
				if (startX != targetX || startY != targetY) {
					result.fullPath.push_back(glm::ivec2{ targetX, targetY });
				}
			}

			nlohmann::json pathJson = nlohmann::json::array();
			for (const auto& tile : result.fullPath) {
				pathJson.push_back({ tile.x, tile.y });
			}

			std::string jsonStr = pathJson.dump();

			char* cStr = static_cast<char*>(malloc(jsonStr.length() + 1));
			if (cStr) {
				std::strcpy(cStr, jsonStr.c_str());
			}
			return cStr;
		}
		catch (...) {
			nlohmann::json pathJson = nlohmann::json::array();
			pathJson.push_back({ startX, startY });
			if (startX != targetX || startY != targetY) {
				pathJson.push_back({ targetX, targetY });
			}
			std::string jsonStr = pathJson.dump();
			char* cStr = static_cast<char*>(malloc(jsonStr.length() + 1));
			if (cStr) {
				std::strcpy(cStr, jsonStr.c_str());
			}
			return cStr;
		}
	}

	ENGINE_API char* Engine_FindPathAdvanced(
		EngineContextHandle h,
		int                 startX,
		int                 startY,
		int                 targetX,
		int                 targetY,
		int                 maxRange,
		EntityId            tilemapLayer)
	{
		auto* w = static_cast<Wrapper*>(h);
		auto* state = asAppState(w);
		if (!state) {
			return _marshalPathToJson({});
		}

		auto& registry = state->world.GetRegistry();
		entt::entity layerEntity = static_cast<entt::entity>(tilemapLayer.id);

		if (!registry.valid(layerEntity)) {
			return _marshalPathToJson({});
		}

		auto result = WanderSpire::Pathfinder2D::FindPath(
			glm::ivec2(startX, startY),
			glm::ivec2(targetX, targetY),
			maxRange,
			registry,
			layerEntity
		);

		return _marshalPathToJson(result.fullPath);
	}

	ENGINE_API void Engine_FreeString(char* str) {
		std::free(str);
	}

	//=============================================================================
	// SCENE MANAGEMENT API
	//=============================================================================

	ENGINE_API void SceneManager_SaveScene(EngineContextHandle wrapperHandle, const char* path) {
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(wrapperHandle);

		WanderSpire::Scene::SceneMetadata metadata{
			.name = std::filesystem::path(path).stem().string(),
			.version = "2.0"
		};

		auto result = w->ctx->sceneManager->SaveScene(path, w->reg(), metadata);

		if (!result.success) {
			spdlog::error("[SceneAPI] Save failed: {}", result.error);
		}
	}

	ENGINE_API bool SceneManager_LoadScene(
		EngineContextHandle wrapperHandle,
		const char* path,
		uint32_t* outPlayer,
		float* outPlayerX,
		float* outPlayerY,
		uint32_t* outMainTilemap
	) {
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(wrapperHandle);
		auto* state = static_cast<WanderSpire::AppState*>(w->appState);

		auto result = w->ctx->sceneManager->LoadScene(path, w->reg());

		if (!result.success) {
			spdlog::error("[SceneAPI] Load failed: {}", result.error);

			if (outPlayer) *outPlayer = 0;
			if (outPlayerX) *outPlayerX = 0.0f;
			if (outPlayerY) *outPlayerY = 0.0f;
			if (outMainTilemap) *outMainTilemap = 0;
			return false;
		}

		if (result.playerEntity != entt::null) {
			state->SetPlayer(result.playerEntity);
			if (outPlayer) *outPlayer = entity_to_uint32(result.playerEntity);
			if (outPlayerX) *outPlayerX = result.playerPosition.x;
			if (outPlayerY) *outPlayerY = result.playerPosition.y;
		}
		else {
			if (outPlayer) *outPlayer = 0;
			if (outPlayerX) *outPlayerX = 0.0f;
			if (outPlayerY) *outPlayerY = 0.0f;
		}

		if (result.mainTilemap != entt::null) {
			state->SetMainTilemap(result.mainTilemap);
			if (outMainTilemap) *outMainTilemap = entity_to_uint32(result.mainTilemap);
		}
		else {
			if (outMainTilemap) *outMainTilemap = 0;
		}

		using namespace WanderSpire;
		EventBus::Get().Publish(FrameRenderEvent{ state });

		return true;
	}

	ENGINE_API bool SceneManager_LoadTilemap(
		EngineContextHandle wrapperHandle,
		const char* path,
		float positionX,
		float positionY,
		uint32_t* outTilemap
	) {
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(wrapperHandle);

		auto result = w->ctx->sceneManager->LoadTilemap(path, w->reg(), { positionX, positionY });

		if (!result.success) {
			spdlog::error("[SceneAPI] Tilemap load failed: {}", result.error);
			if (outTilemap) *outTilemap = 0;
			return false;
		}

		if (outTilemap) {
			*outTilemap = (result.mainTilemap != entt::null) ?
				entity_to_uint32(result.mainTilemap) : 0;
		}

		return true;
	}

	ENGINE_API bool SceneManager_SaveTilemap(
		EngineContextHandle wrapperHandle,
		const char* path,
		uint32_t tilemapEntity
	) {
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(wrapperHandle);
		entt::entity tilemap = static_cast<entt::entity>(tilemapEntity);

		auto result = w->ctx->sceneManager->SaveTilemap(path, w->reg(), tilemap);

		if (!result.success) {
			spdlog::error("[SceneAPI] Tilemap save failed: {}", result.error);
			return false;
		}

		return true;
	}

	ENGINE_API int SceneManager_GetSupportedFormatsCount(EngineContextHandle wrapperHandle, bool forLoading) {
		auto* w = static_cast<EngineCoreInternal::Wrapper*>(wrapperHandle);
		if (forLoading) {
			return static_cast<int>(w->ctx->sceneManager->GetSupportedLoadFormats().size());
		}
		else {
			return static_cast<int>(w->ctx->sceneManager->GetSupportedSaveFormats().size());
		}
	}

	//=============================================================================
	// IMGUI INTEGRATION API
	//=============================================================================

	ENGINE_API int ImGui_Initialize(EngineContextHandle h)
	{
		if (!h) return -1;
		IMGUI_CHECKVERSION();
		ImGui::CreateContext();
		ImGuiIO& io = ImGui::GetIO();
		io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard |
			ImGuiConfigFlags_NavEnableGamepad;
		io.Fonts->AddFontDefault();

		const char* font_path = FONT_ICON_FILE_NAME_FAS;
		std::ifstream f(font_path, std::ios::binary | std::ios::ate);
		if (!f)
		{
			spdlog::error("[ImGui] FontAwesome file NOT found: '{}'", font_path);
		}
		else
		{
			std::streamsize file_size = f.tellg();
			f.close();
			spdlog::info("[ImGui] FontAwesome file found: '{}' (size: {} bytes)", font_path, file_size);

			if (file_size < 1000)
			{
				spdlog::error("[ImGui] FontAwesome file too small: {} bytes", file_size);
			}
			else
			{
				float baseFontSize = 13.0f;
				float iconFontSize = baseFontSize * 2.0f / 3.0f;

				ImFontConfig cfg;
				cfg.MergeMode = true;
				cfg.PixelSnapH = true;
				cfg.OversampleH = 1;
				cfg.OversampleV = 1;

				static const ImWchar fa_ranges[] = { 0xf000, 0xf8ff, 0 };

				spdlog::info("[ImGui] Loading FontAwesome with range 0x{:04X}-0x{:04X}", fa_ranges[0], fa_ranges[1]);

				g_FontAwesome = io.Fonts->AddFontFromFileTTF(font_path, iconFontSize, &cfg, fa_ranges);

				if (g_FontAwesome)
				{
					spdlog::info("[ImGui] FontAwesome font object created successfully");

					bool atlas_built = io.Fonts->Build();
					spdlog::info("[ImGui] Atlas build result: {}", atlas_built ? "SUCCESS" : "FAILED");

					if (atlas_built)
					{
						spdlog::info("[ImGui] FontAwesome parsed – {} glyphs", g_FontAwesome->Glyphs.Size);

						if (g_FontAwesome->Glyphs.Size > 0)
						{
							ImWchar min_codepoint = g_FontAwesome->Glyphs[0].Codepoint;
							ImWchar max_codepoint = g_FontAwesome->Glyphs[g_FontAwesome->Glyphs.Size - 1].Codepoint;
							spdlog::info("[ImGui] Actual glyph range loaded: 0x{:04X} - 0x{:04X}", min_codepoint, max_codepoint);
						}
					}
					else
					{
						spdlog::error("[ImGui] FontAwesome atlas build FAILED");
						g_FontAwesome = nullptr;
					}
				}
				else
				{
					spdlog::error("[ImGui] FontAwesome AddFontFromFileTTF FAILED - font file may be corrupted or invalid");
				}
			}
		}

		SDL_Window* win = SDL_GL_GetCurrentWindow();
		if (!win) { ImGui::DestroyContext(); return -3; }
		if (!ImGui_ImplSDL3_InitForOpenGL(win, SDL_GL_GetCurrentContext()))
		{
			ImGui::DestroyContext(); return -4;
		}
		if (!ImGui_ImplOpenGL3_Init("#version 330 core"))
		{
			ImGui_ImplSDL3_Shutdown(); ImGui::DestroyContext(); return -5;
		}

		io.Fonts->Build();
		ImGui_ImplOpenGL3_CreateFontsTexture();
		ImGui::StyleColorsDark();
		spdlog::info("[ImGui] initialise complete (default{} icons)",
			g_FontAwesome ? " + " : " – NO ");
		return 0;
	}

	ENGINE_API void* ImGui_GetFontAwesome()
	{
		return g_FontAwesome;
	}

	ENGINE_API void ImGui_Shutdown(EngineContextHandle h)
	{
		if (!h) return;

		ImGui_ImplOpenGL3_Shutdown();
		ImGui_ImplSDL3_Shutdown();
		ImGui::DestroyContext();

		spdlog::info("[ImGui] Shutdown complete");
	}

	ENGINE_API int ImGui_ProcessEvent(EngineContextHandle h, void* sdlEvent)
	{
		if (!h || !sdlEvent) return 0;

		SDL_Event* event = static_cast<SDL_Event*>(sdlEvent);
		return ImGui_ImplSDL3_ProcessEvent(event) ? 1 : 0;
	}

	ENGINE_API void ImGui_NewFrame(EngineContextHandle h)
	{
		if (!h) return;

		ImGui_ImplOpenGL3_NewFrame();
		ImGui_ImplSDL3_NewFrame();
		ImGui::NewFrame();
	}

	ENGINE_API void ImGui_Render(EngineContextHandle h)
	{
		if (!h) return;

		ImGui::Render();
		ImGui_ImplOpenGL3_RenderDrawData(ImGui::GetDrawData());
	}

	ENGINE_API int ImGui_WantCaptureMouse(EngineContextHandle h)
	{
		if (!h) return 0;
		return ImGui::GetIO().WantCaptureMouse ? 1 : 0;
	}

	ENGINE_API int ImGui_WantCaptureKeyboard(EngineContextHandle h)
	{
		if (!h) return 0;
		return ImGui::GetIO().WantCaptureKeyboard ? 1 : 0;
	}

	ENGINE_API void ImGui_SetDisplaySize(EngineContextHandle h, float width, float height)
	{
		if (!h) return;
		ImGui::GetIO().DisplaySize = ImVec2(width, height);
	}

	ENGINE_API void ImGui_SetDockingEnabled(int enabled)
	{
		ImGuiIO& io = ImGui::GetIO();
		if (enabled)
			io.ConfigFlags |= ImGuiConfigFlags_DockingEnable;
		else
			io.ConfigFlags &= ~ImGuiConfigFlags_DockingEnable;
	}

	//=============================================================================
	// SCENE HIERARCHY API IMPLEMENTATION
	//=============================================================================

	ENGINE_API EntityId SceneHierarchy_CreateGameObject(
		EngineContextHandle ctx,
		const char* name,
		EntityId parent)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return { WS_INVALID_ENTITY };

		auto& registry = w->reg();
		std::string objectName = name ? name : "GameObject";

		entt::entity entity = WanderSpire::SceneHierarchyManager::GetInstance().CreateGameObject(registry, objectName);

		if (parent.id != WS_INVALID_ENTITY) {
			entt::entity parentEntity = static_cast<entt::entity>(parent.id);
			if (registry.valid(parentEntity)) {
				WanderSpire::SceneHierarchyManager::GetInstance().SetParent(registry, entity, parentEntity);
			}
		}

		return { entt::to_integral(entity) };
	}

	ENGINE_API int SceneHierarchy_SetParent(
		EngineContextHandle ctx,
		EntityId child,
		EntityId parent)
	{
		auto* w = GetWrapper(ctx);
		if (!w || child.id == WS_INVALID_ENTITY) return -1;

		auto& registry = w->reg();
		entt::entity childEntity = static_cast<entt::entity>(child.id);
		entt::entity parentEntity = (parent.id == WS_INVALID_ENTITY) ? entt::null : static_cast<entt::entity>(parent.id);

		if (!registry.valid(childEntity)) return -1;
		if (parentEntity != entt::null && !registry.valid(parentEntity)) return -1;

		WanderSpire::SceneHierarchyManager::GetInstance().SetParent(registry, childEntity, parentEntity);
		return 0;
	}

	ENGINE_API int SceneHierarchy_GetChildren(
		EngineContextHandle ctx,
		EntityId parent,
		uint32_t* outChildren,
		int maxCount)
	{
		auto* w = GetWrapper(ctx);
		if (!w || !outChildren || maxCount <= 0) return 0;

		auto& registry = w->reg();
		entt::entity parentEntity = static_cast<entt::entity>(parent.id);

		if (!registry.valid(parentEntity)) return 0;

		auto children = WanderSpire::SceneHierarchyManager::GetInstance().GetChildren(registry, parentEntity);
		int count = std::min(static_cast<int>(children.size()), maxCount);

		for (int i = 0; i < count; ++i) {
			outChildren[i] = entt::to_integral(children[i]);
		}

		return count;
	}

	ENGINE_API EntityId SceneHierarchy_GetParent(
		EngineContextHandle ctx,
		EntityId child)
	{
		auto* w = GetWrapper(ctx);
		if (!w || child.id == WS_INVALID_ENTITY) return { WS_INVALID_ENTITY };

		auto& registry = w->reg();
		entt::entity childEntity = static_cast<entt::entity>(child.id);

		if (!registry.valid(childEntity)) return { WS_INVALID_ENTITY };

		entt::entity parent = WanderSpire::SceneHierarchyManager::GetInstance().GetParent(registry, childEntity);
		return { parent == entt::null ? WS_INVALID_ENTITY : entt::to_integral(parent) };
	}

	ENGINE_API int SceneHierarchy_GetRootObjects(
		EngineContextHandle ctx,
		uint32_t* outRoots,
		int maxCount)
	{
		auto* w = GetWrapper(ctx);
		if (!w || !outRoots || maxCount <= 0) return 0;

		auto& registry = w->reg();
		auto roots = WanderSpire::SceneHierarchyManager::GetInstance().GetRootObjects(registry);
		int count = std::min(static_cast<int>(roots.size()), maxCount);

		for (int i = 0; i < count; ++i) {
			outRoots[i] = entt::to_integral(roots[i]);
		}

		return count;
	}

	//=============================================================================
	// SELECTION API IMPLEMENTATION
	//=============================================================================

	ENGINE_API void Selection_SelectEntity(EngineContextHandle ctx, EntityId entity) {
		auto* w = GetWrapper(ctx);
		if (!w || entity.id == WS_INVALID_ENTITY) return;

		auto& registry = w->reg();
		entt::entity ent = static_cast<entt::entity>(entity.id);

		if (registry.valid(ent)) {
			WanderSpire::SelectionManager::GetInstance().SelectEntity(registry, ent);
		}
	}

	ENGINE_API void Selection_AddToSelection(EngineContextHandle ctx, EntityId entity) {
		auto* w = GetWrapper(ctx);
		if (!w || entity.id == WS_INVALID_ENTITY) return;

		auto& registry = w->reg();
		entt::entity ent = static_cast<entt::entity>(entity.id);

		if (registry.valid(ent)) {
			WanderSpire::SelectionManager::GetInstance().AddToSelection(registry, ent);
		}
	}

	ENGINE_API void Selection_DeselectAll(EngineContextHandle ctx) {
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		WanderSpire::SelectionManager::GetInstance().DeselectAll(registry);
	}

	ENGINE_API int Selection_GetSelectedEntities(
		EngineContextHandle ctx,
		uint32_t* outEntities,
		int maxCount)
	{
		auto* w = GetWrapper(ctx);
		if (!w || !outEntities || maxCount <= 0) return 0;

		const auto& selected = WanderSpire::SelectionManager::GetInstance().GetSelectedEntities();
		int count = std::min(static_cast<int>(selected.size()), maxCount);

		int i = 0;
		for (auto entity : selected) {
			if (i >= count) break;
			outEntities[i++] = entt::to_integral(entity);
		}

		return count;
	}

	ENGINE_API int Selection_SelectInBounds(
		EngineContextHandle ctx,
		float minX, float minY,
		float maxX, float maxY)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return 0;

		auto& registry = w->reg();
		WanderSpire::SelectionManager::GetInstance().SelectInBounds(registry, { minX, minY }, { maxX, maxY });

		return WanderSpire::SelectionManager::GetInstance().GetSelectionCount();
	}

	//=============================================================================
	// LAYER MANAGEMENT API IMPLEMENTATION
	//=============================================================================

	ENGINE_API int Layer_Create(EngineContextHandle ctx, const char* name) {
		if (!name) return -1;

		return WanderSpire::LayerManager::GetInstance().CreateLayer(name);
	}

	ENGINE_API void Layer_Remove(EngineContextHandle ctx, int layerId) {
		WanderSpire::LayerManager::GetInstance().RemoveLayer(layerId);
	}

	ENGINE_API void Layer_SetVisible(EngineContextHandle ctx, int layerId, int visible) {
		WanderSpire::LayerManager::GetInstance().SetLayerVisible(layerId, visible != 0);
	}

	ENGINE_API void Layer_SetEntityLayer(EngineContextHandle ctx, EntityId entity, int layerId) {
		auto* w = GetWrapper(ctx);
		if (!w || entity.id == WS_INVALID_ENTITY) return;

		auto& registry = w->reg();
		entt::entity ent = static_cast<entt::entity>(entity.id);

		if (registry.valid(ent)) {
			auto& layer = registry.get_or_emplace<WanderSpire::LayerComponent>(ent);
			layer.renderLayer = layerId;

			const auto* layerInfo = WanderSpire::LayerManager::GetInstance().GetLayer(layerId);
			if (layerInfo) {
				layer.layerName = layerInfo->name;
			}
		}
	}

	ENGINE_API int Layer_GetEntityLayer(EngineContextHandle ctx, EntityId entity) {
		auto* w = GetWrapper(ctx);
		if (!w || entity.id == WS_INVALID_ENTITY) return 0;

		auto& registry = w->reg();
		entt::entity ent = static_cast<entt::entity>(entity.id);

		if (registry.valid(ent)) {
			auto* layer = registry.try_get<WanderSpire::LayerComponent>(ent);
			return layer ? layer->renderLayer : 0;
		}

		return 0;
	}

	//=============================================================================
	// TILEMAP API IMPLEMENTATION
	//=============================================================================

	ENGINE_API EntityId Tilemap_Create(EngineContextHandle ctx, const char* name) {
		auto* w = GetWrapper(ctx);
		if (!w) return { WS_INVALID_ENTITY };

		auto& registry = w->reg();
		std::string tilemapName = name ? name : "Tilemap";

		entt::entity tilemap = WanderSpire::TilemapSystem::GetInstance().CreateTilemap(registry, tilemapName);
		return { entt::to_integral(tilemap) };
	}

	ENGINE_API EntityId Tilemap_CreateLayer(
		EngineContextHandle ctx,
		EntityId tilemap,
		const char* layerName)
	{
		auto* w = GetWrapper(ctx);
		if (!w || tilemap.id == WS_INVALID_ENTITY) return { WS_INVALID_ENTITY };

		auto& registry = w->reg();
		entt::entity tilemapEntity = static_cast<entt::entity>(tilemap.id);
		std::string name = layerName ? layerName : "Layer";

		if (registry.valid(tilemapEntity)) {
			entt::entity layer = WanderSpire::TilemapSystem::GetInstance().CreateTilemapLayer(registry, tilemapEntity, name);
			return { entt::to_integral(layer) };
		}

		return { WS_INVALID_ENTITY };
	}

	ENGINE_API void Tilemap_SetTile(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY,
		int tileId)
	{
		auto* w = GetWrapper(ctx);
		if (!w || tilemapLayer.id == WS_INVALID_ENTITY) return;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (registry.valid(layer)) {
			WanderSpire::TilemapSystem::GetInstance().SetTile(registry, layer, { tileX, tileY }, tileId);
		}
	}

	ENGINE_API int Tilemap_GetTile(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY)
	{
		auto* w = GetWrapper(ctx);
		if (!w || tilemapLayer.id == WS_INVALID_ENTITY) return -1;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (registry.valid(layer)) {
			return WanderSpire::TilemapSystem::GetInstance().GetTile(registry, layer, { tileX, tileY });
		}

		return -1;
	}

	ENGINE_API int Tilemap_FloodFill(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int startX, int startY,
		int newTileId)
	{
		auto* w = GetWrapper(ctx);
		if (!w || tilemapLayer.id == WS_INVALID_ENTITY) return 0;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (registry.valid(layer)) {
			WanderSpire::TilemapSystem::GetInstance().FloodFill(registry, layer, { startX, startY }, newTileId);
			return 1; // TODO: Return actual count of affected tiles
		}

		return 0;
	}

	//=============================================================================
	// TILE PALETTE API IMPLEMENTATION
	//=============================================================================

	ENGINE_API int TilePalette_Create(
		EngineContextHandle ctx,
		const char* paletteName,
		const char* atlasPath,
		int tileWidth, int tileHeight)
	{
		if (!paletteName) return -1;

		WanderSpire::TilePalette palette;
		palette.name = paletteName;
		palette.atlasPath = atlasPath ? atlasPath : "";
		palette.tileSize = { tileWidth, tileHeight };

		int paletteId = WanderSpire::g_nextPaletteId++;
		WanderSpire::g_tilePalettes[paletteId] = std::move(palette);

		spdlog::info("[TilePalette] Created palette '{}' with ID {}", paletteName, paletteId);
		return paletteId;
	}

	ENGINE_API void TilePalette_SetActive(EngineContextHandle ctx, int paletteId) {
		auto it = WanderSpire::g_tilePalettes.find(paletteId);
		if (it != WanderSpire::g_tilePalettes.end()) {
			WanderSpire::g_activePaletteId = paletteId;
			spdlog::debug("[TilePalette] Set active palette to {}", paletteId);
		}
	}

	ENGINE_API int TilePalette_GetActive(EngineContextHandle ctx) {
		return WanderSpire::g_activePaletteId;
	}

	ENGINE_API int TilePalette_AddTile(
		EngineContextHandle ctx,
		int paletteId,
		int tileId,
		const char* tileName,
		const char* assetPath,
		int atlasX, int atlasY,
		int walkable,
		int collisionType)
	{
		auto it = WanderSpire::g_tilePalettes.find(paletteId);
		if (it == WanderSpire::g_tilePalettes.end()) return -1;

		WanderSpire::TilePalette::TileEntry tile;
		tile.tileId = tileId;
		tile.name = tileName ? tileName : "Tile";
		tile.assetPath = assetPath ? assetPath : "";
		tile.atlasPosition = { atlasX, atlasY };
		tile.walkable = walkable != 0;
		tile.collisionType = collisionType;

		it->second.tiles.push_back(tile);

		spdlog::debug("[TilePalette] Added tile '{}' to palette {}", tile.name, paletteId);
		return static_cast<int>(it->second.tiles.size()) - 1; // Return tile index
	}

	ENGINE_API int TilePalette_GetInfo(
		EngineContextHandle ctx,
		int paletteId,
		char* outName, int nameBufferSize,
		char* outAtlasPath, int atlasPathBufferSize,
		int* outTileWidth,
		int* outTileHeight,
		int* outColumns)
	{
		auto it = WanderSpire::g_tilePalettes.find(paletteId);
		if (it == WanderSpire::g_tilePalettes.end()) return 0;

		const auto& palette = it->second;

		if (outName && nameBufferSize > 0) {
			SafeStringCopy(palette.name, outName, nameBufferSize);
		}
		if (outAtlasPath && atlasPathBufferSize > 0) {
			SafeStringCopy(palette.atlasPath, outAtlasPath, atlasPathBufferSize);
		}
		if (outTileWidth) *outTileWidth = palette.tileSize.x;
		if (outTileHeight) *outTileHeight = palette.tileSize.y;
		if (outColumns) *outColumns = palette.columns;

		return 1;
	}

	ENGINE_API int TilePalette_GetTileCount(EngineContextHandle ctx, int paletteId) {
		auto it = WanderSpire::g_tilePalettes.find(paletteId);
		if (it != WanderSpire::g_tilePalettes.end()) {
			return static_cast<int>(it->second.tiles.size());
		}
		return 0;
	}

	ENGINE_API int TilePalette_GetTileInfo(
		EngineContextHandle ctx,
		int paletteId,
		int tileIndex,
		int* outTileId,
		char* outTileName, int nameBufferSize,
		int* outAtlasX, int* outAtlasY,
		int* outWalkable,
		int* outCollisionType)
	{
		auto it = WanderSpire::g_tilePalettes.find(paletteId);
		if (it == WanderSpire::g_tilePalettes.end()) return 0;

		const auto& palette = it->second;
		if (tileIndex < 0 || tileIndex >= static_cast<int>(palette.tiles.size())) return 0;

		const auto& tile = palette.tiles[tileIndex];

		if (outTileId) *outTileId = tile.tileId;
		if (outTileName && nameBufferSize > 0) {
			SafeStringCopy(tile.name, outTileName, nameBufferSize);
		}
		if (outAtlasX) *outAtlasX = tile.atlasPosition.x;
		if (outAtlasY) *outAtlasY = tile.atlasPosition.y;
		if (outWalkable) *outWalkable = tile.walkable ? 1 : 0;
		if (outCollisionType) *outCollisionType = tile.collisionType;

		return 1;
	}

	ENGINE_API int TilePalette_Load(EngineContextHandle ctx, const char* palettePath) {
		if (!palettePath) return 0;

		try {
			WanderSpire::TilePaintingManager::GetInstance().LoadPalette(palettePath);
			return 1;
		}
		catch (...) {
			return 0;
		}
	}

	ENGINE_API int TilePalette_Save(EngineContextHandle ctx, int paletteId, const char* palettePath) {
		if (!palettePath) return 0;

		auto it = WanderSpire::g_tilePalettes.find(paletteId);
		if (it == WanderSpire::g_tilePalettes.end()) return 0;

		try {
			WanderSpire::TilePaintingManager::GetInstance().SavePalette(palettePath, it->second);
			return 1;
		}
		catch (...) {
			return 0;
		}
	}

	ENGINE_API int TilePalette_GetSelectedTile(EngineContextHandle ctx) {
		if (WanderSpire::g_activePaletteId > 0) {
			auto it = WanderSpire::g_tilePalettes.find(WanderSpire::g_activePaletteId);
			if (it != WanderSpire::g_tilePalettes.end() && !it->second.tiles.empty()) {
				return it->second.tiles[0].tileId;
			}
		}
		return -1;
	}

	ENGINE_API void TilePalette_SetSelectedTile(EngineContextHandle ctx, int tileId) {
		WanderSpire::TilePaintingManager::GetInstance().SetSelectedTile(tileId);
	}

	//=============================================================================
	// TILE BRUSH API IMPLEMENTATION
	//=============================================================================

	ENGINE_API void TileBrush_SetType(EngineContextHandle ctx, int brushType) {
		auto& brush = WanderSpire::TilePaintingManager::GetInstance().GetActiveBrush();
		WanderSpire::TileBrush newBrush = brush;
		newBrush.type = static_cast<WanderSpire::TileBrush::BrushType>(brushType);
		WanderSpire::TilePaintingManager::GetInstance().SetActiveBrush(newBrush);
	}

	ENGINE_API void TileBrush_SetSize(EngineContextHandle ctx, int size) {
		auto& brush = WanderSpire::TilePaintingManager::GetInstance().GetActiveBrush();
		WanderSpire::TileBrush newBrush = brush;
		newBrush.size = std::max(1, size);
		WanderSpire::TilePaintingManager::GetInstance().SetActiveBrush(newBrush);
	}

	ENGINE_API void TileBrush_SetBlendMode(EngineContextHandle ctx, int blendMode) {
		auto& brush = WanderSpire::TilePaintingManager::GetInstance().GetActiveBrush();
		WanderSpire::TileBrush newBrush = brush;
		newBrush.blendMode = static_cast<WanderSpire::TileBrush::BlendMode>(blendMode);
		WanderSpire::TilePaintingManager::GetInstance().SetActiveBrush(newBrush);
	}

	ENGINE_API void TileBrush_SetRandomization(
		EngineContextHandle ctx,
		int enabled,
		float strength)
	{
		auto& paintManager = WanderSpire::TilePaintingManager::GetInstance();
		auto brush = paintManager.GetActiveBrush();
		brush.randomize = enabled != 0;
		brush.randomStrength = std::clamp(strength, 0.0f, 1.0f);
		paintManager.SetActiveBrush(brush);
	}

	ENGINE_API void TileBrush_SetOpacity(EngineContextHandle ctx, float opacity) {
		auto& paintManager = WanderSpire::TilePaintingManager::GetInstance();
		auto brush = paintManager.GetActiveBrush();
		brush.opacity = std::clamp(opacity, 0.0f, 1.0f);
		paintManager.SetActiveBrush(brush);
	}

	ENGINE_API int TileBrush_GetSettings(
		EngineContextHandle ctx,
		int* outType,
		int* outSize,
		int* outBlendMode,
		float* outOpacity,
		int* outRandomEnabled,
		float* outRandomStrength)
	{
		auto& paintManager = WanderSpire::TilePaintingManager::GetInstance();
		const auto& brush = paintManager.GetActiveBrush();

		if (outType) *outType = static_cast<int>(brush.type);
		if (outSize) *outSize = brush.size;
		if (outBlendMode) *outBlendMode = static_cast<int>(brush.blendMode);
		if (outOpacity) *outOpacity = brush.opacity;
		if (outRandomEnabled) *outRandomEnabled = brush.randomize ? 1 : 0;
		if (outRandomStrength) *outRandomStrength = brush.randomStrength;

		return 1;
	}

	ENGINE_API int TileBrush_LoadPattern(EngineContextHandle ctx, const char* patternPath) {
		if (!patternPath) return 0;

		try {
			WanderSpire::TilePaintingManager::GetInstance().LoadPattern(patternPath);
			return 1;
		}
		catch (...) {
			return 0;
		}
	}

	ENGINE_API int TileBrush_SavePattern(EngineContextHandle ctx, const char* patternPath) {
		if (!patternPath) return 0;

		try {
			auto& paintManager = WanderSpire::TilePaintingManager::GetInstance();
			const auto& brush = paintManager.GetActiveBrush();
			paintManager.SavePattern(patternPath, brush.pattern);
			return 1;
		}
		catch (...) {
			return 0;
		}
	}

	//=============================================================================
	// TILE PAINTING API IMPLEMENTATION
	//=============================================================================

	ENGINE_API void TilePaint_Begin(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY)
	{
		auto* w = GetWrapper(ctx);
		if (!w || tilemapLayer.id == WS_INVALID_ENTITY) return;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (registry.valid(layer)) {
			WanderSpire::TilePaintingManager::GetInstance().BeginPaint(registry, layer, { tileX, tileY });
		}
	}

	ENGINE_API void TilePaint_Continue(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY)
	{
		auto* w = GetWrapper(ctx);
		if (!w || tilemapLayer.id == WS_INVALID_ENTITY) return;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (registry.valid(layer)) {
			WanderSpire::TilePaintingManager::GetInstance().ContinuePaint(registry, layer, { tileX, tileY });
		}
	}

	ENGINE_API void TilePaint_End(EngineContextHandle ctx, EntityId tilemapLayer) {
		auto* w = GetWrapper(ctx);
		if (!w || tilemapLayer.id == WS_INVALID_ENTITY) return;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (registry.valid(layer)) {
			WanderSpire::TilePaintingManager::GetInstance().EndPaint(registry, layer);
		}
	}

	ENGINE_API void TilePaint_PaintWithBrush(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (ValidateLayer(registry, layer)) {
			WanderSpire::TilePaintingManager::GetInstance().BeginPaint(registry, layer, { tileX, tileY });
			WanderSpire::TilePaintingManager::GetInstance().EndPaint(registry, layer);
		}
	}

	ENGINE_API int TilePaint_GetPreview(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY,
		int* outTilePositions,
		int maxPositions)
	{
		auto* w = GetWrapper(ctx);
		if (!w || !outTilePositions || maxPositions <= 0) return 0;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (registry.valid(layer)) {
			auto preview = WanderSpire::TilePaintingManager::GetInstance().GetPaintPreview(registry, layer, { tileX, tileY });
			int count = std::min(static_cast<int>(preview.size()), maxPositions / 2);

			for (int i = 0; i < count; ++i) {
				outTilePositions[i * 2] = preview[i].x;
				outTilePositions[i * 2 + 1] = preview[i].y;
			}

			return count;
		}

		return 0;
	}

	ENGINE_API int TilePaint_GetBrushPreview(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY,
		int* outTilePositions,
		int maxPositions)
	{
		auto* w = GetWrapper(ctx);
		if (!w || !outTilePositions || maxPositions <= 0) return 0;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (!ValidateLayer(registry, layer)) return 0;

		auto preview = WanderSpire::TilePaintingManager::GetInstance().GetPaintPreview(registry, layer, { tileX, tileY });
		return ConvertPositionsToArray(preview, outTilePositions, maxPositions);
	}

	ENGINE_API void TilePaint_PaintLine(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int startX, int startY,
		int endX, int endY)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (ValidateLayer(registry, layer)) {
			WanderSpire::TilePaintingManager::GetInstance().PaintLine(registry, layer, { startX, startY }, { endX, endY });
		}
	}

	ENGINE_API void TilePaint_PaintRectangle(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int minX, int minY,
		int maxX, int maxY,
		int filled)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (ValidateLayer(registry, layer)) {
			WanderSpire::TilePaintingManager::GetInstance().PaintRectangle(registry, layer,
				{ minX, minY }, { maxX, maxY }, filled != 0);
		}
	}

	ENGINE_API void TilePaint_PaintCircle(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int centerX, int centerY,
		int radius,
		int filled)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (ValidateLayer(registry, layer)) {
			WanderSpire::TilePaintingManager::GetInstance().PaintCircle(registry, layer,
				{ centerX, centerY }, radius, filled != 0);
		}
	}

	ENGINE_API int TilePaint_SampleTile(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileX, int tileY)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return -1;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (ValidateLayer(registry, layer)) {
			return WanderSpire::TilePaintingManager::GetInstance().SampleTile(registry, layer, { tileX, tileY });
		}

		return -1;
	}

	//=============================================================================
	// TILE LAYER OPERATIONS API IMPLEMENTATION
	//=============================================================================

	ENGINE_API int TilemapLayer_GetAllInTilemap(
		EngineContextHandle ctx,
		EntityId tilemap,
		uint32_t* outLayers,
		int maxCount)
	{
		auto* w = GetWrapper(ctx);
		if (!w || !outLayers || maxCount <= 0) return 0;

		auto& registry = w->reg();
		entt::entity tilemapEntity = static_cast<entt::entity>(tilemap.id);

		if (!ValidateTilemap(registry, tilemapEntity)) return 0;

		auto layers = WanderSpire::TileLayerManager::GetInstance().GetLayersInTilemap(registry, tilemapEntity);
		int count = std::min(static_cast<int>(layers.size()), maxCount);

		for (int i = 0; i < count; ++i) {
			outLayers[i] = entt::to_integral(layers[i]);
		}

		return count;
	}

	ENGINE_API int TilemapLayer_GetInfo(
		EngineContextHandle ctx,
		EntityId layer,
		char* outName, int nameBufferSize,
		int* outVisible,
		int* outLocked,
		float* outOpacity,
		int* outSortOrder)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return 0;

		auto& registry = w->reg();
		entt::entity layerEntity = static_cast<entt::entity>(layer.id);

		if (!ValidateLayer(registry, layerEntity)) return 0;

		auto layerInfo = WanderSpire::TileLayerManager::GetInstance().GetLayerInfo(registry, layerEntity);

		if (outName && nameBufferSize > 0) {
			SafeStringCopy(layerInfo.name, outName, nameBufferSize);
		}
		if (outVisible) *outVisible = layerInfo.visible ? 1 : 0;
		if (outLocked) *outLocked = layerInfo.locked ? 1 : 0;
		if (outOpacity) *outOpacity = layerInfo.opacity;
		if (outSortOrder) *outSortOrder = layerInfo.sortingOrder;

		return 1;
	}

	ENGINE_API void TilemapLayer_SetVisible(EngineContextHandle ctx, EntityId layer, int visible) {
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity layerEntity = static_cast<entt::entity>(layer.id);

		if (ValidateLayer(registry, layerEntity)) {
			WanderSpire::TileLayerManager::GetInstance().SetLayerVisible(registry, layerEntity, visible != 0);
		}
	}

	ENGINE_API void TilemapLayer_SetLocked(EngineContextHandle ctx, EntityId layer, int locked) {
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity layerEntity = static_cast<entt::entity>(layer.id);

		if (ValidateLayer(registry, layerEntity)) {
			WanderSpire::TileLayerManager::GetInstance().SetLayerLocked(registry, layerEntity, locked != 0);
		}
	}

	ENGINE_API void TilemapLayer_SetOpacity(EngineContextHandle ctx, EntityId layer, float opacity) {
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity layerEntity = static_cast<entt::entity>(layer.id);

		if (ValidateLayer(registry, layerEntity)) {
			WanderSpire::TileLayerManager::GetInstance().SetLayerOpacity(registry, layerEntity, opacity);
		}
	}

	ENGINE_API void TilemapLayer_SetSortOrder(EngineContextHandle ctx, EntityId layer, int sortOrder) {
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity layerEntity = static_cast<entt::entity>(layer.id);

		if (ValidateLayer(registry, layerEntity)) {
			WanderSpire::TileLayerManager::GetInstance().SetLayerSortOrder(registry, layerEntity, sortOrder);
		}
	}

	ENGINE_API void TilemapLayer_Reorder(EngineContextHandle ctx, EntityId layer, int newSortOrder) {
		TilemapLayer_SetSortOrder(ctx, layer, newSortOrder);
	}

	ENGINE_API int TilemapLayer_GetPaintable(
		EngineContextHandle ctx,
		uint32_t* outLayers,
		int maxCount)
	{
		auto* w = GetWrapper(ctx);
		if (!w || !outLayers || maxCount <= 0) return 0;

		auto& registry = w->reg();
		auto layers = WanderSpire::TileLayerManager::GetInstance().GetPaintableLayers(registry);
		int count = std::min(static_cast<int>(layers.size()), maxCount);

		for (int i = 0; i < count; ++i) {
			outLayers[i] = entt::to_integral(layers[i]);
		}

		return count;
	}

	ENGINE_API void TileLayer_CopyRegion(
		EngineContextHandle ctx,
		EntityId srcLayer,
		EntityId dstLayer,
		int srcMinX, int srcMinY,
		int srcMaxX, int srcMaxY,
		int dstX, int dstY)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity srcEntity = static_cast<entt::entity>(srcLayer.id);
		entt::entity dstEntity = static_cast<entt::entity>(dstLayer.id);

		if (ValidateLayer(registry, srcEntity) && ValidateLayer(registry, dstEntity)) {
			WanderSpire::TileLayerManager::GetInstance().CopyLayerRegion(registry, srcEntity, dstEntity,
				{ srcMinX, srcMinY }, { srcMaxX, srcMaxY }, { dstX, dstY });
		}
	}

	ENGINE_API void TileLayer_CopyToClipboard(
		EngineContextHandle ctx,
		EntityId layer,
		int minX, int minY,
		int maxX, int maxY)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity layerEntity = static_cast<entt::entity>(layer.id);

		if (ValidateLayer(registry, layerEntity)) {
			WanderSpire::TileLayerManager::GetInstance().CopyLayerToClipboard(registry, layerEntity,
				{ minX, minY }, { maxX, maxY });
		}
	}

	ENGINE_API void TileLayer_PasteFromClipboard(
		EngineContextHandle ctx,
		EntityId layer,
		int x, int y)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity layerEntity = static_cast<entt::entity>(layer.id);

		if (ValidateLayer(registry, layerEntity)) {
			WanderSpire::TileLayerManager::GetInstance().PasteFromClipboard(registry, layerEntity, { x, y });
		}
	}

	ENGINE_API void TileLayer_BlendLayers(
		EngineContextHandle ctx,
		EntityId baseLayer,
		EntityId overlayLayer,
		int minX, int minY,
		int maxX, int maxY,
		float opacity)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity baseEntity = static_cast<entt::entity>(baseLayer.id);
		entt::entity overlayEntity = static_cast<entt::entity>(overlayLayer.id);

		if (ValidateLayer(registry, baseEntity) && ValidateLayer(registry, overlayEntity)) {
			WanderSpire::TileLayerManager::GetInstance().BlendLayers(registry, baseEntity, overlayEntity,
				{ minX, minY }, { maxX, maxY }, opacity);
		}
	}

	ENGINE_API void TileLayer_SetPalette(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int paletteId)
	{
		auto* w = static_cast<Wrapper*>(ctx);

		if (!w || tilemapLayer.id == WS_INVALID_ENTITY) return;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (registry.valid(layer)) {
			auto* layerComponent = registry.try_get<WanderSpire::TilemapLayerComponent>(layer);
			if (layerComponent) {
				layerComponent->paletteId = paletteId;

				if (layerComponent->autoRefreshDefinitions) {
					auto& manager = WanderSpire::TileDefinitionManager::GetInstance();
					manager.LoadFromPalette(paletteId);
				}

				spdlog::debug("[TileLayer_SetPalette] Set layer {} to use palette {}",
					entt::to_integral(layer), paletteId);
			}
			else {
				spdlog::error("[TileLayer_SetPalette] Entity {} is not a tilemap layer",
					entt::to_integral(layer));
			}
		}
	}

	ENGINE_API int TileLayer_GetPalette(
		EngineContextHandle ctx,
		EntityId tilemapLayer)
	{
		auto* w = static_cast<Wrapper*>(ctx);
		if (!w || tilemapLayer.id == WS_INVALID_ENTITY) return 0;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (registry.valid(layer)) {
			auto* layerComponent = registry.try_get<WanderSpire::TilemapLayerComponent>(layer);
			if (layerComponent) {
				return layerComponent->paletteId;
			}
		}

		return 0;
	}

	ENGINE_API void TileLayer_RefreshDefinitions(
		EngineContextHandle ctx,
		EntityId tilemapLayer)
	{
		auto* w = static_cast<Wrapper*>(ctx);
		if (!w || tilemapLayer.id == WS_INVALID_ENTITY) return;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (registry.valid(layer)) {
			auto* layerComponent = registry.try_get<WanderSpire::TilemapLayerComponent>(layer);
			if (layerComponent && layerComponent->paletteId > 0) {
				auto& manager = WanderSpire::TileDefinitionManager::GetInstance();
				manager.LoadFromPalette(layerComponent->paletteId);

				spdlog::info("[TileLayer_RefreshDefinitions] Refreshed tile definitions for layer {} from palette {}",
					entt::to_integral(layer), layerComponent->paletteId);
			}
		}
	}

	//=============================================================================
	// COMMAND SYSTEM API IMPLEMENTATION
	//=============================================================================

	ENGINE_API void Command_Execute(EngineContextHandle ctx, const char* commandJson) {
		spdlog::warn("[Commands] JSON command parsing not yet implemented");
	}

	ENGINE_API int Command_Undo(EngineContextHandle ctx) {
		if (!WanderSpire::g_commandHistory) {
			WanderSpire::g_commandHistory = std::make_unique<WanderSpire::CommandHistory>();
		}

		if (WanderSpire::g_commandHistory->CanUndo()) {
			WanderSpire::g_commandHistory->Undo();
			return 1;
		}
		return 0;
	}

	ENGINE_API int Command_Redo(EngineContextHandle ctx) {
		if (!WanderSpire::g_commandHistory) {
			WanderSpire::g_commandHistory = std::make_unique<WanderSpire::CommandHistory>();
		}

		if (WanderSpire::g_commandHistory->CanRedo()) {
			WanderSpire::g_commandHistory->Redo();
			return 1;
		}
		return 0;
	}

	ENGINE_API int Command_CanUndo(EngineContextHandle ctx) {
		return WanderSpire::g_commandHistory ? WanderSpire::g_commandHistory->CanUndo() : 0;
	}

	ENGINE_API int Command_CanRedo(EngineContextHandle ctx) {
		return WanderSpire::g_commandHistory ? WanderSpire::g_commandHistory->CanRedo() : 0;
	}

	ENGINE_API int Command_GetUndoDescription(
		EngineContextHandle ctx,
		char* outDescription,
		int bufferSize)
	{
		if (!WanderSpire::g_commandHistory || !outDescription || bufferSize <= 0) return 0;

		std::string desc = WanderSpire::g_commandHistory->GetUndoDescription();
		if (desc.empty()) return 0;

		SafeStringCopy(desc, outDescription, bufferSize);
		return 1;
	}

	ENGINE_API int Command_GetRedoDescription(
		EngineContextHandle ctx,
		char* outDescription,
		int bufferSize)
	{
		if (!WanderSpire::g_commandHistory || !outDescription || bufferSize <= 0) return 0;

		std::string desc = WanderSpire::g_commandHistory->GetRedoDescription();
		if (desc.empty()) return 0;

		SafeStringCopy(desc, outDescription, bufferSize);
		return 1;
	}

	ENGINE_API int Command_GetHistorySize(EngineContextHandle ctx) {
		return WanderSpire::g_commandHistory ? WanderSpire::g_commandHistory->GetHistorySize() : 0;
	}

	ENGINE_API void Command_SetMaxHistorySize(EngineContextHandle ctx, int maxSize) {
		if (!WanderSpire::g_commandHistory) {
			WanderSpire::g_commandHistory = std::make_unique<WanderSpire::CommandHistory>();
		}
		WanderSpire::g_commandHistory->SetMaxHistorySize(maxSize);
	}

	ENGINE_API void Command_ClearHistory(EngineContextHandle ctx) {
		if (WanderSpire::g_commandHistory) {
			WanderSpire::g_commandHistory->Clear();
		}
	}

	ENGINE_API void Command_MoveSelection(
		EngineContextHandle ctx,
		float deltaX, float deltaY)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		const auto& selected = WanderSpire::SelectionManager::GetInstance().GetSelectedEntities();

		if (!selected.empty()) {
			if (!WanderSpire::g_commandHistory) {
				WanderSpire::g_commandHistory = std::make_unique<WanderSpire::CommandHistory>();
			}

			std::vector<entt::entity> entities(selected.begin(), selected.end());
			auto command = std::make_unique<WanderSpire::MoveCommand>(registry, entities, glm::vec2{ deltaX, deltaY });
			WanderSpire::g_commandHistory->ExecuteCommand(std::move(command));
		}
	}

	ENGINE_API void Command_DeleteSelection(EngineContextHandle ctx) {
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		const auto& selected = WanderSpire::SelectionManager::GetInstance().GetSelectedEntities();

		if (!selected.empty()) {
			if (!WanderSpire::g_commandHistory) {
				WanderSpire::g_commandHistory = std::make_unique<WanderSpire::CommandHistory>();
			}

			std::vector<entt::entity> entities(selected.begin(), selected.end());
			auto command = std::make_unique<WanderSpire::DeleteGameObjectCommand>(registry, entities);
			WanderSpire::g_commandHistory->ExecuteCommand(std::move(command));

			// Clear selection since entities are deleted
			WanderSpire::SelectionManager::GetInstance().DeselectAll(registry);
		}
	}

	//=============================================================================
	// GRID OPERATIONS API IMPLEMENTATION
	//=============================================================================

	ENGINE_API void Grid_SnapPosition(
		EngineContextHandle ctx,
		float inX, float inY,
		float* outX, float* outY)
	{
		auto* w = GetWrapper(ctx);
		if (!w || !outX || !outY) return;

		float gridSize = w->tileSize();
		*outX = std::round(inX / gridSize) * gridSize;
		*outY = std::round(inY / gridSize) * gridSize;
	}

	ENGINE_API float Grid_GetTileSize(EngineContextHandle ctx) {
		auto* w = GetWrapper(ctx);
		return w ? w->tileSize() : 32.0f;
	}

	//=============================================================================
	// AUTO-TILING API IMPLEMENTATION
	//=============================================================================

	// Global auto-tile rule sets storage
	static std::unordered_map<int, WanderSpire::AutoTileSet> g_autoTileRuleSets;
	static int g_nextAutoTileSetId = 1;

	ENGINE_API int AutoTile_CreateRuleSet(EngineContextHandle ctx, const char* name) {
		if (!name) return -1;

		WanderSpire::AutoTileSet ruleSet;
		ruleSet.name = name;
		ruleSet.enabled = true;

		int setId = g_nextAutoTileSetId++;
		g_autoTileRuleSets[setId] = std::move(ruleSet);

		return setId;
	}

	ENGINE_API void AutoTile_AddRule(
		EngineContextHandle ctx,
		int ruleSetId,
		const int* neighbors,
		int resultTileId,
		int priority)
	{
		if (!neighbors) return;

		auto it = g_autoTileRuleSets.find(ruleSetId);
		if (it == g_autoTileRuleSets.end()) return;

		WanderSpire::AutoTileRule rule;
		for (int i = 0; i < 9; ++i) {
			rule.neighbors[i] = static_cast<WanderSpire::AutoTileRule::NeighborState>(neighbors[i]);
		}
		rule.resultTileId = resultTileId;
		rule.priority = priority;

		it->second.rules.push_back(rule);

		// Register with painting manager
		WanderSpire::TilePaintingManager::GetInstance().RegisterAutoTileSet(it->second);
	}

	ENGINE_API void AutoTile_SetEnabled(EngineContextHandle ctx, int ruleSetId, int enabled) {
		auto it = g_autoTileRuleSets.find(ruleSetId);
		if (it != g_autoTileRuleSets.end()) {
			it->second.enabled = enabled != 0;
			WanderSpire::TilePaintingManager::GetInstance().RegisterAutoTileSet(it->second);
		}
	}

	ENGINE_API void AutoTile_ApplyToRegion(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int minX, int minY,
		int maxX, int maxY)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (!ValidateLayer(registry, layer)) return;

		// Build position list for the region
		std::vector<glm::ivec2> positions;
		for (int y = minY; y <= maxY; ++y) {
			for (int x = minX; x <= maxX; ++x) {
				positions.push_back({ x, y });
			}
		}

		WanderSpire::TilePaintingManager::GetInstance().ApplyAutoTiling(registry, layer, positions);
	}

	//=============================================================================
	// TILE DEFINITION API IMPLEMENTATION
	//=============================================================================

	ENGINE_API void TileDef_Register(
		EngineContextHandle ctx,
		int tileId,
		const char* atlasName,
		const char* frameName,
		int walkable,
		int collisionType)
	{
		if (!atlasName || !frameName) return;

		auto& tileDefManager = WanderSpire::TileDefinitionManager::GetInstance();
		tileDefManager.RegisterTile(tileId, atlasName, frameName,
			walkable != 0, collisionType);
	}

	ENGINE_API void TileDef_SetDefault(
		EngineContextHandle ctx,
		const char* atlasName,
		const char* frameName)
	{
		if (!atlasName || !frameName) return;

		auto& tileDefManager = WanderSpire::TileDefinitionManager::GetInstance();
		tileDefManager.SetDefaultDefinition(atlasName, frameName);
	}

	ENGINE_API int TileDef_GetCount(EngineContextHandle ctx) {
		auto& tileDefManager = WanderSpire::TileDefinitionManager::GetInstance();
		return static_cast<int>(tileDefManager.GetTileCount());
	}

	ENGINE_API void TileDef_Clear(EngineContextHandle ctx) {
		auto& tileDefManager = WanderSpire::TileDefinitionManager::GetInstance();
		tileDefManager.Clear();
	}

	ENGINE_API void TileDef_RegisterTile(
		EngineContextHandle ctx,
		int tileId,
		const char* atlasName,
		const char* frameName,
		int walkable,
		int collisionType)
	{
		if (!atlasName || !frameName) {
			spdlog::error("[TileDef_RegisterTile] Invalid parameters: atlasName or frameName is null");
			return;
		}

		auto& manager = WanderSpire::TileDefinitionManager::GetInstance();
		manager.RegisterTile(tileId, atlasName, frameName, walkable != 0, collisionType);
	}

	ENGINE_API int TileDef_GetTileInfo(
		EngineContextHandle ctx,
		int tileId,
		char* outAtlasName, int atlasNameSize,
		char* outFrameName, int frameNameSize,
		int* outWalkable,
		int* outCollisionType)
	{
		if (!outAtlasName || !outFrameName || atlasNameSize <= 0 || frameNameSize <= 0) {
			return -1;
		}

		auto& manager = WanderSpire::TileDefinitionManager::GetInstance();
		const auto* def = manager.GetTileDefinition(tileId);

		if (!def) {
			return -2;
		}

		// Copy atlas name
		if (def->atlasName.length() >= static_cast<size_t>(atlasNameSize)) {
			return -3; // Buffer too small
		}
		std::strcpy(outAtlasName, def->atlasName.c_str());

		// Copy frame name
		if (def->frameName.length() >= static_cast<size_t>(frameNameSize)) {
			return -4; // Buffer too small
		}
		std::strcpy(outFrameName, def->frameName.c_str());

		// Set optional outputs
		if (outWalkable) *outWalkable = def->walkable ? 1 : 0;
		if (outCollisionType) *outCollisionType = def->collisionType;

		return 0; // Success
	}

	ENGINE_API void TileDef_LoadFromPalette(
		EngineContextHandle ctx,
		int paletteId)
	{
		auto& manager = WanderSpire::TileDefinitionManager::GetInstance();
		manager.LoadFromPalette(paletteId);
	}

	//=============================================================================
	// TILEMAP ANALYSIS API IMPLEMENTATION
	//=============================================================================

	ENGINE_API int Tilemap_GetBounds(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int* outMinX, int* outMinY,
		int* outMaxX, int* outMaxY)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return 0;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (!ValidateLayer(registry, layer)) return 0;

		// This would need to be implemented by scanning through chunks
		// For now, return a default large area
		if (outMinX) *outMinX = -100;
		if (outMinY) *outMinY = -100;
		if (outMaxX) *outMaxX = 100;
		if (outMaxY) *outMaxY = 100;

		return 1;
	}

	ENGINE_API int Tilemap_CountTilesInRegion(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int minX, int minY,
		int maxX, int maxY)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return 0;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (!ValidateLayer(registry, layer)) return 0;

		auto& tilemapSystem = WanderSpire::TilemapSystem::GetInstance();
		int count = 0;

		for (int y = minY; y <= maxY; ++y) {
			for (int x = minX; x <= maxX; ++x) {
				if (tilemapSystem.GetTile(registry, layer, { x, y }) != -1) {
					count++;
				}
			}
		}

		return count;
	}

	ENGINE_API int Tilemap_FindTilePositions(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int tileId,
		int* outPositions,
		int maxPositions)
	{
		auto* w = GetWrapper(ctx);
		if (!w || !outPositions || maxPositions <= 0) return 0;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (!ValidateLayer(registry, layer)) return 0;

		// This would need to scan through all loaded chunks
		// For now, return empty result
		return 0;
	}

	ENGINE_API int Tilemap_ReplaceTiles(
		EngineContextHandle ctx,
		EntityId tilemapLayer,
		int oldTileId,
		int newTileId,
		int minX, int minY,
		int maxX, int maxY)
	{
		auto* w = GetWrapper(ctx);
		if (!w) return 0;

		auto& registry = w->reg();
		entt::entity layer = static_cast<entt::entity>(tilemapLayer.id);

		if (!ValidateLayer(registry, layer)) return 0;

		auto& tilemapSystem = WanderSpire::TilemapSystem::GetInstance();
		int replacedCount = 0;

		for (int y = minY; y <= maxY; ++y) {
			for (int x = minX; x <= maxX; ++x) {
				if (tilemapSystem.GetTile(registry, layer, { x, y }) == oldTileId) {
					tilemapSystem.SetTile(registry, layer, { x, y }, newTileId);
					replacedCount++;
				}
			}
		}

		return replacedCount;
	}

	//=============================================================================
	// COORDINATE CONVERSION API IMPLEMENTATION
	//=============================================================================

	ENGINE_API void Coord_WorldToTile(
		EngineContextHandle ctx,
		float worldX, float worldY,
		int* outTileX, int* outTileY)
	{
		auto* w = GetWrapper(ctx);
		float tileSize = w ? w->tileSize() : 64.0f;

		if (outTileX) *outTileX = static_cast<int>(std::floor(worldX / tileSize));
		if (outTileY) *outTileY = static_cast<int>(std::floor(worldY / tileSize));
	}

	ENGINE_API void Coord_TileToWorld(
		EngineContextHandle ctx,
		int tileX, int tileY,
		float* outWorldX, float* outWorldY)
	{
		auto* w = GetWrapper(ctx);
		float tileSize = w ? w->tileSize() : 64.0f;

		if (outWorldX) *outWorldX = (static_cast<float>(tileX) + 0.5f) * tileSize;
		if (outWorldY) *outWorldY = (static_cast<float>(tileY) + 0.5f) * tileSize;
	}

	ENGINE_API float Coord_GetTileSize(EngineContextHandle ctx) {
		auto* w = GetWrapper(ctx);
		return w ? w->tileSize() : 64.0f;
	}

	ENGINE_API void Coord_SetTileSize(EngineContextHandle ctx, float tileSize) {
		if (tileSize > 0.0f) {
			WanderSpire::ConfigManager::SetTileSize(tileSize);
		}
	}

	//=============================================================================
// EDITOR-SPECIFIC ENGINE FUNCTIONS
//=============================================================================

	ENGINE_API int Engine_InitializeEditor(EngineContextHandle ctx, int width, int height, int flags) {
		if (!ctx) return -1;

		g_editorState.viewportWidth = width;
		g_editorState.viewportHeight = height;
		g_editorState.editorRenderFlags = flags;

		// Initialize editor-specific rendering
		glViewport(0, 0, width, height);

		spdlog::info("[Editor] Initialized editor mode: {}x{}, flags: {}", width, height, flags);
		return 0;
	}

	ENGINE_API void Engine_SetViewportSize(EngineContextHandle ctx, int width, int height) {
		if (!ctx) return;

		g_editorState.viewportWidth = width;
		g_editorState.viewportHeight = height;
		glViewport(0, 0, width, height);

		// Update camera aspect ratio
		WanderSpire::Application::GetCamera().SetScreenSize(static_cast<float>(width), static_cast<float>(height));
	}

	ENGINE_API void Engine_GetViewportSize(EngineContextHandle ctx, int* outWidth, int* outHeight) {
		if (!ctx || !outWidth || !outHeight) return;

		*outWidth = g_editorState.viewportWidth;
		*outHeight = g_editorState.viewportHeight;
	}

	ENGINE_API void Engine_SetEditorRenderFlags(EngineContextHandle ctx, int flags) {
		if (!ctx) return;
		g_editorState.editorRenderFlags = flags;
	}

	//ENGINE_API void Engine_RenderToFramebuffer(EngineContextHandle ctx, uint32_t framebuffer, int width, int height) {
	//	if (!ctx) return;

	//	glBindFramebuffer(GL_FRAMEBUFFER, framebuffer);
	//	glViewport(0, 0, width, height);

	//	// Clear framebuffer
	//	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
	//}



	//=============================================================================
	// ENTITY PICKING AND SELECTION
	//=============================================================================

	ENGINE_API EntityId Engine_PickEntity(EngineContextHandle ctx, int screenX, int screenY) {
		if (!ctx) return { WS_INVALID_ENTITY };

		auto* w = static_cast<Wrapper*>(ctx);
		auto& registry = w->reg();
		auto& camera = WanderSpire::Application::GetCamera();

		// Convert screen to world coordinates
		float worldX, worldY;
		Engine_ScreenToWorld(ctx, screenX, screenY, &worldX, &worldY);

		// Find entity at world position
		glm::vec2 worldPos(worldX, worldY);
		float tileSize = w->tileSize();

		// Check grid-positioned entities first
		auto gridView = registry.view<WanderSpire::GridPositionComponent, WanderSpire::SpriteRenderComponent>();
		for (auto entity : gridView) {
			const auto& gp = gridView.get<WanderSpire::GridPositionComponent>(entity);
			const auto& sprite = gridView.get<WanderSpire::SpriteRenderComponent>(entity);

			glm::vec2 entityCenter = glm::vec2(gp.tile) * tileSize + glm::vec2(tileSize * 0.5f);
			glm::vec2 halfSize = sprite.worldSize * 0.5f;

			if (worldPos.x >= entityCenter.x - halfSize.x && worldPos.x <= entityCenter.x + halfSize.x &&
				worldPos.y >= entityCenter.y - halfSize.y && worldPos.y <= entityCenter.y + halfSize.y) {
				return { entt::to_integral(entity) };
			}
		}

		// Check transform-positioned entities
		auto transformView = registry.view<WanderSpire::TransformComponent, WanderSpire::SpriteRenderComponent>();
		for (auto entity : transformView) {
			const auto& transform = transformView.get<WanderSpire::TransformComponent>(entity);
			const auto& sprite = transformView.get<WanderSpire::SpriteRenderComponent>(entity);

			glm::vec2 halfSize = sprite.worldSize * 0.5f;

			if (worldPos.x >= transform.localPosition.x - halfSize.x && worldPos.x <= transform.localPosition.x + halfSize.x &&
				worldPos.y >= transform.localPosition.y - halfSize.y && worldPos.y <= transform.localPosition.y + halfSize.y) {
				return { entt::to_integral(entity) };
			}
		}

		return { WS_INVALID_ENTITY };
	}

	ENGINE_API int Engine_PickEntitiesInRect(EngineContextHandle ctx, int x1, int y1, int x2, int y2,
		uint32_t* outEntities, int maxEntities) {
		if (!ctx || !outEntities || maxEntities <= 0) return 0;

		auto* w = static_cast<Wrapper*>(ctx);
		auto& registry = w->reg();

		// Convert screen coordinates to world
		float worldX1, worldY1, worldX2, worldY2;
		Engine_ScreenToWorld(ctx, x1, y1, &worldX1, &worldY1);
		Engine_ScreenToWorld(ctx, x2, y2, &worldX2, &worldY2);

		// Ensure min/max order
		float minX = std::min(worldX1, worldX2);
		float minY = std::min(worldY1, worldY2);
		float maxX = std::max(worldX1, worldX2);
		float maxY = std::max(worldY1, worldY2);

		int count = 0;
		float tileSize = w->tileSize();

		// Check grid entities
		auto gridView = registry.view<WanderSpire::GridPositionComponent, WanderSpire::SpriteRenderComponent>();
		for (auto entity : gridView) {
			if (count >= maxEntities) break;

			const auto& gp = gridView.get<WanderSpire::GridPositionComponent>(entity);
			glm::vec2 entityCenter = glm::vec2(gp.tile) * tileSize + glm::vec2(tileSize * 0.5f);

			if (entityCenter.x >= minX && entityCenter.x <= maxX &&
				entityCenter.y >= minY && entityCenter.y <= maxY) {
				outEntities[count++] = entt::to_integral(entity);
			}
		}

		// Check transform entities
		auto transformView = registry.view<WanderSpire::TransformComponent, WanderSpire::SpriteRenderComponent>();
		for (auto entity : transformView) {
			if (count >= maxEntities) break;

			const auto& transform = transformView.get<WanderSpire::TransformComponent>(entity);

			if (transform.localPosition.x >= minX && transform.localPosition.x <= maxX &&
				transform.localPosition.y >= minY && transform.localPosition.y <= maxY) {
				outEntities[count++] = entt::to_integral(entity);
			}
		}

		return count;
	}

	ENGINE_API int Engine_GetEntityScreenBounds(EngineContextHandle ctx, EntityId entity,
		float* outMinX, float* outMinY, float* outMaxX, float* outMaxY) {
		if (!ctx || !outMinX || !outMinY || !outMaxX || !outMaxY || entity.id == WS_INVALID_ENTITY) return -1;

		auto* w = static_cast<Wrapper*>(ctx);
		auto& registry = w->reg();
		auto ent = static_cast<entt::entity>(entity.id);

		if (!registry.valid(ent)) return -1;

		// Get world bounds first
		float worldMinX, worldMinY, worldMaxX, worldMaxY;
		if (Engine_GetEntityWorldBounds(ctx, entity, &worldMinX, &worldMinY, &worldMaxX, &worldMaxY) != 0) {
			return -1;
		}

		// Convert world corners to screen
		int screenX1, screenY1, screenX2, screenY2;
		Engine_WorldToScreen(ctx, worldMinX, worldMinY, &screenX1, &screenY1);
		Engine_WorldToScreen(ctx, worldMaxX, worldMaxY, &screenX2, &screenY2);

		*outMinX = static_cast<float>(std::min(screenX1, screenX2));
		*outMinY = static_cast<float>(std::min(screenY1, screenY2));
		*outMaxX = static_cast<float>(std::max(screenX1, screenX2));
		*outMaxY = static_cast<float>(std::max(screenY1, screenY2));

		return 0;
	}

	ENGINE_API int Engine_GetEntityWorldBounds(EngineContextHandle ctx, EntityId entity,
		float* outMinX, float* outMinY, float* outMaxX, float* outMaxY) {
		if (!ctx || !outMinX || !outMinY || !outMaxX || !outMaxY || entity.id == WS_INVALID_ENTITY) return -1;

		auto* w = static_cast<Wrapper*>(ctx);
		auto& registry = w->reg();
		auto ent = static_cast<entt::entity>(entity.id);

		if (!registry.valid(ent)) return -1;

		glm::vec2 center(0.0f);
		glm::vec2 size(w->tileSize());

		// Get entity center position
		if (auto* gp = registry.try_get<WanderSpire::GridPositionComponent>(ent)) {
			float tileSize = w->tileSize();
			center = glm::vec2(gp->tile) * tileSize + glm::vec2(tileSize * 0.5f);
		}
		else if (auto* transform = registry.try_get<WanderSpire::TransformComponent>(ent)) {
			center = transform->localPosition;
		}

		// Get entity size
		if (auto* sprite = registry.try_get<WanderSpire::SpriteRenderComponent>(ent)) {
			size = sprite->worldSize;
		}

		glm::vec2 halfSize = size * 0.5f;
		*outMinX = center.x - halfSize.x;
		*outMinY = center.y - halfSize.y;
		*outMaxX = center.x + halfSize.x;
		*outMaxY = center.y + halfSize.y;

		return 0;
	}

	//=============================================================================
	// CAMERA AND VIEWPORT CONTROLS
	//=============================================================================

	ENGINE_API void Engine_SetCameraZoom(EngineContextHandle ctx, float zoom) {
		if (!ctx) return;
		WanderSpire::Application::GetCamera().SetZoom(zoom);
	}

	ENGINE_API float Engine_GetCameraZoom(EngineContextHandle ctx) {
		if (!ctx) return 1.0f;
		return WanderSpire::Application::GetCamera().GetZoom();
	}

	ENGINE_API void Engine_ScreenToWorld(EngineContextHandle ctx, int screenX, int screenY,
		float* outWorldX, float* outWorldY) {
		if (!ctx || !outWorldX || !outWorldY) return;

		auto& camera = WanderSpire::Application::GetCamera();

		// Convert screen coordinates to normalized device coordinates
		float ndcX = (2.0f * screenX) / g_editorState.viewportWidth - 1.0f;
		float ndcY = 1.0f - (2.0f * screenY) / g_editorState.viewportHeight;

		// Convert to world coordinates
		float zoom = camera.GetZoom();
		glm::vec2 cameraPos = camera.GetPosition();

		*outWorldX = cameraPos.x + (ndcX * camera.GetWidth() * 0.5f) / zoom;
		*outWorldY = cameraPos.y + (ndcY * camera.GetHeight() * 0.5f) / zoom;
	}

	ENGINE_API void Engine_WorldToScreen(EngineContextHandle ctx, float worldX, float worldY,
		int* outScreenX, int* outScreenY) {
		if (!ctx || !outScreenX || !outScreenY) return;

		auto& camera = WanderSpire::Application::GetCamera();

		float zoom = camera.GetZoom();
		glm::vec2 cameraPos = camera.GetPosition();

		// Convert world to normalized device coordinates
		float ndcX = ((worldX - cameraPos.x) * zoom) / (camera.GetWidth() * 0.5f);
		float ndcY = ((worldY - cameraPos.y) * zoom) / (camera.GetHeight() * 0.5f);

		// Convert to screen coordinates
		*outScreenX = static_cast<int>((ndcX + 1.0f) * g_editorState.viewportWidth * 0.5f);
		*outScreenY = static_cast<int>((1.0f - ndcY) * g_editorState.viewportHeight * 0.5f);
	}

	ENGINE_API void Engine_GetCameraViewMatrix(EngineContextHandle ctx, float* outMatrix) {
		if (!ctx || !outMatrix) return;

		auto& camera = WanderSpire::Application::GetCamera();
		const auto& vp = camera.GetViewProjectionMatrix();

		// Extract view matrix (this is simplified - in a real implementation you'd want to separate view and projection)
		for (int i = 0; i < 16; ++i) {
			outMatrix[i] = vp[i / 4][i % 4];
		}
	}

	ENGINE_API void Engine_GetCameraProjectionMatrix(EngineContextHandle ctx, float* outMatrix) {
		if (!ctx || !outMatrix) return;

		// For 2D, we'll return the same as view-projection matrix
		Engine_GetCameraViewMatrix(ctx, outMatrix);
	}

	//=============================================================================
	// GRID AND GIZMO RENDERING
	//=============================================================================

	ENGINE_API void Engine_SetGridVisible(EngineContextHandle ctx, int visible) {
		if (!ctx) return;
		g_editorState.gridVisible = visible != 0;
	}

	ENGINE_API void Engine_SetGridProperties(EngineContextHandle ctx, float size, int subdivisions,
		float colorR, float colorG, float colorB, float alpha) {
		if (!ctx) return;

		g_editorState.gridSize = size;
		g_editorState.gridSubdivisions = subdivisions;
		g_editorState.gridColor = { colorR, colorG, colorB, alpha };
	}

	ENGINE_API void Engine_RenderSelectionOutline(EngineContextHandle ctx, EntityId entity,
		float colorR, float colorG, float colorB, float width) {
		if (!ctx || entity.id == WS_INVALID_ENTITY) return;

		// Get entity bounds
		float minX, minY, maxX, maxY;
		if (Engine_GetEntityWorldBounds(ctx, entity, &minX, &minY, &maxX, &maxY) != 0) return;

		// Render outline using debug drawing
		glm::vec3 color(colorR, colorG, colorB);
		float w = maxX - minX;
		float h = maxY - minY;

		Engine_DrawDebugRect(ctx, minX, minY, w, h, colorR, colorG, colorB, 0);
	}

	ENGINE_API void Engine_RenderTransformGizmo(EngineContextHandle ctx, float worldX, float worldY,
		float scale, int gizmoType) {
		if (!ctx) return;

		// Render basic gizmo based on type
		float gizmoSize = 50.0f * scale;

		switch (gizmoType) {
		case GIZMO_TRANSLATION:
			// Draw X axis (red)
			Engine_DrawDebugLine(ctx, worldX, worldY, worldX + gizmoSize, worldY, 1.0f, 0.0f, 0.0f, 2.0f);
			// Draw Y axis (green)
			Engine_DrawDebugLine(ctx, worldX, worldY, worldX, worldY + gizmoSize, 0.0f, 1.0f, 0.0f, 2.0f);
			break;

		case GIZMO_ROTATION:
			Engine_DrawDebugCircle(ctx, worldX, worldY, gizmoSize, 0.0f, 0.0f, 1.0f, 32);
			break;

		case GIZMO_SCALE:
			Engine_DrawDebugRect(ctx, worldX - gizmoSize * 0.5f, worldY - gizmoSize * 0.5f,
				gizmoSize, gizmoSize, 1.0f, 1.0f, 0.0f, 0);
			break;
		}
	}

	//=============================================================================
	// DEBUG AND VISUALIZATION
	//=============================================================================

	ENGINE_API void Engine_SetDebugRenderEnabled(EngineContextHandle ctx, int enabled) {
		if (!ctx) return;
		// This could be used to enable/disable debug rendering globally
	}

	ENGINE_API void Engine_SetDebugRenderFlags(EngineContextHandle ctx, int flags) {
		if (!ctx) return;
		g_editorState.debugRenderFlags = flags;
	}

	ENGINE_API void Engine_DrawDebugLine(EngineContextHandle ctx, float x1, float y1, float x2, float y2,
		float colorR, float colorG, float colorB, float width) {
		if (!ctx) return;

		// Submit debug line to render manager
		auto& renderMgr = WanderSpire::RenderManager::Get();
		renderMgr.SubmitCustom([x1, y1, x2, y2, colorR, colorG, colorB, width]() {
			auto& renderer = WanderSpire::SpriteRenderer::Get();

			// Calculate line properties
			glm::vec2 start(x1, y1);
			glm::vec2 end(x2, y2);
			glm::vec2 center = (start + end) * 0.5f;
			glm::vec2 diff = end - start;
			float length = glm::length(diff);
			float angle = std::atan2(diff.y, diff.x);

			// Draw line as stretched quad
			renderer.DrawSprite(0, center, { length, width }, angle,
				{ colorR, colorG, colorB }, { 0, 0 }, { 1, 1 });
			}, WanderSpire::RenderLayer::Debug);
	}

	ENGINE_API void Engine_DrawDebugCircle(EngineContextHandle ctx, float centerX, float centerY, float radius,
		float colorR, float colorG, float colorB, int segments) {
		if (!ctx || segments < 3) return;

		// Draw circle as line segments
		float angleStep = 2.0f * M_PI / segments;
		for (int i = 0; i < segments; ++i) {
			float angle1 = i * angleStep;
			float angle2 = (i + 1) * angleStep;

			float x1 = centerX + radius * std::cos(angle1);
			float y1 = centerY + radius * std::sin(angle1);
			float x2 = centerX + radius * std::cos(angle2);
			float y2 = centerY + radius * std::sin(angle2);

			Engine_DrawDebugLine(ctx, x1, y1, x2, y2, colorR, colorG, colorB, 1.0f);
		}
	}

	ENGINE_API void Engine_DrawDebugRect(EngineContextHandle ctx, float x, float y, float width, float height,
		float colorR, float colorG, float colorB, int filled) {
		if (!ctx) return;

		if (filled) {
			// Draw filled rectangle
			auto& renderMgr = WanderSpire::RenderManager::Get();
			renderMgr.SubmitCustom([x, y, width, height, colorR, colorG, colorB]() {
				auto& renderer = WanderSpire::SpriteRenderer::Get();
				glm::vec2 center(x + width * 0.5f, y + height * 0.5f);
				renderer.DrawSprite(0, center, { width, height }, 0.0f,
					{ colorR, colorG, colorB }, { 0, 0 }, { 1, 1 });
				}, WanderSpire::RenderLayer::Debug);
		}
		else {
			// Draw outline rectangle
			Engine_DrawDebugLine(ctx, x, y, x + width, y, colorR, colorG, colorB, 1.0f);
			Engine_DrawDebugLine(ctx, x + width, y, x + width, y + height, colorR, colorG, colorB, 1.0f);
			Engine_DrawDebugLine(ctx, x + width, y + height, x, y + height, colorR, colorG, colorB, 1.0f);
			Engine_DrawDebugLine(ctx, x, y + height, x, y, colorR, colorG, colorB, 1.0f);
		}
	}

	//=============================================================================
	// PERFORMANCE AND PROFILING
	//=============================================================================

	ENGINE_API void Engine_GetPerformanceMetrics(EngineContextHandle ctx, PerformanceMetrics* outMetrics) {
		if (!ctx || !outMetrics) return;

		// Use performance tracking from Application
		float frameTime = WanderSpire::Application::GetLastFrameTime();
		int   drawCalls = WanderSpire::Application::GetLastFrameDrawCalls();

		// Return basic performance metrics
		outMetrics->avgFrameTime = frameTime;
		outMetrics->minFrameTime = frameTime; // TODO: Track properly
		outMetrics->maxFrameTime = frameTime; // TODO: Track properly
		outMetrics->avgFPS = frameTime > 0 ? 1000.0f / frameTime : 0.0f; // frameTime is in milliseconds
		outMetrics->totalDrawCalls = drawCalls;
		outMetrics->totalTriangles = drawCalls * 2;
		outMetrics->totalMemoryUsed = 0; // TODO: Implement
		outMetrics->peakMemoryUsed = 0; // TODO: Implement
	}

	ENGINE_API void Engine_BeginProfileSection(EngineContextHandle ctx, const char* name) {
		if (!ctx || !name) return;

		g_editorState.profileStarts[name] = std::chrono::high_resolution_clock::now();
	}

	ENGINE_API void Engine_EndProfileSection(EngineContextHandle ctx, const char* name) {
		if (!ctx || !name) return;

		auto it = g_editorState.profileStarts.find(name);
		if (it == g_editorState.profileStarts.end()) return;

		auto endTime = std::chrono::high_resolution_clock::now();
		auto duration = std::chrono::duration<float, std::milli>(endTime - it->second).count();

		auto& section = g_editorState.profileData[name];
		std::strncpy(section.name, name, sizeof(section.name) - 1);
		section.name[sizeof(section.name) - 1] = '\0';

		section.callCount++;
		section.totalTime += duration;
		section.avgTime = section.totalTime / section.callCount;

		if (section.callCount == 1) {
			section.minTime = section.maxTime = duration;
		}
		else {
			section.minTime = std::min(section.minTime, duration);
			section.maxTime = std::max(section.maxTime, duration);
		}

		g_editorState.profileStarts.erase(it);
	}

	ENGINE_API int Engine_GetProfilingResults(EngineContextHandle ctx, ProfileSection* outSections, int maxSections) {
		if (!ctx || !outSections || maxSections <= 0) return 0;

		int count = 0;
		for (const auto& [name, section] : g_editorState.profileData) {
			if (count >= maxSections) break;
			outSections[count++] = section;
		}

		return count;
	}

	//=============================================================================
	// ASSET MANAGEMENT
	//=============================================================================

	ENGINE_API uint32_t Engine_LoadTexture(EngineContextHandle ctx, const char* path) {
		if (!ctx || !path) return 0;

		// Use the existing texture system
		auto& rm = WanderSpire::RenderResourceManager::Get();
		rm.RegisterTexture(path, path);

		auto texture = rm.GetTexture(path);
		return texture ? texture->GetID() : 0;
	}

	ENGINE_API void Engine_UnloadTexture(EngineContextHandle ctx, uint32_t textureHandle) {
		if (!ctx || textureHandle == 0) return;

		// In the current system, textures are managed automatically
		// This would need to be extended for manual texture management
	}

	ENGINE_API int Engine_GetTextureInfo(EngineContextHandle ctx, uint32_t textureHandle, TextureInfo* outInfo) {
		if (!ctx || textureHandle == 0 || !outInfo) return -1;

		// Try to find texture by handle (this is a simplified implementation)
		auto& rm = WanderSpire::RenderResourceManager::Get();

		// Since we don't have reverse lookup by ID, return default info
		outInfo->width = 0;
		outInfo->height = 0;
		outInfo->channels = 4;
		outInfo->format = GL_RGBA;
		outInfo->memorySize = 0;
		std::strcpy(outInfo->path, "unknown");

		return -1; // Not found
	}

	ENGINE_API int Engine_ReloadAsset(EngineContextHandle ctx, const char* path) {
		if (!ctx || !path) return -1;

		// Use the existing hot-reload system
		auto& rm = WanderSpire::RenderResourceManager::Get();
		rm.RegisterTexture(path, path);

		return 0;
	}

	//=============================================================================
	// ENTITY MANIPULATION
	//=============================================================================

	ENGINE_API EntityId Engine_CloneEntity(EngineContextHandle ctx, EntityId source) {
		if (!ctx || source.id == WS_INVALID_ENTITY) return { WS_INVALID_ENTITY };

		auto* w = static_cast<Wrapper*>(ctx);
		auto& registry = w->reg();
		auto sourceEntity = static_cast<entt::entity>(source.id);

		if (!registry.valid(sourceEntity)) return { WS_INVALID_ENTITY };

		// Create new entity
		entt::entity cloned = registry.create();

		// Copy components using reflection system - use fully qualified namespace
		try {
			const auto& typeMap = Reflect::TypeRegistry::Get().GetNameMap();
			for (const auto& [typeName, typeInfo] : typeMap) {
				if (typeInfo.saveFn && typeInfo.loadFn) {
					nlohmann::json data;
					typeInfo.saveFn(registry, sourceEntity, data);

					if (data.contains(typeInfo.name)) {
						typeInfo.loadFn(registry, cloned, data);
					}
				}
			}
		}
		catch (const std::exception& e) {
			spdlog::error("[Engine_CloneEntity] Failed to clone entity: {}", e.what());
			registry.destroy(cloned);
			return { WS_INVALID_ENTITY };
		}

		// Assign new UUID
		uint64_t uuid = g_nextUuid++;
		if (auto* idComp = registry.try_get<WanderSpire::IDComponent>(cloned)) {
			idComp->uuid = uuid;
		}
		else {
			registry.emplace<WanderSpire::IDComponent>(cloned, uuid);
		}

		return { entt::to_integral(cloned) };
	}

	ENGINE_API int Engine_MoveEntityInHierarchy(EngineContextHandle ctx, EntityId entity, EntityId newParent, int siblingIndex) {
		if (!ctx || entity.id == WS_INVALID_ENTITY) return -1;

		auto* w = static_cast<Wrapper*>(ctx);
		auto& registry = w->reg();
		auto ent = static_cast<entt::entity>(entity.id);
		auto parentEnt = (newParent.id == WS_INVALID_ENTITY) ? entt::null : static_cast<entt::entity>(newParent.id);

		if (!registry.valid(ent)) return -1;
		if (parentEnt != entt::null && !registry.valid(parentEnt)) return -1;

		// Use the scene hierarchy manager
		WanderSpire::SceneHierarchyManager::GetInstance().SetParent(registry, ent, parentEnt);

		return 0;
	}

	ENGINE_API int Engine_GetEntityDepth(EngineContextHandle ctx, EntityId entity) {
		if (!ctx || entity.id == WS_INVALID_ENTITY) return -1;

		auto* w = static_cast<Wrapper*>(ctx);
		auto& registry = w->reg();
		auto ent = static_cast<entt::entity>(entity.id);

		if (!registry.valid(ent)) return -1;

		int depth = 0;
		entt::entity current = ent;

		while (current != entt::null) {
			auto* node = registry.try_get<WanderSpire::SceneNodeComponent>(current);
			if (!node || node->parent == entt::null) break;

			current = node->parent;
			depth++;

			// Prevent infinite loops
			if (depth > 100) break;
		}

		return depth;
	}

	ENGINE_API int Engine_IsEntityAncestorOf(EngineContextHandle ctx, EntityId ancestor, EntityId descendant) {
		if (!ctx || ancestor.id == WS_INVALID_ENTITY || descendant.id == WS_INVALID_ENTITY) return 0;

		auto* w = static_cast<Wrapper*>(ctx);
		auto& registry = w->reg();
		auto ancestorEnt = static_cast<entt::entity>(ancestor.id);
		auto descendantEnt = static_cast<entt::entity>(descendant.id);

		if (!registry.valid(ancestorEnt) || !registry.valid(descendantEnt)) return 0;

		entt::entity current = descendantEnt;
		int safety = 0;

		while (current != entt::null && safety++ < 100) {
			auto* node = registry.try_get<WanderSpire::SceneNodeComponent>(current);
			if (!node) break;

			if (node->parent == ancestorEnt) return 1;
			current = node->parent;
		}

		return 0;
	}

	//=============================================================================
	// OPENGL CONTEXT AND FRAMEBUFFER MANAGEMENT
	//=============================================================================

	ENGINE_API int Engine_InitializeSharedGL(EngineContextHandle ctx, EngineContextHandle sharedContext) {
		if (!ctx) return -1;

		auto* w = static_cast<Wrapper*>(ctx);

		// Store the shared context
		g_glState.sharedContext = static_cast<SDL_GLContext>(sharedContext);
		g_glState.primaryContext = SDL_GL_GetCurrentContext();

		if (!g_glState.primaryContext) {
			spdlog::error("[OpenGL] No current OpenGL context found");
			return -2;
		}

		// Initialize OpenGL state
		glGetIntegerv(GL_FRAMEBUFFER_BINDING, reinterpret_cast<GLint*>(&g_glState.currentFramebuffer));

		spdlog::info("[OpenGL] Shared context initialized successfully");
		return 0;
	}

	ENGINE_API uint32_t Engine_CreateRenderTarget(EngineContextHandle ctx, int width, int height,
		uint32_t* outColorTexture, uint32_t* outDepthTexture) {

		if (!ctx || width <= 0 || height <= 0 || !outColorTexture || !outDepthTexture) {
			return 0;
		}

		using namespace WanderSpire::GL;

		// Generate framebuffer
		GLuint fbo;
		glGenFramebuffers(1, &fbo);
		VertexArrayBinder fboBinder(fbo);
		glBindFramebuffer(GL_FRAMEBUFFER, fbo);

		// Create color texture
		GLuint colorTexture;
		glGenTextures(1, &colorTexture);
		{
			TextureBinder texBinder(colorTexture, GL_TEXTURE_2D);
			glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, nullptr);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		}
		glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, colorTexture, 0);

		// Create depth texture
		GLuint depthTexture;
		glGenTextures(1, &depthTexture);
		{
			TextureBinder texBinder(depthTexture, GL_TEXTURE_2D);
			glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT24, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, nullptr);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		}
		glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, depthTexture, 0);

		// Check framebuffer completeness
		GLenum status = glCheckFramebufferStatus(GL_FRAMEBUFFER);
		if (status != GL_FRAMEBUFFER_COMPLETE) {
			spdlog::error("[OpenGL] Framebuffer incomplete: {}", status);
			glDeleteFramebuffers(1, &fbo);
			glDeleteTextures(1, &colorTexture);
			glDeleteTextures(1, &depthTexture);
			return 0;
		}

		// Store in our tracking maps
		uint32_t fboId = g_glState.nextFBOId++;
		g_glState.framebuffers[fboId] = fbo;
		g_glState.colorTextures[fboId] = colorTexture;
		g_glState.depthTextures[fboId] = depthTexture;

		*outColorTexture = colorTexture;
		*outDepthTexture = depthTexture;

		spdlog::debug("[OpenGL] Created render target {}x{} (FBO: {}, ID: {})", width, height, fbo, fboId);
		return fboId;
	}

	ENGINE_API void Engine_DestroyRenderTarget(EngineContextHandle ctx, uint32_t framebuffer,
		uint32_t colorTexture, uint32_t depthTexture) {

		if (!ctx || framebuffer == 0) return;

		auto fboIt = g_glState.framebuffers.find(framebuffer);
		if (fboIt != g_glState.framebuffers.end()) {
			GLuint fbo = fboIt->second;
			glDeleteFramebuffers(1, &fbo);
			g_glState.framebuffers.erase(fboIt);
		}

		auto colorIt = g_glState.colorTextures.find(framebuffer);
		if (colorIt != g_glState.colorTextures.end()) {
			GLuint tex = colorIt->second;
			glDeleteTextures(1, &tex);
			g_glState.colorTextures.erase(colorIt);
		}

		auto depthIt = g_glState.depthTextures.find(framebuffer);
		if (depthIt != g_glState.depthTextures.end()) {
			GLuint tex = depthIt->second;
			glDeleteTextures(1, &tex);
			g_glState.depthTextures.erase(depthIt);
		}

		spdlog::debug("[OpenGL] Destroyed render target {}", framebuffer);
	}

	ENGINE_API int Engine_ResizeRenderTarget(EngineContextHandle ctx, uint32_t framebuffer,
		uint32_t colorTexture, uint32_t depthTexture, int newWidth, int newHeight) {

		if (!ctx || framebuffer == 0 || newWidth <= 0 || newHeight <= 0) return -1;

		using namespace WanderSpire::GL;

		auto colorIt = g_glState.colorTextures.find(framebuffer);
		auto depthIt = g_glState.depthTextures.find(framebuffer);

		if (colorIt == g_glState.colorTextures.end() || depthIt == g_glState.depthTextures.end()) {
			return -2;
		}

		// Resize color texture
		{
			TextureBinder texBinder(colorIt->second, GL_TEXTURE_2D);
			glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, newWidth, newHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, nullptr);
		}

		// Resize depth texture
		{
			TextureBinder texBinder(depthIt->second, GL_TEXTURE_2D);
			glTexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT24, newWidth, newHeight, 0, GL_DEPTH_COMPONENT, GL_FLOAT, nullptr);
		}

		spdlog::debug("[OpenGL] Resized render target {} to {}x{}", framebuffer, newWidth, newHeight);
		return 0;
	}

	ENGINE_API void Engine_SetRenderTarget(EngineContextHandle ctx, uint32_t framebuffer, int width, int height) {
		if (!ctx) return;

		GLuint fbo = 0;
		if (framebuffer != 0) {
			auto it = g_glState.framebuffers.find(framebuffer);
			if (it != g_glState.framebuffers.end()) {
				fbo = it->second;
			}
		}

		glBindFramebuffer(GL_FRAMEBUFFER, fbo);
		glViewport(0, 0, width, height);
		g_glState.currentFramebuffer = fbo;
		g_glState.viewportWidth = width;
		g_glState.viewportHeight = height;

		spdlog::debug("[OpenGL] Set render target to FBO {} ({}x{})", fbo, width, height);
	}

	ENGINE_API void Engine_RestoreDefaultFramebuffer(EngineContextHandle ctx) {
		if (!ctx) return;

		glBindFramebuffer(GL_FRAMEBUFFER, 0);
		g_glState.currentFramebuffer = 0;

		// Get window size for viewport
		SDL_Window* window = SDL_GL_GetCurrentWindow();
		if (window) {
			int w, h;
			SDL_GetWindowSizeInPixels(window, &w, &h);
			glViewport(0, 0, w, h);
			g_glState.viewportWidth = w;
			g_glState.viewportHeight = h;
		}

		spdlog::debug("[OpenGL] Restored default framebuffer");
	}

	ENGINE_API void Engine_RenderToTarget(EngineContextHandle ctx, EngineContextHandle nativeWindow, int width, int height) {
		if (!ctx) return;

		auto* w = static_cast<Wrapper*>(ctx);

		// Clear the current render target
		glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

		// Execute render commands through the existing render manager
		auto& renderMgr = WanderSpire::RenderManager::Get();
		renderMgr.ExecuteFrame();

		spdlog::debug("[OpenGL] Rendered to target {}x{}", width, height);
	}

	ENGINE_API void Engine_RenderToFramebuffer(EngineContextHandle ctx, uint32_t framebuffer, int width, int height) {
		if (!ctx) return;

		// Set the render target
		Engine_SetRenderTarget(ctx, framebuffer, width, height);

		// Render to it
		Engine_RenderToTarget(ctx, nullptr, width, height);
	}

	ENGINE_API void Engine_BlitFramebuffer(EngineContextHandle ctx, uint32_t srcFBO, uint32_t dstFBO,
		int srcX0, int srcY0, int srcX1, int srcY1,
		int dstX0, int dstY0, int dstX1, int dstY1, uint32_t mask, uint32_t filter) {

		if (!ctx) return;

		GLuint srcGL = 0, dstGL = 0;

		if (srcFBO != 0) {
			auto it = g_glState.framebuffers.find(srcFBO);
			if (it != g_glState.framebuffers.end()) {
				srcGL = it->second;
			}
		}

		if (dstFBO != 0) {
			auto it = g_glState.framebuffers.find(dstFBO);
			if (it != g_glState.framebuffers.end()) {
				dstGL = it->second;
			}
		}

		glBindFramebuffer(GL_READ_FRAMEBUFFER, srcGL);
		glBindFramebuffer(GL_DRAW_FRAMEBUFFER, dstGL);

		glBlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1,
			mask, filter);

		// Restore previous framebuffer
		glBindFramebuffer(GL_FRAMEBUFFER, g_glState.currentFramebuffer);

		spdlog::debug("[OpenGL] Blitted framebuffer {} to {}", srcFBO, dstFBO);
	}

	//=============================================================================
	// OPENGL STATE MANAGEMENT
	//=============================================================================

	ENGINE_API EngineContextHandle Engine_GetGLContext(EngineContextHandle ctx) {
		if (!ctx) return nullptr;
		return static_cast<EngineContextHandle>(SDL_GL_GetCurrentContext());
	}

	ENGINE_API int Engine_MakeGLContextCurrent(EngineContextHandle ctx) {
		if (!ctx) return -1;

		SDL_Window* window = SDL_GL_GetCurrentWindow();
		if (!window) return -2;

		if (g_glState.primaryContext) {
			int result = SDL_GL_MakeCurrent(window, g_glState.primaryContext);
			if (result == 0) {
				spdlog::debug("[OpenGL] Made engine context current");
				return 0;
			}
			else {
				spdlog::error("[OpenGL] Failed to make context current: {}", SDL_GetError());
				return -3;
			}
		}

		return -4;
	}

	ENGINE_API int Engine_ShareGLContext(EngineContextHandle ctx, EngineContextHandle externalContext) {
		if (!ctx || !externalContext) return -1;

		g_glState.sharedContext = static_cast<SDL_GLContext>(externalContext);
		spdlog::info("[OpenGL] Shared context registered");
		return 0;
	}

	ENGINE_API void Engine_SyncGLState(EngineContextHandle ctx) {
		if (!ctx) return;

		// Synchronize OpenGL state between contexts
		glFlush();
		glFinish();

		spdlog::debug("[OpenGL] GL state synchronized");
	}

	ENGINE_API uint32_t Engine_GetGLTextureHandle(EngineContextHandle ctx, uint32_t engineTextureId) {
		if (!ctx || engineTextureId == 0) return 0;

		// Try to find the texture in our engine's resource manager
		auto& rm = WanderSpire::RenderResourceManager::Get();

		// This is a simplified lookup - you might need to extend your resource manager
		// to support reverse lookups by engine texture ID

		return engineTextureId; // Assuming direct mapping for now
	}

	//=============================================================================
	// TEXTURE MANAGEMENT
	//=============================================================================

	ENGINE_API uint32_t Engine_CreateGLTexture(EngineContextHandle ctx, int width, int height,
		uint32_t internalFormat, uint32_t format, uint32_t type) {

		if (!ctx || width <= 0 || height <= 0) return 0;

		using namespace WanderSpire::GL;

		GLuint texture;
		glGenTextures(1, &texture);

		{
			TextureBinder texBinder(texture, GL_TEXTURE_2D);
			glTexImage2D(GL_TEXTURE_2D, 0, internalFormat, width, height, 0, format, type, nullptr);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		}

		spdlog::debug("[OpenGL] Created texture {} ({}x{}, format: 0x{:X})",
			texture, width, height, internalFormat);
		return texture;
	}

	ENGINE_API void Engine_UpdateTextureData(EngineContextHandle ctx, uint32_t textureId,
		int width, int height, uint32_t format, uint32_t type, EngineContextHandle data) {

		if (!ctx || textureId == 0) return;

		using namespace WanderSpire::GL;
		TextureBinder texBinder(textureId, GL_TEXTURE_2D);
		glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, width, height, format, type, data);

		spdlog::debug("[OpenGL] Updated texture {} data ({}x{})", textureId, width, height);
	}

	ENGINE_API int Engine_GetTextureData(EngineContextHandle ctx, uint32_t textureId,
		uint32_t format, uint32_t type, EngineContextHandle outData, int bufferSize) {

		if (!ctx || textureId == 0 || !outData || bufferSize <= 0) return -1;

		using namespace WanderSpire::GL;
		TextureBinder texBinder(textureId, GL_TEXTURE_2D);

		// Get texture dimensions
		GLint width, height;
		glGetTexLevelParameteriv(GL_TEXTURE_2D, 0, GL_TEXTURE_WIDTH, &width);
		glGetTexLevelParameteriv(GL_TEXTURE_2D, 0, GL_TEXTURE_HEIGHT, &height);

		// Calculate required buffer size
		int channels = (format == GL_RGBA) ? 4 : (format == GL_RGB) ? 3 : 1;
		int typeSize = (type == GL_UNSIGNED_BYTE) ? 1 : 4;
		int requiredSize = width * height * channels * typeSize;

		if (bufferSize < requiredSize) {
			spdlog::error("[OpenGL] Buffer too small for texture data: {} < {}", bufferSize, requiredSize);
			return -2;
		}

		glGetTexImage(GL_TEXTURE_2D, 0, format, type, outData);

		spdlog::debug("[OpenGL] Retrieved texture {} data", textureId);
		return requiredSize;
	}

	//=============================================================================
	// EDITOR-SPECIFIC RENDERING
	//=============================================================================

	ENGINE_API void Engine_BeginEditorFrame(EngineContextHandle ctx) {
		if (!ctx) return;

		auto* w = static_cast<Wrapper*>(ctx);
		auto& renderMgr = WanderSpire::RenderManager::Get();

		// Clear previous frame commands
		renderMgr.Clear();

		// Begin a new frame with the current camera
		auto& camera = WanderSpire::Application::GetCamera();
		renderMgr.BeginFrame(camera.GetViewProjectionMatrix());

		// Set up editor-specific rendering state
		g_editorState.editorRenderFlags |= EDITOR_RENDER_SHOW_GRID;

		spdlog::debug("[Engine] Begin editor frame");
	}

	ENGINE_API void Engine_EndEditorFrame(EngineContextHandle ctx) {
		if (!ctx) return;

		auto* w = static_cast<Wrapper*>(ctx);

		// Finalize editor frame rendering
		auto& renderMgr = WanderSpire::RenderManager::Get();

		// Render grid if enabled
		if (g_editorState.gridVisible) {
			// Submit grid rendering commands
			auto& camera = WanderSpire::Application::GetCamera();
			float cameraMinX, cameraMinY, cameraMaxX, cameraMaxY;
			Render_GetCameraBounds(ctx, &cameraMinX, &cameraMinY, &cameraMaxX, &cameraMaxY);

			float gridSize = g_editorState.gridSize;
			int startX = static_cast<int>(std::floor(cameraMinX / gridSize)) - 1;
			int endX = static_cast<int>(std::ceil(cameraMaxX / gridSize)) + 1;
			int startY = static_cast<int>(std::floor(cameraMinY / gridSize)) - 1;
			int endY = static_cast<int>(std::ceil(cameraMaxY / gridSize)) + 1;

			// Draw grid lines
			glm::vec3 gridColor = glm::vec3(g_editorState.gridColor);
			for (int x = startX; x <= endX; ++x) {
				float worldX = x * gridSize;
				Engine_DrawDebugLine(ctx, worldX, cameraMinY, worldX, cameraMaxY,
					gridColor.r, gridColor.g, gridColor.b, 1.0f);
			}
			for (int y = startY; y <= endY; ++y) {
				float worldY = y * gridSize;
				Engine_DrawDebugLine(ctx, cameraMinX, worldY, cameraMaxX, worldY,
					gridColor.r, gridColor.g, gridColor.b, 1.0f);
			}
		}

		renderMgr.EndFrame();

		spdlog::debug("[Editor] End editor frame");
	}

	ENGINE_API void Engine_SetEditorViewport(EngineContextHandle ctx, int x, int y, int width, int height) {
		if (!ctx) return;

		glViewport(x, y, width, height);
		g_editorState.viewportWidth = width;
		g_editorState.viewportHeight = height;

		// Update camera aspect ratio
		WanderSpire::Application::GetCamera().SetScreenSize(static_cast<float>(width), static_cast<float>(height));
	}

	ENGINE_API void Engine_RenderSceneWithOverlays(EngineContextHandle ctx) {
		if (!ctx) return;

		auto* w = static_cast<Wrapper*>(ctx);

		// Render the main scene
		auto& renderMgr = WanderSpire::RenderManager::Get();
		renderMgr.ExecuteFrame();

		// Render editor overlays
		if (g_editorState.editorRenderFlags & EDITOR_RENDER_SHOW_GIZMOS) {
			// Render transform gizmos for selected entities
			const auto& selected = WanderSpire::SelectionManager::GetInstance().GetSelectedEntities();
			for (auto entity : selected) {
				float worldX, worldY;
				Engine_GetEntityWorldPosition(ctx, { entt::to_integral(entity) }, &worldX, &worldY);
				Engine_RenderTransformGizmo(ctx, worldX, worldY, 1.0f, GIZMO_TRANSLATION);
			}
		}

		if (g_editorState.editorRenderFlags & EDITOR_RENDER_SHOW_BOUNDS) {
			// Render entity bounds
			const auto& selected = WanderSpire::SelectionManager::GetInstance().GetSelectedEntities();
			for (auto entity : selected) {
				Engine_RenderSelectionOutline(ctx, { entt::to_integral(entity) }, 1.0f, 1.0f, 0.0f, 2.0f);
			}
		}

		// Render overlay graphics
		Engine_OverlayPresent();

		spdlog::debug("[Editor] Rendered scene with overlays");
	}

	//=============================================================================
	// ADDITIONAL OPENGL INTEROP AND COMPATIBILITY FUNCTIONS
	//=============================================================================

	ENGINE_API int Engine_SupportsExternalGL(EngineContextHandle ctx) {
		if (!ctx) return 0;

		// The engine supports external OpenGL contexts through SDL3's shared context system
		return 1;
	}

	ENGINE_API int Engine_GetGLCapabilities(EngineContextHandle ctx, char* capabilities, int bufferSize) {
		if (!ctx || !capabilities || bufferSize <= 0) return -1;

		try {
			// Query OpenGL capabilities
			const char* vendor = reinterpret_cast<const char*>(glGetString(GL_VENDOR));
			const char* renderer = reinterpret_cast<const char*>(glGetString(GL_RENDERER));
			const char* version = reinterpret_cast<const char*>(glGetString(GL_VERSION));
			const char* glslVersion = reinterpret_cast<const char*>(glGetString(GL_SHADING_LANGUAGE_VERSION));

			GLint maxTextureSize, maxTextureUnits, maxVertexAttribs, maxViewportDims[2];
			glGetIntegerv(GL_MAX_TEXTURE_SIZE, &maxTextureSize);
			glGetIntegerv(GL_MAX_TEXTURE_IMAGE_UNITS, &maxTextureUnits);
			glGetIntegerv(GL_MAX_VERTEX_ATTRIBS, &maxVertexAttribs);
			glGetIntegerv(GL_MAX_VIEWPORT_DIMS, maxViewportDims);

			// Build JSON capabilities string
			nlohmann::json caps;
			caps["vendor"] = vendor ? vendor : "Unknown";
			caps["renderer"] = renderer ? renderer : "Unknown";
			caps["version"] = version ? version : "Unknown";
			caps["glsl_version"] = glslVersion ? glslVersion : "Unknown";
			caps["max_texture_size"] = maxTextureSize;
			caps["max_texture_units"] = maxTextureUnits;
			caps["max_vertex_attribs"] = maxVertexAttribs;
			caps["max_viewport_width"] = maxViewportDims[0];
			caps["max_viewport_height"] = maxViewportDims[1];
			caps["supports_instancing"] = true;
			caps["supports_framebuffers"] = true;
			caps["engine_immediate_mode"] = WanderSpire::Application::IsHeadlessMode() ? false : true;

			// Check for key extensions
			GLint numExtensions;
			glGetIntegerv(GL_NUM_EXTENSIONS, &numExtensions);
			nlohmann::json extensions = nlohmann::json::array();

			// Look for specific important extensions
			std::vector<std::string> importantExts = {
				"GL_ARB_vertex_array_object",
				"GL_ARB_framebuffer_object",
				"GL_ARB_instanced_arrays",
				"GL_ARB_debug_output"
			};

			for (const auto& extName : importantExts) {
				bool found = false;
				for (GLint i = 0; i < numExtensions && !found; ++i) {
					const char* ext = reinterpret_cast<const char*>(glGetStringi(GL_EXTENSIONS, i));
					if (ext && std::string(ext) == extName) {
						extensions.push_back(extName);
						found = true;
					}
				}
			}
			caps["key_extensions"] = extensions;

			std::string capsStr = caps.dump();

			if (static_cast<int>(capsStr.length()) + 1 > bufferSize) {
				return -2; // Buffer too small
			}

			std::strcpy(capabilities, capsStr.c_str());
			return static_cast<int>(capsStr.length());

		}
		catch (const std::exception& e) {
			spdlog::error("[OpenGL] Error getting capabilities: {}", e.what());
			return -3;
		}
	}

	ENGINE_API int Engine_ValidateSharedContext(EngineContextHandle ctx, EngineContextHandle externalContext) {
		if (!ctx || !externalContext) return -1;

		SDL_GLContext engineContext = WanderSpire::Application::GetCurrentGLContext();
		SDL_GLContext extContext = static_cast<SDL_GLContext>(externalContext);

		if (!engineContext || !extContext) {
			spdlog::error("[OpenGL] Invalid contexts for validation");
			return -2;
		}

		// Store current context
		SDL_GLContext currentContext = SDL_GL_GetCurrentContext();
		SDL_Window* currentWindow = SDL_GL_GetCurrentWindow();

		if (!currentWindow) {
			spdlog::error("[OpenGL] No current window for context validation");
			return -3;
		}

		// Test engine context
		if (SDL_GL_MakeCurrent(currentWindow, engineContext) != 0) {
			spdlog::error("[OpenGL] Cannot make engine context current: {}", SDL_GetError());
			return -4;
		}

		// Get engine context properties
		GLint engineMajor, engineMinor, engineProfile;
		SDL_GL_GetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, &engineMajor);
		SDL_GL_GetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, &engineMinor);
		SDL_GL_GetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, &engineProfile);

		// Test external context
		if (SDL_GL_MakeCurrent(currentWindow, extContext) != 0) {
			spdlog::error("[OpenGL] Cannot make external context current: {}", SDL_GetError());
			// Restore engine context
			SDL_GL_MakeCurrent(currentWindow, engineContext);
			return -5;
		}

		// Get external context properties  
		GLint extMajor, extMinor, extProfile;
		SDL_GL_GetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, &extMajor);
		SDL_GL_GetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, &extMinor);
		SDL_GL_GetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, &extProfile);

		// Restore original context
		SDL_GL_MakeCurrent(currentWindow, currentContext);

		// Check compatibility
		if (engineMajor != extMajor || engineMinor != extMinor) {
			spdlog::warn("[OpenGL] Context version mismatch: Engine {}.{}, External {}.{}",
				engineMajor, engineMinor, extMajor, extMinor);
			return 0; // Compatible but with warnings
		}

		if (engineProfile != extProfile) {
			spdlog::warn("[OpenGL] Context profile mismatch: Engine {}, External {}",
				engineProfile, extProfile);
			return 0; // Compatible but with warnings
		}

		g_glState.contextValidated = true;
		g_glState.sharedContext = extContext;
		spdlog::info("[OpenGL] Contexts are fully compatible");
		return 1; // Fully compatible
	}

	ENGINE_API uint32_t Engine_GetLastGLError(EngineContextHandle ctx) {
		if (!ctx) return GL_INVALID_OPERATION;

		// Get and store the current error
		GLenum error = glGetError();
		g_glState.lastGLError = error;

		if (error != GL_NO_ERROR) {
			const char* errorStr = "Unknown error";
			switch (error) {
			case GL_INVALID_ENUM: errorStr = "GL_INVALID_ENUM"; break;
			case GL_INVALID_VALUE: errorStr = "GL_INVALID_VALUE"; break;
			case GL_INVALID_OPERATION: errorStr = "GL_INVALID_OPERATION"; break;
			case GL_OUT_OF_MEMORY: errorStr = "GL_OUT_OF_MEMORY"; break;
			case GL_INVALID_FRAMEBUFFER_OPERATION: errorStr = "GL_INVALID_FRAMEBUFFER_OPERATION"; break;
			}
			spdlog::debug("[OpenGL] Retrieved error: {} (0x{:X})", errorStr, error);
		}

		return static_cast<uint32_t>(error);
	}

	ENGINE_API void Engine_SetImmediateModeRendering(EngineContextHandle ctx, int enabled) {
		if (!ctx) return;

		// Use the Application's immediate mode control
		WanderSpire::Application::SetImmediateModeRendering(enabled != 0);

		// This could be extended to modify RenderManager behavior
		// For example, disabling command batching or using immediate draws
	}

	ENGINE_API int Engine_IsHeadless(EngineContextHandle ctx) {
		if (!ctx) return -1;

		// Use the Application's headless detection
		return WanderSpire::Application::IsHeadlessMode() ? 1 : 0;
	}

	//=============================================================================
	// 6. Enhanced Engine_GetFrameStats implementation
	//=============================================================================

	ENGINE_API void Engine_GetFrameStats(EngineContextHandle ctx, FrameStats* outStats) {
		if (!ctx || !outStats) return;

		auto* w = static_cast<Wrapper*>(ctx);
		auto& registry = w->reg();

		// Count entities
		int entityCount = 0;
		registry.view<entt::entity>().each([&entityCount](auto) { entityCount++; });

		// Use the Application's performance tracking
		outStats->frameTime = WanderSpire::Application::GetLastFrameTime();
		outStats->renderTime = WanderSpire::Application::GetLastRenderTime();
		outStats->updateTime = WanderSpire::Application::GetLastUpdateTime();
		outStats->drawCalls = WanderSpire::Application::GetLastFrameDrawCalls();
		outStats->triangles = outStats->drawCalls * 2; // Rough estimate for quads
		outStats->entities = entityCount;
		outStats->memoryUsed = 0; // TODO: Implement memory tracking
	}

	ENGINE_API void Engine_BindOpenGLContext(WS_GetProcAddress getProc)
	{
		try {
			// Load OpenGL function pointers
			if (!gladLoadGLLoader(getProc)) {
				spdlog::error("[OpenGL] Failed to initialize GLAD");
				return;
			}

			// Detect OpenGL context type and version
			DetectOpenGLContext();

			// Initialize render resource manager with context info
			auto& rrm = WanderSpire::RenderResourceManager::Get();
			rrm.OnContextBound();

			spdlog::info("[OpenGL] Context bound successfully");
		}
		catch (const std::exception& ex) {
			spdlog::error("[OpenGL] Context binding failed: {}", ex.what());
		}
	}

	ENGINE_API void Engine_RegisterRunInContext(WS_RunInContext cb) { g_runInCtx = cb; }
	void Engine_RunInContext(void (*fn)(void*), void* user)
	{
		if (g_runInCtx) g_runInCtx(fn, user);
		else fn(user);                      // fallback: same thread (unit tests)
	}

	//=============================================================================
	// EDITOR-SPECIFIC ENGINE FUNCTIONS
	//=============================================================================

	ENGINE_API int EngineIterateEditor(EngineContextHandle h)
	{
		// Editor-safe iteration that doesn't require SDL window/context
		if (!h) return -1;

		auto* w = static_cast<Wrapper*>(h);
		auto* state = static_cast<WanderSpire::AppState*>(w->appState);
		if (!state) return -1;

		try
		{
			// Get delta time using high-resolution timer instead of SDL
			static auto last = std::chrono::high_resolution_clock::now();
			auto now = std::chrono::high_resolution_clock::now();
			float dt = std::chrono::duration<float>(now - last).count();
			last = now;

			// Clamp delta time to reasonable values
			dt = std::min(dt, 1.0f / 30.0f); // Max 30 FPS minimum

			// Update only the core engine systems (no SDL, no rendering)
			WanderSpire::AssetLoader::Get().UpdateMainThread();
			WanderSpire::FileWatcher::Get().Update();

			// Update world systems (ECS, physics, etc.)
			state->world.Tick(dt, state->ctx);
			state->world.Update(dt, state->ctx);

			return 0; // Success
		}
		catch (const std::exception& ex)
		{
			spdlog::error("[EngineIterateEditor] Exception: {}", ex.what());
			return -2;
		}
		catch (...)
		{
			spdlog::error("[EngineIterateEditor] Unknown exception");
			return -3;
		}
	}

	ENGINE_API int EngineInitEditor(EngineContextHandle h, int argc, char** argv)
	{
		// Editor-specific initialization that doesn't create SDL window
		auto* w = static_cast<Wrapper*>(h);

		try
		{
			// Set headless mode flag
			g_glState.contextValidated = false;

			// Create a minimal AppState without SDL context
			auto* state = new WanderSpire::AppState();
			w->appState = state;
			w->world = &state->world;
			w->ctx = &state->ctx;

			// Initialize logging
			spdlog::set_pattern("[%T] [%^%l%$] %v");
			spdlog::set_level(spdlog::level::debug);

			// Load config
			WanderSpire::ConfigManager::Load("config/engine.json");
			state->ctx.settings = WanderSpire::ConfigManager::Get();

			// Initialize core systems (no graphics)
			state->ctx.assets.Initialize(state->ctx.settings.assetsRoot);
			state->world.Initialize(state->ctx);

			// Load prefabs
			state->ctx.prefabs.LoadPrefabsFromFolder(
				std::filesystem::path(state->ctx.settings.assetsRoot) / "prefabs");

			// Wire up script event forwarding
			WireUpScriptEventForwarding(w, h);

			spdlog::info("[EngineInitEditor] Editor initialization complete");
			return 0; // Success
		}
		catch (const std::exception& ex)
		{
			spdlog::error("[EngineInitEditor] Exception: {}", ex.what());
			return -1;
		}
		catch (...)
		{
			spdlog::error("[EngineInitEditor] Unknown exception");
			return -2;
		}
	}

	ENGINE_API int EngineInitRendering(EngineContextHandle h, int width, int height)
	{
		// Initialize rendering subsystem after OpenGL context is ready
		auto* w = static_cast<Wrapper*>(h);
		auto* state = static_cast<WanderSpire::AppState*>(w->appState);
		if (!state) return -1;

		try
		{
			spdlog::info("[EngineInitRendering] Initializing rendering subsystem {}x{}", width, height);

			// Ensure OpenGL context is properly detected
			DetectOpenGLContext();
			if (!g_glContextInfo.contextValidated) {
				spdlog::error("[EngineInitRendering] OpenGL context not properly detected");
				return -2;
			}

			// Initialize rendering systems
			auto& rm = state->ctx.renderer;

			// Create GL objects with proper error checking
			GLuint vao, vbo, ebo;
			glGenVertexArrays(1, &vao);
			GLenum error = glGetError();
			if (error != GL_NO_ERROR) {
				spdlog::error("[EngineInitRendering] Failed to create VAO: 0x{:X}", error);
				return -3;
			}

			glGenBuffers(1, &vbo);
			glGenBuffers(1, &ebo);

			// Set up basic quad geometry for rendering
			glBindVertexArray(vao);

			static const float verts[] = {
				 0.5f,  0.5f, 0.0f,   1.0f, 1.0f,
				 0.5f, -0.5f, 0.0f,   1.0f, 0.0f,
				-0.5f, -0.5f, 0.0f,   0.0f, 0.0f,
				-0.5f,  0.5f, 0.0f,   0.0f, 1.0f
			};
			static const unsigned idx[] = { 0, 1, 3, 1, 2, 3 };

			glBindBuffer(GL_ARRAY_BUFFER, vbo);
			glBufferData(GL_ARRAY_BUFFER, sizeof(verts), verts, GL_STATIC_DRAW);

			glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, ebo);
			glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(idx), idx, GL_STATIC_DRAW);

			// Set up vertex attributes
			glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)0);
			glEnableVertexAttribArray(0);
			glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 5 * sizeof(float), (void*)(3 * sizeof(float)));
			glEnableVertexAttribArray(1);

			// Set up instance attributes (for terrain rendering)
			const GLsizei instStride = 6 * sizeof(float);   // vec2 pos, vec2 uvOff, vec2 uvSize

			// a_InstancePos  (vec2)
			glVertexAttribPointer(2, 2, GL_FLOAT, GL_FALSE, instStride, (void*)0);
			glEnableVertexAttribArray(2);
			glVertexAttribDivisor(2, 1);

			// a_InstanceUVOffset (vec2)
			glVertexAttribPointer(3, 2, GL_FLOAT, GL_FALSE, instStride, (void*)(2 * sizeof(float)));
			glEnableVertexAttribArray(3);
			glVertexAttribDivisor(3, 1);

			// a_InstanceUVSize (vec2)
			glVertexAttribPointer(4, 2, GL_FLOAT, GL_FALSE, instStride, (void*)(4 * sizeof(float)));
			glEnableVertexAttribArray(4);
			glVertexAttribDivisor(4, 1);

			glBindVertexArray(0);

			// Check for any OpenGL errors
			error = glGetError();
			if (error != GL_NO_ERROR) {
				spdlog::error("[EngineInitRendering] OpenGL error during geometry setup: 0x{:X}", error);
				return -4;
			}

			// Initialize resource manager with our geometry
			rm.Init(vao, ebo);

			// Register shaders - the shader files should already be compatible with OpenGL ES
			rm.RegisterShader("sprite", "shaders/vertex.glsl", "shaders/fragment.glsl");

			// Load basic textures
			rm.RegisterTexture("debug_tile", "textures/debug_tile.png");

			// Set viewport
			glViewport(0, 0, width, height);

			spdlog::info("[EngineInitRendering] Rendering initialization complete");
			return 0;
		}
		catch (const std::exception& ex)
		{
			spdlog::error("[EngineInitRendering] Exception: {}", ex.what());
			return -1;
		}
	}

	ENGINE_API int EngineCanRender(EngineContextHandle h)
	{
		if (!h) return 0;

		auto* w = static_cast<Wrapper*>(h);
		if (!w) return 0;

		auto* state = static_cast<WanderSpire::AppState*>(w->appState);
		if (!state) return 0;

		try {
			// Check OpenGL context
			if (!g_glContextInfo.contextValidated) {
				DetectOpenGLContext();
				if (!g_glContextInfo.contextValidated) {
					return 0;
				}
			}

			// Check if render manager is available
			auto& renderMgr = WanderSpire::RenderManager::Get();
			(void)renderMgr; // Just accessing it to see if it throws

			// Check if editor camera is initialized
			if (!g_editorCamera.initialized) {
				return 0;
			}

			// Quick OpenGL state check
			GLenum error = glGetError();
			if (error != GL_NO_ERROR) {
				spdlog::warn("[EngineCanRender] OpenGL error present: 0x{:X}", error);
				return 0;
			}

			return 1; // Safe to render
		}
		catch (const std::exception& ex) {
			spdlog::error("[EngineCanRender] Exception: {}", ex.what());
			return 0;
		}
		catch (...) {
			return 0;
		}
	}

	ENGINE_API void EngineRenderFrame(EngineContextHandle h)
	{
		if (!h) {
			spdlog::error("[EngineRenderFrame] Invalid context handle");
			return;
		}

		auto* w = static_cast<Wrapper*>(h);
		if (!w) {
			spdlog::error("[EngineRenderFrame] Invalid wrapper");
			return;
		}

		auto* state = static_cast<WanderSpire::AppState*>(w->appState);
		if (!state) {
			spdlog::error("[EngineRenderFrame] Invalid app state");
			return;
		}

		try
		{
			// Ensure OpenGL context is ready
			if (!g_glContextInfo.contextValidated) {
				DetectOpenGLContext();
				if (!g_glContextInfo.contextValidated) {
					spdlog::error("[EngineRenderFrame] OpenGL context not ready");
					return;
				}
			}

			// Initialize editor camera if needed
			if (!g_editorCamera.initialized) {
				g_editorCamera.camera.SetPosition({ 0.0f, 0.0f });
				g_editorCamera.camera.SetZoom(1.0f);
				g_editorCamera.initialized = true;
				spdlog::debug("[EngineRenderFrame] Initialized editor camera");
			}

			// Check for OpenGL errors before rendering
			GLenum error = glGetError();
			if (error != GL_NO_ERROR) {
				spdlog::warn("[EngineRenderFrame] OpenGL error before rendering: 0x{:X}", error);
			}

			auto& renderMgr = WanderSpire::RenderManager::Get();

			// Use editor camera instead of static Application camera
			glm::mat4 viewProjection = g_editorCamera.camera.GetViewProjectionMatrix();

			// Begin frame with current camera
			renderMgr.BeginFrame(viewProjection);

			// Submit rendering commands for current state
			WanderSpire::EventBus::Get().Publish<WanderSpire::FrameRenderEvent>({ state });

			// Execute all commands
			renderMgr.ExecuteFrame();

			// Check for OpenGL errors after rendering
			error = glGetError();
			if (error != GL_NO_ERROR) {
				spdlog::warn("[EngineRenderFrame] OpenGL error after rendering: 0x{:X}", error);
			}
		}
		catch (const std::exception& ex)
		{
			spdlog::error("[EngineRenderFrame] Exception: {}", ex.what());
		}
		catch (...)
		{
			spdlog::error("[EngineRenderFrame] Unknown exception");
		}
	}

	ENGINE_API void EngineSetEditorCamera(EngineContextHandle h, float x, float y, float zoom, float width, float height)
	{
		// Update editor camera safely
		if (!h) return;

		try {
			if (!g_editorCamera.initialized) {
				g_editorCamera.camera = WanderSpire::Camera2D(width, height);
				g_editorCamera.initialized = true;
			}

			g_editorCamera.camera.SetPosition({ x, y });
			g_editorCamera.camera.SetZoom(zoom);

			if (width > 0 && height > 0) {
				g_editorCamera.camera.SetScreenSize(width, height);
			}
		}
		catch (const std::exception& ex) {
			spdlog::error("[EngineSetEditorCamera] Exception: {}", ex.what());
		}
		catch (...) {
			spdlog::error("[EngineSetEditorCamera] Unknown exception");
		}
	}


}/* extern "C" */