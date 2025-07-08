#pragma once
#include <string>
#include <vector>
#include <unordered_map>
#include <unordered_set>
#include <functional>
#include <entt/entt.hpp>

namespace WanderSpire {

	class AssetDependencyTracker {
	public:
		static AssetDependencyTracker& GetInstance();

		void RegisterDependency(entt::entity entity, const std::string& assetPath);
		void UnregisterDependency(entt::entity entity, const std::string& assetPath);
		void UpdateAssetTimestamp(const std::string& assetPath, uint64_t timestamp);

		std::vector<entt::entity> GetDependentEntities(const std::string& assetPath);
		std::vector<std::string> GetAssetDependencies(entt::entity entity);
		bool HasMissingDependencies(entt::registry& registry, entt::entity entity);

		void ScanForChangedAssets();
		void ReloadChangedAssets(entt::registry& registry);

		using AssetChangedCallback = std::function<void(const std::string&, const std::vector<entt::entity>&)>;
		void RegisterAssetChangedCallback(AssetChangedCallback callback);

	private:
		std::unordered_map<std::string, std::unordered_set<entt::entity>> assetToEntities;
		std::unordered_map<entt::entity, std::unordered_set<std::string>> entityToAssets;
		std::unordered_map<std::string, uint64_t> assetTimestamps;
		std::vector<AssetChangedCallback> assetChangedCallbacks;

		void NotifyAssetChanged(const std::string& assetPath, const std::vector<entt::entity>& entities);
	};

} // namespace WanderSpire
