#pragma once

#include <string>
#include <unordered_map>
#include <functional>
#include <filesystem>
#include <glm/glm.hpp>
#include <entt/entt.hpp>
#include <nlohmann/json.hpp>

namespace WanderSpire {

	class PrefabManager {
	public:
		using PrefabFunction = std::function<entt::entity(entt::registry&, const glm::vec2&)>;

		static PrefabManager& GetInstance();

		/* ── C++ legacy path ─────────────────────────────────────────────── */
		void RegisterPrefab(const std::string& name, PrefabFunction fn);

		/* ── JSON prefabs ────────────────────────────────────────────────── */
		void LoadPrefabsFromFolder(const std::filesystem::path& folder);

		entt::entity Instantiate(const std::string& name,
			entt::registry& registry,
			const glm::vec2& worldPosition);

	private:
		/* helpers */
		entt::entity instantiateFromJson(const nlohmann::json& data,
			entt::registry& registry,
			const glm::vec2& worldPos);

		template<typename C>
		void tryLoad(entt::entity e,
			entt::registry& registry,
			const nlohmann::json& comps);

		std::unordered_map<std::string, PrefabFunction>          m_CodePrefabs;
		std::unordered_map<std::string, nlohmann::json>          m_JsonPrefabs;
	};

}