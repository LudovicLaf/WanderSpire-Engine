// Engine/WanderSpire/src/ECS/PrefabManager.cpp
#include "WanderSpire/ECS/PrefabManager.h"

#include "WanderSpire/ECS/Serialization.h"
#include "WanderSpire/ECS/SerializableComponents.h"
#include "WanderSpire/Components/AllComponents.h"
#include "WanderSpire/Components/ScriptDataComponent.h"
#include "WanderSpire/Graphics/RenderResourceManager.h"
#include "WanderSpire/Core/EngineContext.h"
#include "WanderSpire/Core/Reflection.h"

#include <spdlog/spdlog.h>
#include <nlohmann/json.hpp>
#include <filesystem>
#include <fstream>
#include <unordered_set>

namespace WanderSpire {

	using json = nlohmann::json;

	/*───────────────────────────────────────────────────────────────────────────
		Helpers – run-time reflection look-ups
	───────────────────────────────────────────────────────────────────────────*/
	namespace {
		/*  Build once, on first use, the set of *native* component names that have
			a registered TypeInfo (short name) or that need bespoke handling.      */
		inline const std::unordered_set<std::string>& NativeComponents()
		{
			static const std::unordered_set<std::string> cache = [] {
				std::unordered_set<std::string> set;
				for (auto const& kv : Reflect::TypeRegistry::Get().GetNameMap())
					set.insert(kv.first);                       // short JSON names

				/* components with custom loader that live outside reflection */
				set.insert("AnimationClipsComponent");
				return set;
				}();
			return cache;
		}

		inline bool IsNativeComponent(const std::string& name)
		{
			return NativeComponents().count(name) != 0;
		}

		/*  Generic run-time loader: use the TypeRegistry's loadFn to materialise
			a reflected component from JSON.                                       */
		inline void LoadReflected(
			const std::string& name,
			const json& node,
			entt::registry& reg,
			entt::entity  ent)
		{
			auto& map = Reflect::TypeRegistry::Get().GetNameMap();
			auto  it = map.find(name);
			if (it == map.end() || !it->second.loadFn)
				return;

			json wrapper;
			wrapper[name] = node;                   // loadFn expects a wrapped object
			it->second.loadFn(reg, ent, wrapper);
		}

	} // anonymous namespace


	/*───────────────────────────────────────────────────────────────────────────
		PrefabManager – singleton boiler-plate
	───────────────────────────────────────────────────────────────────────────*/
	PrefabManager& PrefabManager::GetInstance()
	{
		static PrefabManager inst;
		return inst;
	}

	void PrefabManager::RegisterPrefab(const std::string& name, PrefabFunction fn)
	{
		m_CodePrefabs[name] = std::move(fn);
	}

	void PrefabManager::LoadPrefabsFromFolder(const std::filesystem::path& folder)
	{
		namespace fs = std::filesystem;
		if (!fs::exists(folder)) {
			spdlog::warn("[PrefabManager] '{}' not found", folder.string());
			return;
		}

		size_t count = 0;
		for (auto const& f : fs::recursive_directory_iterator(folder))
		{
			if (!f.is_regular_file() || f.path().extension() != ".json") continue;

			json j;
			try { std::ifstream{ f.path() } >> j; }
			catch (std::exception const& ex) {
				spdlog::error("[PrefabManager] JSON error in {}: {}", f.path().string(), ex.what());
				continue;
			}

			std::string key = j.value("name", f.path().stem().string());
			m_JsonPrefabs[key] = std::move(j);
			++count;
		}
		spdlog::info("[PrefabManager] loaded {} JSON prefabs from '{}'", count, folder.string());
	}

	/*───────────────────────────────────────────────────────────────────────────
		Public Instantiate – now fully data-driven
	───────────────────────────────────────────────────────────────────────────*/
	entt::entity PrefabManager::Instantiate(
		const std::string& name,
		entt::registry& registry,
		const glm::vec2& worldPos)
	{
		if (auto it = m_JsonPrefabs.find(name); it != m_JsonPrefabs.end())
			return instantiateFromJson(it->second, registry, worldPos);

		if (auto it = m_CodePrefabs.find(name); it != m_CodePrefabs.end())
			return it->second(registry, worldPos);

		spdlog::warn("[PrefabManager] instantiate failed – '{}' not found", name);
		return entt::null;
	}

	/*───────────────────────────────────────────────────────────────────────────
		Generic loader helper for reflected components
	───────────────────────────────────────────────────────────────────────────*/
	template<typename C>
	void PrefabManager::tryLoad(entt::entity        e,
		entt::registry& registry,
		const json& comps)
	{
		TryLoadComponent<C>(registry, e, comps);
	}

	/*───────────────────────────────────────────────────────────────────────────
		Core: instantiateFromJson
	───────────────────────────────────────────────────────────────────────────*/
	entt::entity PrefabManager::instantiateFromJson(
		const json& data,
		entt::registry& registry,
		const glm::vec2& worldPos)
	{
		auto e = registry.create();
		const json& comps = data["components"];

		/* 1)  Pass over every JSON object, dispatching either to reflection
			   or to a bespoke loader, else storing it in ScriptDataComponent.  */
		for (auto const& item : comps.items())
		{
			const std::string& comp = item.key();
			const json& body = item.value();

			if (comp == "AnimationClipsComponent")
			{
				AnimationClipsComponent acc;
				acc.LoadFromJson(body);
				registry.emplace_or_replace<AnimationClipsComponent>(e, std::move(acc));
				continue;
			}

			if (IsNativeComponent(comp))            // reflected native component
			{
				LoadReflected(comp, body, registry, e);
				continue;
			}

			/* ── managed-only / unknown component → ScriptDataComponent ───── */
			json merged;
			if (auto ptr = registry.try_get<ScriptDataComponent>(e))
				try { merged = json::parse(ptr->data); }
			catch (...) {}

			merged[comp] = body;
			registry.emplace_or_replace<ScriptDataComponent>(e, merged.dump());
		}

		/* 2)  Placement overrides so caller chooses spawn tile/world pos.       */
		if (registry.all_of<GridPositionComponent>(e))
			registry.get<GridPositionComponent>(e).tile = glm::ivec2(worldPos);

		if (registry.all_of<TransformComponent>(e))
			registry.get<TransformComponent>(e).localPosition = worldPos;

		/* 3)  NEW: Set up animated sprite texture based on SpriteComponent.atlasName  */
		if (registry.all_of<SpriteAnimationComponent, SpriteComponent>(e))
		{
			auto& sp = registry.get<SpriteComponent>(e);
			auto& anim = registry.get<SpriteAnimationComponent>(e);

			// NEW LOGIC: If frameName is empty, treat atlasName as spritesheet path
			if (sp.frameName.empty()) {
				// This is a spritesheet reference (for animated entities)
				auto tex = RenderResourceManager::Get().GetTexture(sp.atlasName);
				if (tex) {
					anim.texture = tex;
					anim.columns = anim.texture->GetWidth() / anim.frameWidth;
					anim.rows = anim.texture->GetHeight() / anim.frameHeight;

					spdlog::debug("[Prefab] Loaded spritesheet '{}' for animated entity '{}'",
						sp.atlasName, data.value("name", "<unnamed>"));
				}
				else {
					spdlog::warn("[Prefab] Spritesheet '{}' not found for '{}'",
						sp.atlasName, data.value("name", "<unnamed>"));
				}
			}
			else {
				// This is an atlas reference (unusual for animated entities, but supported)
				if (auto* atlas = RenderResourceManager::Get().GetAtlas(sp.atlasName)) {
					anim.texture = atlas->GetTexture();
					if (anim.texture) {
						anim.columns = anim.texture->GetWidth() / anim.frameWidth;
						anim.rows = anim.texture->GetHeight() / anim.frameHeight;
					}
				}
				else {
					spdlog::warn("[Prefab] Atlas '{}' not found for animated entity '{}'",
						sp.atlasName, data.value("name", "<unnamed>"));
				}
			}
		}

		/* 4)  Guarantee every renderable entity has a grid tile.                */
		if (!registry.any_of<GridPositionComponent>(e))
		{
			float ts = registry.ctx().get<EngineContext*>()->settings.tileSize;
			glm::ivec2 tile = glm::floor(worldPos / ts);
			registry.emplace<GridPositionComponent>(e, tile);
		}

		return e;
	}

} // namespace WanderSpire