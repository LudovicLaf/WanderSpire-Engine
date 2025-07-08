#pragma once
#include <nlohmann/json.hpp>
#include <glm/glm.hpp>
// This file provides nlohmann::json ADL support for glm::ivec2,
// so that our reflection-based serializers will treat tile coordinates
// as integers rather than bit-casting floats.

namespace nlohmann {
	// glm::vec2 serialization
	template<>
	struct adl_serializer<glm::vec2> {
		static void to_json(json& j, const glm::vec2& v) {
			j = json{ v.x, v.y };
		}
		static void from_json(const json& j, glm::vec2& v) {
			if (j.is_array() && j.size() == 2) {
				v.x = j[0].get<float>();
				v.y = j[1].get<float>();
			}
		}
	};

	// glm::ivec2 serialization
	template<>
	struct adl_serializer<glm::ivec2> {
		static void to_json(json& j, const glm::ivec2& v) {
			j = json{ v.x, v.y };
		}
		static void from_json(const json& j, glm::ivec2& v) {
			if (j.is_array() && j.size() == 2) {
				v.x = j[0].get<int>();
				v.y = j[1].get<int>();
			}
		}
	};

	// glm::vec3 serialization
	template<>
	struct adl_serializer<glm::vec3> {
		static void to_json(json& j, const glm::vec3& v) {
			j = json{ v.x, v.y, v.z };
		}
		static void from_json(const json& j, glm::vec3& v) {
			if (j.is_array() && j.size() == 3) {
				v.x = j[0].get<float>();
				v.y = j[1].get<float>();
				v.z = j[2].get<float>();
			}
		}
	};

	// glm::vec4 serialization
	template<>
	struct adl_serializer<glm::vec4> {
		static void to_json(json& j, const glm::vec4& v) {
			j = json{ v.x, v.y, v.z, v.w };
		}
		static void from_json(const json& j, glm::vec4& v) {
			if (j.is_array() && j.size() == 4) {
				v.x = j[0].get<float>();
				v.y = j[1].get<float>();
				v.z = j[2].get<float>();
				v.w = j[3].get<float>();
			}
		}
	};
}