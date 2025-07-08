#pragma once

#include "WanderSpire/ECS/JsonGLM.hpp"

#include <nlohmann/json.hpp>
#include <glm/glm.hpp>
#include <stdexcept>
#include <type_traits>
#include <entt/entt.hpp>
#include "WanderSpire/Core/Reflection.h"
#include <climits>

// we need the definition of GridPositionComponent for our specialization
#include "WanderSpire/Components/GridPositionComponent.h"

namespace WanderSpire { using json = nlohmann::json; }

namespace WanderSpire::ReflectJson {

	// Helper to test for default/sentinel values (INT_MAX, 0, etc.)
	inline bool isSentinelVec2(const float v[2]) {
		int x = static_cast<int>(v[0]);
		int y = static_cast<int>(v[1]);
		// only INT_MAX represents “unset” now; (0,0) is a legitimate tile
		return (x == INT_MAX && y == INT_MAX);
	}

	// Overload for glm::ivec2, glm::vec2, etc.
	template<typename Vec>
	inline bool isSentinelVec2(const Vec& v) {
		int x = static_cast<int>(v[0]);
		int y = static_cast<int>(v[1]);
		return (x == INT_MAX && y == INT_MAX);
	}

	// ───────────────────── basic (de)serialisers ────────────────────────────
	inline json to_json(const Reflect::TypeInfo& ti, void* structPtr) {
		json j;
		for (const auto& f : ti.fields) {
			char* base = static_cast<char*>(structPtr) + f.offset;
			switch (f.type) {
			case Reflect::FieldType::Float: {
				const float val = *reinterpret_cast<float*>(base);
				// Don't serialize +0.0 unless field is not at default (change if needed)
				j[f.name] = val;
				break;
			}
			case Reflect::FieldType::Int: {
				const int val = *reinterpret_cast<int*>(base);
				// Don't serialize INT_MAX as a default value for IDs, etc.
				if (val == INT_MAX)
					continue;
				j[f.name] = val;
				break;
			}
			case Reflect::FieldType::Bool:
				j[f.name] = *reinterpret_cast<bool*>(base);
				break;
			case Reflect::FieldType::Vec2: {
				auto v = reinterpret_cast<float(*)[2]>(base);
				// Universal skip for any Vec2 at {INT_MAX, INT_MAX} (sentinel/unset)
				if (isSentinelVec2(*v)) continue; // Don't emit sentinel value at all
				j[f.name] = { int((*v)[0]), int((*v)[1]) }; // Write as ints, not floats
				break;
			}
			case Reflect::FieldType::String: {
				const std::string& val = *reinterpret_cast<std::string*>(base);
				if (val.empty()) continue;
				j[f.name] = val;
				break;
			}
			}
		}
		return j;
	}

	inline void from_json(const Reflect::TypeInfo& ti, void* structPtr, const json& j) {
		for (const auto& f : ti.fields) {
			if (!j.contains(f.name) || j[f.name].is_null()) continue;
			const json& v = j.at(f.name);
			char* base = static_cast<char*>(structPtr) + f.offset;
			try {
				switch (f.type) {
				case Reflect::FieldType::Float: {
					float val{};
					if (v.is_number_float())        val = v.get<float>();
					else if (v.is_number_integer()) val = static_cast<float>(v.get<int>());
					else if (v.is_string())         val = std::stof(v.get<std::string>());
					else                            continue;
					*reinterpret_cast<float*>(base) = val;
					break;
				}
				case Reflect::FieldType::Int: {
					int val{};
					if (v.is_number_integer())      val = v.get<int>();
					else if (v.is_number_float())   val = static_cast<int>(v.get<float>());
					else if (v.is_string())         val = std::stoi(v.get<std::string>());
					else                            continue;
					*reinterpret_cast<int*>(base) = val;
					break;
				}
				case Reflect::FieldType::Bool: {
					bool val{};
					if (v.is_boolean())             val = v.get<bool>();
					else if (v.is_number_integer()) val = (v.get<int>() != 0);
					else if (v.is_string()) {
						std::string s = v.get<std::string>();
						val = (s == "1" || s == "true" || s == "True");
					}
					else continue;
					*reinterpret_cast<bool*>(base) = val;
					break;
				}
				case Reflect::FieldType::Vec2: {
					float x = 0.f, y = 0.f;
					if (v.is_array() && v.size() == 2) {
						// If [null,null] appears, treat as sentinel (unset)
						if ((v[0].is_null() || v[1].is_null())) {
							x = float(INT_MAX); y = float(INT_MAX);
						}
						else {
							x = v[0].get<float>();
							y = v[1].get<float>();
						}
					}
					else if (v.is_string()) {
						sscanf(v.get<std::string>().c_str(), "%f,%f", &x, &y);
					}
					else continue;
					auto vec = reinterpret_cast<float(*)[2]>(base);
					(*vec)[0] = x;
					(*vec)[1] = y;
					break;
				}
				case Reflect::FieldType::String:
					*reinterpret_cast<std::string*>(base) =
						v.is_string() ? v.get<std::string>() : v.dump();
					break;
				}
			}
			catch (...) { /* silently skip malformed */ }
		}
	}

} // namespace WanderSpire::ReflectJson


namespace WanderSpire {

	template<typename C>
	inline const Reflect::TypeInfo& getTypeInfo() {
		const auto& nameMap = Reflect::TypeRegistry::Get().GetNameMap();
		std::type_index key(typeid(C));
		for (auto const& [_, ti] : nameMap)
			if (ti.ti == key) return ti;
		throw std::runtime_error("No reflection info for type " + std::string(typeid(C).name()));
	}

	/* ---------- Save ---------- */
	template<typename C>
	void TrySaveComponent(const entt::registry& reg, entt::entity e, json& ej) {
		if (!reg.all_of<C>(e)) return;
		const auto& ti = getTypeInfo<C>();

		if constexpr (std::is_empty_v<C>) {
			/* empty / tag component: just a presence marker */
			ej[ti.name] = json::object();
		}
		else {
			/* non-empty: dump field data */
			const C& c = reg.get<C>(e);
			void* ptr = const_cast<void*>(static_cast<const void*>(&c));
			json compJson = ReflectJson::to_json(ti, ptr);
			if (!compJson.empty())
				ej[ti.name] = std::move(compJson);
		}
	}

	/* ---------- Load ---------- */
	template<typename C>
	void TryLoadComponent(entt::registry& reg, entt::entity e, const json& ej) {
		const auto& ti = getTypeInfo<C>();
		const std::string shortName = ti.name.substr(
			ti.name.rfind("::") == std::string::npos ? 0 : ti.name.rfind("::") + 2
		);

		if (!ej.contains(ti.name) && !ej.contains(shortName)) return;
		const json& node = ej.contains(ti.name) ? ej.at(ti.name) : ej.at(shortName);

		if constexpr (std::is_empty_v<C>) {
			reg.emplace_or_replace<C>(e);
		}
		else {
			C data{};
			ReflectJson::from_json(ti, &data, node);
			reg.emplace_or_replace<C>(e, std::move(data));
		}
	}

	/* ─── Full specialization for GridPositionComponent ─────────────────────── */

	template<>
	inline void TrySaveComponent<GridPositionComponent>(
		const entt::registry& reg,
		entt::entity e,
		json& ej)
	{
		if (!reg.all_of<GridPositionComponent>(e)) return;
		const auto& ti = getTypeInfo<GridPositionComponent>();
		const auto& comp = reg.get<GridPositionComponent>(e);
		// ADL for glm::ivec2 will emit [x, y]
		if (!ReflectJson::isSentinelVec2(comp.tile))
			ej[ti.name] = { { "tile", comp.tile } };
	}

	template<>
	inline void TryLoadComponent<GridPositionComponent>(
		entt::registry& reg,
		entt::entity e,
		const json& ej)
	{
		const auto& ti = getTypeInfo<GridPositionComponent>();
		const std::string shortName = ti.name.substr(
			ti.name.rfind("::") == std::string::npos ? 0 : ti.name.rfind("::") + 2
		);

		if (!ej.contains(ti.name) && !ej.contains(shortName)) return;
		const json& node = ej.contains(ti.name) ? ej.at(ti.name) : ej.at(shortName);

		// read “tile” via ADL-specialised glm::ivec2 serializer
		glm::ivec2 tile = node.at("tile").get<glm::ivec2>();
		reg.emplace_or_replace<GridPositionComponent>(e, tile);
	}

} // namespace WanderSpire
