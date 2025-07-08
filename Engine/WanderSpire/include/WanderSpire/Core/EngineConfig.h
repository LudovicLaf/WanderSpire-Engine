#pragma once

#include <string>
#include <nlohmann/json.hpp>

struct EngineConfig {
	float       tileSize = 64.0f;
	float       tickInterval = 0.6f;
	int         chunkSize = 32;
	std::string assetsRoot = "Assets/";
	std::string mapsRoot = "Assets/maps/";

	// (De)serializers for nlohmann::json
	friend void to_json(nlohmann::json& j, const EngineConfig& c) {
		j = {
			{"tileSize",     c.tileSize},
			{"tickInterval", c.tickInterval},
			{"chunkSize",    c.chunkSize},
			{"assetsRoot",   c.assetsRoot},
			{"mapsRoot",     c.mapsRoot}
		};
	}
	friend void from_json(const nlohmann::json& j, EngineConfig& c) {
		c.tileSize = j.value("tileSize", c.tileSize);
		c.tickInterval = j.value("tickInterval", c.tickInterval);
		c.chunkSize = j.value("chunkSize", c.chunkSize);
		c.assetsRoot = j.value("assetsRoot", c.assetsRoot);
		c.mapsRoot = j.value("mapsRoot", c.mapsRoot);
	}
};
