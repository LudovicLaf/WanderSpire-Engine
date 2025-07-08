#include "WanderSpire/Systems/RenderSystem.h"
#include "WanderSpire/Graphics/RenderManager.h"
#include "WanderSpire/Graphics/RenderResourceManager.h"
#include "WanderSpire/Graphics/SpriteRenderer.h"
#include "WanderSpire/Graphics/InstanceRenderer.h"
#include "WanderSpire/Core/Application.h"
#include "WanderSpire/Core/EventBus.h"
#include "WanderSpire/Core/Events.h"
#include "WanderSpire/Components/TransformComponent.h"
#include "WanderSpire/Components/SpriteRenderComponent.h"
#include "WanderSpire/Components/ObstacleComponent.h"
#include "WanderSpire/Components/GridPositionComponent.h"
#include "WanderSpire/Components/TilemapChunkComponent.h"
#include "WanderSpire/Components/TilemapLayerComponent.h"
#include "WanderSpire/Components/SceneNodeComponent.h"
#include "WanderSpire/Core/AppState.h"
#include "WanderSpire/World/TileDefinitionManager.h"
#include <algorithm>
#include <vector>

namespace WanderSpire {

	void RenderSystem::Initialize() {
		static EventBus::Subscription tok =
			EventBus::Get().Subscribe<FrameRenderEvent>(
				[](const FrameRenderEvent& ev) {
					if (ev.state) {
						const auto& cam = Application::GetCamera();
						const float halfW = cam.GetWidth() * 0.5f / cam.GetZoom();
						const float halfH = cam.GetHeight() * 0.5f / cam.GetZoom();
						const glm::vec2 minB = cam.GetPosition() - glm::vec2(halfW, halfH);
						const glm::vec2 maxB = cam.GetPosition() + glm::vec2(halfW, halfH);

						SubmitTerrainCommands(ev.state, minB, maxB);
						SubmitEntityCommands(ev.state->world.GetRegistry(), ev.state);
						SubmitDebugCommands(ev.state->world.GetRegistry(), ev.state, minB, maxB);
					}
				});
	}

	void RenderSystem::SubmitEntityCommands(const entt::registry& registry, const AppState* state) {
		auto& renderMgr = RenderManager::Get();

		struct SpriteItem {
			int zOrder;
			const SpriteRenderComponent* render;
			const TransformComponent* transform;
		};
		std::vector<SpriteItem> sprites;

		const auto& cam = Application::GetCamera();
		const float halfW = cam.GetWidth() * 0.5f / cam.GetZoom();
		const float halfH = cam.GetHeight() * 0.5f / cam.GetZoom();
		const glm::vec2 minB = cam.GetPosition() - glm::vec2(halfW, halfH);
		const glm::vec2 maxB = cam.GetPosition() + glm::vec2(halfW, halfH);

		for (auto entity : registry.view<SpriteRenderComponent>()) {
			const auto* render = &registry.get<SpriteRenderComponent>(entity);
			const auto* transform = registry.try_get<TransformComponent>(entity);
			if (!transform) continue;

			// Frustum culling
			const glm::vec2 centre = transform->localPosition;
			if (centre.x + render->worldSize.x < minB.x || centre.x > maxB.x ||
				centre.y + render->worldSize.y < minB.y || centre.y > maxB.y)
				continue;

			int zOrder = 0;
			if (const auto* obstacle = registry.try_get<ObstacleComponent>(entity)) {
				zOrder = obstacle->zOrder;
			}

			sprites.push_back({ zOrder, render, transform });
		}

		// Sort by z-order
		std::sort(sprites.begin(), sprites.end(),
			[](const SpriteItem& a, const SpriteItem& b) {
				return a.zOrder < b.zOrder;
			});

		// Submit sprite commands
		for (const auto& sprite : sprites) {
			renderMgr.SubmitSprite(
				sprite.render->textureID,
				sprite.transform->localPosition,
				sprite.render->worldSize,
				sprite.transform->localRotation,
				{ 1.0f, 1.0f, 1.0f },
				sprite.render->uvOffset,
				sprite.render->uvSize,
				RenderLayer::Entities,
				sprite.zOrder
			);
		}
	}

	void RenderSystem::SubmitTerrainCommands(const AppState* state,
		const glm::vec2& minBound, const glm::vec2& maxBound) {

		if (!state) return;

		auto& renderMgr = RenderManager::Get();
		const auto& registry = state->world.GetRegistry();

		std::vector<entt::entity> tilemapsToRender;

		if (state->HasMainTilemap()) {
			tilemapsToRender.push_back(state->mainTilemap);
		}
		else {
			// Find tilemaps in scene
			auto nodeView = registry.view<SceneNodeComponent>();
			for (auto entity : nodeView) {
				const auto& node = nodeView.get<SceneNodeComponent>(entity);

				if (node.name.find("Tilemap") != std::string::npos && !node.children.empty()) {
					bool hasLayerChildren = std::any_of(node.children.begin(), node.children.end(),
						[&registry](entt::entity child) {
							return registry.any_of<TilemapLayerComponent>(child);
						});

					if (hasLayerChildren) {
						tilemapsToRender.push_back(entity);
					}
				}
			}
		}

		if (tilemapsToRender.empty()) return;

		// Render each tilemap
		for (entt::entity tilemap : tilemapsToRender) {
			auto* tilemapNode = registry.try_get<SceneNodeComponent>(tilemap);
			if (!tilemapNode) continue;

			// Render each layer
			for (entt::entity layerEntity : tilemapNode->children) {
				auto* layerComponent = registry.try_get<TilemapLayerComponent>(layerEntity);
				if (!layerComponent || !layerComponent->visible) continue;

				renderMgr.SubmitCustom([&registry, layerEntity, minBound, maxBound,
					tileSize = state->ctx.settings.tileSize]() {
						RenderTilemapLayer(registry, layerEntity, minBound, maxBound, tileSize);
					}, RenderLayer::Terrain, layerComponent->sortingOrder);
			}
		}
	}

	void RenderSystem::RenderTilemapLayer(const entt::registry& registry,
		entt::entity tilemapLayer,
		const glm::vec2& minBound,
		const glm::vec2& maxBound,
		float tileSize) {

		// Get tile definition manager for texture mapping
		auto& tileDefManager = TileDefinitionManager::GetInstance();

		// Determine which atlas to use - check if layer has specific palette
		std::string primaryAtlasName = "terrain"; // default fallback
		const auto* layerComponent = registry.try_get<TilemapLayerComponent>(tilemapLayer);
		if (layerComponent && layerComponent->paletteId > 0) {
			// Layer has a specific palette - ensure definitions are loaded
			if (layerComponent->autoRefreshDefinitions) {
				tileDefManager.LoadFromPalette(layerComponent->paletteId);
			}
		}

		// Try to get atlas - start with the primary atlas name, then try common fallbacks
		auto& rm = RenderResourceManager::Get();
		TextureAtlas* atlas = rm.GetAtlas(primaryAtlasName);
		if (!atlas) {
			// Try common atlas names
			const char* fallbackNames[] = { "terrain", "tiles", "tileset", nullptr };
			for (int i = 0; fallbackNames[i] && !atlas; ++i) {
				atlas = rm.GetAtlas(fallbackNames[i]);
				if (atlas) {
					primaryAtlasName = fallbackNames[i];
					break;
				}
			}
		}

		auto* shader = rm.GetShader("sprite");
		if (!atlas || !shader || !shader->GetID()) {
			spdlog::warn("[RenderSystem] Missing atlas '{}' or shader for tilemap rendering", primaryAtlasName);
			return;
		}

		GLuint quadVAO = rm.GetQuadVAO();
		GLuint quadEBO = rm.GetQuadEBO();
		if (quadVAO == 0 || quadEBO == 0) return;

		// Calculate visible tile range
		float half = 0.5f * tileSize;
		int x0 = int(std::floor((minBound.x - half) / tileSize));
		int y0 = int(std::floor((minBound.y - half) / tileSize));
		int x1 = int(std::ceil((maxBound.x + half) / tileSize));
		int y1 = int(std::ceil((maxBound.y + half) / tileSize));

		std::vector<InstanceRenderer::InstanceData> instances;
		instances.reserve((y1 - y0) * (x1 - x0));

		auto* layerNode = registry.try_get<SceneNodeComponent>(tilemapLayer);
		if (!layerNode) return;

		// Track missing definitions for logging
		std::unordered_set<int> missingTiles;

		// Process chunks in this layer
		for (entt::entity chunkEntity : layerNode->children) {
			auto* chunkComponent = registry.try_get<TilemapChunkComponent>(chunkEntity);
			if (!chunkComponent || !chunkComponent->loaded || !chunkComponent->visible) continue;

			int chunkWorldX = chunkComponent->chunkCoords.x * chunkComponent->chunkSize;
			int chunkWorldY = chunkComponent->chunkCoords.y * chunkComponent->chunkSize;

			// Skip chunks outside visible area
			if (chunkWorldX + chunkComponent->chunkSize < x0 || chunkWorldX > x1 ||
				chunkWorldY + chunkComponent->chunkSize < y0 || chunkWorldY > y1) {
				continue;
			}

			// Render tiles from this chunk
			for (int localY = 0; localY < chunkComponent->chunkSize; ++localY) {
				for (int localX = 0; localX < chunkComponent->chunkSize; ++localX) {
					int worldX = chunkWorldX + localX;
					int worldY = chunkWorldY + localY;

					if (worldX < x0 || worldX >= x1 || worldY < y0 || worldY >= y1) continue;

					int tileIndex = localY * chunkComponent->chunkSize + localX;
					if (tileIndex >= 0 && tileIndex < static_cast<int>(chunkComponent->tileIds.size())) {
						int tileId = chunkComponent->tileIds[tileIndex];
						if (tileId == -1) continue;

						// NEW: Use TileDefinitionManager instead of hardcoded mapping
						const auto* tileDef = tileDefManager.GetTileDefinition(tileId);
						if (!tileDef) {
							missingTiles.insert(tileId);
							continue;
						}

						// Get the appropriate atlas if different from primary
						TextureAtlas* tileAtlas = atlas;
						if (tileDef->atlasName != primaryAtlasName) {
							tileAtlas = rm.GetAtlas(tileDef->atlasName);
							if (!tileAtlas) {
								// Fallback to primary atlas
								tileAtlas = atlas;
								spdlog::warn("[RenderSystem] Atlas '{}' not found for tile {}, using fallback '{}'",
									tileDef->atlasName, tileId, primaryAtlasName);
							}
						}

						// Get frame from atlas
						auto frame = tileAtlas->GetFrame(tileDef->frameName);
						if (frame.uvSize.x == 0 || frame.uvSize.y == 0) {
							// Frame not found, try fallback frame or skip
							frame = tileAtlas->GetFrame("grass"); // fallback frame
							if (frame.uvSize.x == 0 || frame.uvSize.y == 0) {
								missingTiles.insert(tileId);
								continue;
							}
						}

						glm::vec2 worldPos = glm::vec2(worldX, worldY) * tileSize + glm::vec2(half);
						instances.push_back({ worldPos, frame.uvOffset, frame.uvSize });
					}
				}
			}
		}

		// Log missing tiles (throttled to avoid spam)
		if (!missingTiles.empty()) {
			static std::chrono::steady_clock::time_point lastLog;
			auto now = std::chrono::steady_clock::now();
			if (now - lastLog > std::chrono::seconds(5)) {
				std::string missingStr;
				for (int tileId : missingTiles) {
					if (!missingStr.empty()) missingStr += ", ";
					missingStr += std::to_string(tileId);
				}
				spdlog::warn("[RenderSystem] Missing tile definitions for tiles: {}", missingStr);
				lastLog = now;
			}
		}

		if (instances.empty()) return;

		// Render instances
		auto& instanceRenderer = InstanceRenderer::Get();
		instanceRenderer.BeginFrame(shader, quadVAO, quadEBO);
		instanceRenderer.RenderInstances(atlas->GetTexture()->GetID(), instances, tileSize);
		instanceRenderer.EndFrame();
	}

	void RenderSystem::SubmitDebugCommands(const entt::registry& registry,
		const AppState* state,
		const glm::vec2& minBound,
		const glm::vec2& maxBound) {

		if (!state) return;

		auto& renderMgr = RenderManager::Get();
		const float tileSz = state->ctx.settings.tileSize;
		const glm::vec2 half(tileSz * 0.5f);

		// Grid debug overlay
		if (state->debugEntityTiles) {
			renderMgr.SubmitCustom([tileSz, half, minBound, maxBound]() {
				auto& renderer = SpriteRenderer::Get();

				int x0 = int(std::floor((minBound.x - half.x) / tileSz));
				int y0 = int(std::floor((minBound.y - half.y) / tileSz));
				int x1 = int(std::ceil((maxBound.x + half.x) / tileSz));
				int y1 = int(std::ceil((maxBound.y + half.y) / tileSz));

				for (int y = y0; y < y1; ++y) {
					for (int x = x0; x < x1; ++x) {
						glm::vec2 center = glm::vec2(x, y) * tileSz + half;
						renderer.DrawTileBorder(center - half, tileSz, { 0.8f, 0.8f, 0.8f });
					}
				}
				}, RenderLayer::Debug);
		}

		// Entity tile debug overlay
		if (state->debugEntityTiles) {
			renderMgr.SubmitCustom([&registry, tileSz, half]() {
				auto& renderer = SpriteRenderer::Get();

				for (auto entity : registry.view<GridPositionComponent, SpriteRenderComponent>()) {
					const auto& gp = registry.get<GridPositionComponent>(entity);
					glm::vec2 center = glm::vec2(gp.tile) * tileSz + half;
					renderer.DrawTileBorder(center - half, tileSz, { 0.2f, 0.4f, 1.0f });
				}
				}, RenderLayer::Debug);
		}
	}

	void RenderSystem::Render(const entt::registry& registry, const AppState* state) {
		// Legacy method - use command-based approach instead
		const auto& cam = Application::GetCamera();
		const float halfW = cam.GetWidth() * 0.5f / cam.GetZoom();
		const float halfH = cam.GetHeight() * 0.5f / cam.GetZoom();
		const glm::vec2 minB = cam.GetPosition() - glm::vec2(halfW, halfH);
		const glm::vec2 maxB = cam.GetPosition() + glm::vec2(halfW, halfH);

		SubmitTerrainCommands(state, minB, maxB);
		SubmitEntityCommands(registry, state);
		SubmitDebugCommands(registry, state, minB, maxB);
	}

} // namespace WanderSpire