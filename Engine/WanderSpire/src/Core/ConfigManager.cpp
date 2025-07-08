#include "WanderSpire/Core/ConfigManager.h"
#include <nlohmann/json.hpp>
#include <fstream>
#include <spdlog/spdlog.h>

using json = nlohmann::json;

namespace WanderSpire {

	void ConfigManager::Load(const std::string& path) {
		std::ifstream ifs(path);
		if (!ifs.is_open()) {
			spdlog::warn("[ConfigManager] Could not open config file: {}", path);
			return;
		}

		try {
			json j;
			ifs >> j;
			instance()._cfg = j.get<EngineConfig>();
			spdlog::info("[ConfigManager] Loaded config from '{}'", path);
		}
		catch (const std::exception& e) {
			spdlog::error("[ConfigManager] Failed to parse '{}': {}", path, e.what());
		}
	}

} // namespace WanderSpire
