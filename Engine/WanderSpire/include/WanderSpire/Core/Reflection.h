#pragma once
#include <string>
#include <vector>
#include <typeindex>
#include <unordered_map>
#include <functional>
#include <cstddef>          // offsetof
#include <sstream>
#include <unordered_set>

#include <entt/entt.hpp>
#include <nlohmann/json.hpp>

namespace WanderSpire {
	using json = nlohmann::json;
	template<typename C> void TrySaveComponent(const entt::registry&, entt::entity, json&);
	template<typename C> void TryLoadComponent(entt::registry&, entt::entity, const json&);
}

namespace Reflect {

	enum class FieldType { Float, Int, Bool, Vec2, String };

	struct FieldInfo {
		std::string name;
		FieldType type;
		size_t offset{};
		float min{}, max{}, step{};
		bool hidden{ false };

		std::string getAsString(void* base) const;
		void        setFromString(void* base, const std::string& s) const;
	};

	struct TypeInfo {
		std::string name;
		std::type_index ti{ typeid(void) };
		std::function<void* ()> factory;
		std::vector<FieldInfo> fields;

		std::function<void(const entt::registry&, entt::entity, WanderSpire::json&)> saveFn;
		std::function<void(entt::registry&, entt::entity, const WanderSpire::json&)> loadFn;
		std::function<void(const entt::registry&, std::unordered_set<entt::entity>&)> collectFn;

		TypeInfo& addField(const std::string& n, FieldType ft,
			size_t off, float mn, float mx, float st);
	};

	class TypeRegistry {
	public:
		static TypeRegistry& Get() {
			static TypeRegistry inst;
			return inst;
		}

		template<typename T>
		TypeInfo& registerType(const std::string& typeName)
		{
			// ── Extract final token after "::" ──────────────────────────────
			std::string shortName = typeName;
			if (auto pos = typeName.rfind("::"); pos != std::string::npos)
				shortName = typeName.substr(pos + 2); // skip "::"

			TypeInfo ti;
			ti.name = shortName;           // Use short name for JSON lookup
			ti.ti = typeid(T);
			ti.factory = []() { return static_cast<void*>(new T()); };

			// ── ECS serialization hooks ─────────────────────────────────────
			ti.saveFn = [](const entt::registry& reg, entt::entity e, WanderSpire::json& j) {
				WanderSpire::TrySaveComponent<T>(reg, e, j);
				};
			ti.loadFn = [](entt::registry& reg, entt::entity e, const WanderSpire::json& j) {
				WanderSpire::TryLoadComponent<T>(reg, e, j);
				};
			ti.collectFn = [](const entt::registry& reg, std::unordered_set<entt::entity>& out) {
				for (auto ent : reg.view<T>()) out.insert(ent);
				};

			// ── Store in maps ───────────────────────────────────────────────
			auto res = byName_.emplace(shortName, std::move(ti));
			byType_[res.first->second.ti] = &res.first->second;
			return res.first->second;
		}


		const auto& GetNameMap() const { return byName_; }

	private:
		std::unordered_map<std::string, TypeInfo> byName_;
		std::unordered_map<std::type_index, const TypeInfo*> byType_;
	};

} // namespace Reflect
