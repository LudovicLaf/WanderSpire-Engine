// WanderSpire/src/Editor/AssetDependencyTracker.cpp
#include "WanderSpire/Editor/AssetDependencyTracker.h"
#include "WanderSpire/Components/AssetReferenceComponent.h"
#include <filesystem>
#include <spdlog/spdlog.h>

namespace WanderSpire {

	AssetDependencyTracker& AssetDependencyTracker::GetInstance() {
		static AssetDependencyTracker instance;
		return instance;
	}

	void AssetDependencyTracker::RegisterDependency(entt::entity entity, const std::string& assetPath) {
		assetToEntities[assetPath].insert(entity);
		entityToAssets[entity].insert(assetPath);

		// Update timestamp if we haven't seen this asset before
		if (assetTimestamps.find(assetPath) == assetTimestamps.end()) {
			try {
				if (std::filesystem::exists(assetPath)) {
					auto time = std::filesystem::last_write_time(assetPath);
					assetTimestamps[assetPath] = time.time_since_epoch().count();
				}
			}
			catch (const std::exception& e) {
				spdlog::warn("[AssetDependencyTracker] Failed to get timestamp for {}: {}", assetPath, e.what());
				assetTimestamps[assetPath] = 0;
			}
		}

		spdlog::debug("[AssetDependencyTracker] Registered dependency: entity {} -> {}",
			entt::to_integral(entity), assetPath);
	}

	void AssetDependencyTracker::UnregisterDependency(entt::entity entity, const std::string& assetPath) {
		auto assetIt = assetToEntities.find(assetPath);
		if (assetIt != assetToEntities.end()) {
			assetIt->second.erase(entity);
			if (assetIt->second.empty()) {
				assetToEntities.erase(assetIt);
			}
		}

		auto entityIt = entityToAssets.find(entity);
		if (entityIt != entityToAssets.end()) {
			entityIt->second.erase(assetPath);
			if (entityIt->second.empty()) {
				entityToAssets.erase(entityIt);
			}
		}

		spdlog::debug("[AssetDependencyTracker] Unregistered dependency: entity {} -> {}",
			entt::to_integral(entity), assetPath);
	}

	void AssetDependencyTracker::UpdateAssetTimestamp(const std::string& assetPath, uint64_t timestamp) {
		uint64_t oldTimestamp = assetTimestamps[assetPath];
		assetTimestamps[assetPath] = timestamp;

		if (oldTimestamp != 0 && timestamp != oldTimestamp) {
			// Asset has changed
			auto entities = GetDependentEntities(assetPath);
			if (!entities.empty()) {
				NotifyAssetChanged(assetPath, entities);
			}
		}
	}

	std::vector<entt::entity> AssetDependencyTracker::GetDependentEntities(const std::string& assetPath) {
		auto it = assetToEntities.find(assetPath);
		if (it != assetToEntities.end()) {
			return std::vector<entt::entity>(it->second.begin(), it->second.end());
		}
		return {};
	}

	std::vector<std::string> AssetDependencyTracker::GetAssetDependencies(entt::entity entity) {
		auto it = entityToAssets.find(entity);
		if (it != entityToAssets.end()) {
			return std::vector<std::string>(it->second.begin(), it->second.end());
		}
		return {};
	}

	bool AssetDependencyTracker::HasMissingDependencies(entt::registry& registry, entt::entity entity) {
		auto* assetRef = registry.try_get<AssetReferenceComponent>(entity);
		if (!assetRef) return false;

		for (const auto& dep : assetRef->dependencies) {
			if (!std::filesystem::exists(dep.assetPath)) {
				return true;
			}
		}
		return false;
	}

	void AssetDependencyTracker::ScanForChangedAssets() {
		std::vector<std::string> changedAssets;

		for (auto& [assetPath, storedTimestamp] : assetTimestamps) {
			try {
				if (std::filesystem::exists(assetPath)) {
					auto currentTime = std::filesystem::last_write_time(assetPath);
					uint64_t currentTimestamp = currentTime.time_since_epoch().count();

					if (currentTimestamp != storedTimestamp) {
						changedAssets.push_back(assetPath);
						assetTimestamps[assetPath] = currentTimestamp;
					}
				}
				else if (storedTimestamp != 0) {
					// Asset was deleted
					changedAssets.push_back(assetPath);
					assetTimestamps[assetPath] = 0;
				}
			}
			catch (const std::exception& e) {
				spdlog::warn("[AssetDependencyTracker] Error checking asset {}: {}", assetPath, e.what());
			}
		}

		// Notify about changed assets
		for (const auto& assetPath : changedAssets) {
			auto entities = GetDependentEntities(assetPath);
			if (!entities.empty()) {
				NotifyAssetChanged(assetPath, entities);
			}
		}
	}

	void AssetDependencyTracker::ReloadChangedAssets(entt::registry& registry) {
		// Update AssetReferenceComponents for entities with missing or changed dependencies
		auto view = registry.view<AssetReferenceComponent>();
		for (auto entity : view) {
			auto& assetRef = view.get<AssetReferenceComponent>(entity);

			bool hasChanges = false;
			for (auto& dep : assetRef.dependencies) {
				try {
					if (std::filesystem::exists(dep.assetPath)) {
						auto currentTime = std::filesystem::last_write_time(dep.assetPath);
						uint64_t currentTimestamp = currentTime.time_since_epoch().count();

						if (currentTimestamp != dep.lastModified) {
							dep.lastModified = currentTimestamp;
							dep.missing = false;
							hasChanges = true;
						}
					}
					else {
						if (!dep.missing) {
							dep.missing = true;
							hasChanges = true;
						}
					}
				}
				catch (const std::exception& e) {
					spdlog::warn("[AssetDependencyTracker] Error updating dependency {}: {}", dep.assetPath, e.what());
					if (!dep.missing) {
						dep.missing = true;
						hasChanges = true;
					}
				}
			}

			if (hasChanges) {
				assetRef.dependenciesResolved = !HasMissingDependencies(registry, entity);
			}
		}
	}

	void AssetDependencyTracker::RegisterAssetChangedCallback(AssetChangedCallback callback) {
		assetChangedCallbacks.push_back(std::move(callback));
	}

	void AssetDependencyTracker::NotifyAssetChanged(const std::string& assetPath, const std::vector<entt::entity>& entities) {
		for (auto& callback : assetChangedCallbacks) {
			callback(assetPath, entities);
		}

		spdlog::info("[AssetDependencyTracker] Asset '{}' changed, affecting {} entities",
			assetPath, entities.size());
	}

} // namespace WanderSpire