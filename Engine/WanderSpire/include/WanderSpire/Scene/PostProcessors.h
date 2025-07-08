#pragma once
#include "WanderSpire/Scene/ISceneManager.h"

namespace WanderSpire::Scene {

	// Handles rebuilding tilemap hierarchies and chunk optimization
	class TilemapPostProcessor : public IEntityPostProcessor {
	public:
		void ProcessLoadedEntities(entt::registry& registry,
			const std::vector<entt::entity>& entities) override;
		int GetPriority() const override { return 100; } // Run early, after hierarchy

	private:
		void RestoreTilemapHierarchy(entt::registry& registry);
		void OptimizeChunks(entt::registry& registry);
		void ValidateChunkData(entt::registry& registry, entt::entity chunk);

		// NEW METHODS: Enhanced validation
		void ValidateAndFixChunks(entt::registry& registry);
		void ValidateTilemapStructure(entt::registry& registry);
	};

	// Restores texture references for animations and sprites
	class TexturePostProcessor : public IEntityPostProcessor {
	public:
		void ProcessLoadedEntities(entt::registry& registry,
			const std::vector<entt::entity>& entities) override;
		int GetPriority() const override { return 200; } // Run after tilemap setup

	private:
		void RestoreAnimationTextures(entt::registry& registry);
	};

	// Validates entity hierarchy and relationships
	class HierarchyPostProcessor : public IEntityPostProcessor {
	public:
		void ProcessLoadedEntities(entt::registry& registry,
			const std::vector<entt::entity>& entities) override;
		int GetPriority() const override { return 50; } // Run very early

	private:
		void ValidateParentChildRelationships(entt::registry& registry);
		void CleanupInvalidReferences(entt::registry& registry);
		void UpdateWorldTransforms(entt::registry& registry);
	};

	// Ensures animation systems work without resetting states
	class AnimationPostProcessor : public IEntityPostProcessor {
	public:
		void ProcessLoadedEntities(entt::registry& registry,
			const std::vector<entt::entity>& entities) override;
		int GetPriority() const override { return 300; } // Run after textures

	private:
		void RestoreAnimationSystems(entt::registry& registry);
	};

	// Fixes entities that are stuck between tiles after loading
	class PositionPostProcessor : public IEntityPostProcessor {
	public:
		void ProcessLoadedEntities(entt::registry& registry,
			const std::vector<entt::entity>& entities) override;
		int GetPriority() const override { return 400; } // Run after everything else

	private:
		void SnapStuckEntities(entt::registry& registry);
	};

} // namespace WanderSpire::Scene