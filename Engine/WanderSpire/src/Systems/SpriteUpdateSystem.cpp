#include "WanderSpire/Systems/SpriteUpdateSystem.h"

#include "WanderSpire/Core/EngineContext.h"
#include "WanderSpire/Components/SpriteComponent.h"
#include "WanderSpire/Components/SpriteAnimationComponent.h"
#include "WanderSpire/Components/SpriteRenderComponent.h"
#include "WanderSpire/Components/FacingComponent.h"
#include "WanderSpire/Graphics/RenderResourceManager.h"
#include "WanderSpire/Graphics/SpriteSheet.h"

#include <entt/entt.hpp>
#include <glm/vec2.hpp>
#include <unordered_map>

namespace WanderSpire {

	void SpriteUpdateSystem::Update(entt::registry& registry, const EngineContext& ctx) {
		auto& renderer = ctx.renderer;
		const float defaultTileSize = ctx.settings.tileSize;

		// Cache for texture lookups to avoid repeated map searches
		static std::unordered_map<std::string, std::weak_ptr<Texture>> textureCache;
		static std::unordered_map<std::string, TextureAtlas*> atlasCache;

		// Clear expired weak pointers periodically (every ~1000 frames)
		static int frameClearCounter = 0;
		if (++frameClearCounter > 1000) {
			frameClearCounter = 0;
			for (auto it = textureCache.begin(); it != textureCache.end();) {
				if (it->second.expired()) {
					it = textureCache.erase(it);
				}
				else {
					++it;
				}
			}
		}

		// ─── animated sprites ────────────────────────────────────────────
		auto animatedView = registry.view<SpriteAnimationComponent, SpriteComponent>();
		for (auto entity : animatedView) {
			const auto& [anim, sprite] = animatedView.get<SpriteAnimationComponent, SpriteComponent>(entity);

			SpriteRenderComponent rc{};
			if (anim.texture) {
				// Use cached texture reference
				rc.textureID = anim.texture->GetID();

				// Compute the right sub‑UV via SpriteSheet (cache this too if needed)
				SpriteSheet sheet(
					anim.texture->GetWidth(),
					anim.texture->GetHeight(),
					anim.frameWidth,
					anim.frameHeight
				);
				int idx = anim.startFrame + anim.currentFrame;
				auto uv = sheet.GetUVForFrame(idx);
				rc.uvOffset = { uv.x, uv.y };
				rc.uvSize = { uv.z - uv.x, uv.w - uv.y };
				rc.worldSize = { anim.worldWidth, anim.worldHeight };
			}
			else {
				// no texture → blank full‑quad
				rc.textureID = 0;
				rc.uvOffset = { 0,0 };
				rc.uvSize = { 1,1 };
				rc.worldSize = { anim.worldWidth, anim.worldHeight };
			}

			// Flip horizontally if facing left (avoid repeated component lookup)
			if (const auto* facing = registry.try_get<FacingComponent>(entity);
				facing && facing->facing == Facing::Left) {
				rc.worldSize.x = -rc.worldSize.x;
			}

			registry.emplace_or_replace<SpriteRenderComponent>(entity, std::move(rc));
		}

		// ─── static sprites ───────────────────────────────────────────────
		auto staticView = registry.view<SpriteComponent>(entt::exclude<SpriteAnimationComponent>);
		for (auto entity : staticView) {
			const auto& sprite = staticView.get<SpriteComponent>(entity);

			SpriteRenderComponent rc{};

			// NEW LOGIC: Check if frameName is empty to decide between atlas vs spritesheet
			bool useAtlas = !sprite.frameName.empty();

			if (useAtlas) {
				// Static sprite using atlas (frameName specified)
				TextureAtlas* atlas = nullptr;
				auto atlasIt = atlasCache.find(sprite.atlasName);
				if (atlasIt != atlasCache.end()) {
					atlas = atlasIt->second;
				}
				else {
					atlas = renderer.GetAtlas(sprite.atlasName);
					atlasCache[sprite.atlasName] = atlas; // Cache even if null
				}

				if (atlas) {
					// JSON‑atlas frame
					rc.textureID = atlas->GetTexture()->GetID();
					auto frame = atlas->GetFrame(sprite.frameName);
					rc.uvOffset = frame.uvOffset;
					rc.uvSize = frame.uvSize;
					rc.worldSize = { defaultTileSize, defaultTileSize };
				}
				else {
					// Atlas not found - fallback to missing texture
					spdlog::warn("[SpriteUpdate] Atlas '{}' not found for static sprite", sprite.atlasName);
					rc.textureID = 0;
					rc.uvOffset = { 0,0 };
					rc.uvSize = { 1,1 };
					rc.worldSize = { defaultTileSize, defaultTileSize };
				}
			}
			else {
				// Static sprite using spritesheet (frameName empty - should be rare for static sprites)
				// This would typically be used for single-frame "sprites" that don't animate
				std::shared_ptr<Texture> tex;
				auto texIt = textureCache.find(sprite.atlasName);
				if (texIt != textureCache.end() && !texIt->second.expired()) {
					tex = texIt->second.lock();
				}
				else {
					tex = renderer.GetTexture(sprite.atlasName);
					textureCache[sprite.atlasName] = tex; // Cache even if null
				}

				if (tex) {
					// Single texture/spritesheet - use full texture
					rc.textureID = tex->GetID();
					rc.uvOffset = { 0,0 };
					rc.uvSize = { 1,1 };
					rc.worldSize = { defaultTileSize, defaultTileSize };
				}
				else {
					// Texture not found
					spdlog::warn("[SpriteUpdate] Spritesheet '{}' not found for static sprite", sprite.atlasName);
					rc.textureID = 0;
					rc.uvOffset = { 0,0 };
					rc.uvSize = { 1,1 };
					rc.worldSize = { defaultTileSize, defaultTileSize };
				}
			}

			// Flip horizontally if facing left
			if (const auto* facing = registry.try_get<FacingComponent>(entity);
				facing && facing->facing == Facing::Left) {
				rc.worldSize.x = -rc.worldSize.x;
			}

			registry.emplace_or_replace<SpriteRenderComponent>(entity, std::move(rc));
		}
	}

} // namespace WanderSpire