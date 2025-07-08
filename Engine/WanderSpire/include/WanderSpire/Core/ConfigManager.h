#pragma once
#include "EngineConfig.h"

namespace WanderSpire {
	class ConfigManager {
	public:
		static void SetTileSize(float tileSize) { instance()._cfg.tileSize = tileSize; }
		static const EngineConfig& Get() { return instance()._cfg; }
		static void Load(const std::string& path);

	private:
		EngineConfig _cfg;
		static ConfigManager& instance() {
			static ConfigManager mgr; return mgr;
		}
	};
}
