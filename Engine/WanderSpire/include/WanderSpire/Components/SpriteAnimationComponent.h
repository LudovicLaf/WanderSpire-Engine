// WanderSpire/Components/SpriteAnimationComponent.h
#pragma once

#include <memory>
#include "WanderSpire/Graphics/Texture.h"
#include "WanderSpire/Core/ReflectionMacros.h"

namespace WanderSpire {

	struct SpriteAnimationComponent {
		int   currentFrame = 0;
		float elapsedTime = 0.0f;
		bool  finished = false;

		int   startFrame;
		int   frameCount;
		float frameDuration;
		bool  loop;

		int   frameWidth;   // px
		int   frameHeight;  // px
		int   columns;
		int   rows;

		// desired size in world units
		float worldWidth = 1.0f;
		float worldHeight = 1.0f;

		std::shared_ptr<Texture> texture;

		SpriteAnimationComponent() = default;

		SpriteAnimationComponent(int startFrame,
			int frameCount,
			float frameDuration,
			float elapsedTime,
			int frameWidth,
			int frameHeight,
			std::shared_ptr<Texture> texture,
			float worldWidth,
			float worldHeight,
			bool loop = true)
			: currentFrame(0)
			, elapsedTime(elapsedTime)
			, finished(false)
			, startFrame(startFrame)
			, frameCount(frameCount)
			, frameDuration(frameDuration)
			, loop(loop)
			, frameWidth(frameWidth)
			, frameHeight(frameHeight)
			, worldWidth(worldWidth)
			, worldHeight(worldHeight)
			, texture(std::move(texture))
		{
			if (this->texture) {
				columns = this->texture->GetWidth() / frameWidth;
				rows = this->texture->GetHeight() / frameHeight;
			}
			else {
				columns = rows = 1;
			}
		}

		void Reset() {
			currentFrame = 0;
			elapsedTime = 0.0f;
			finished = false;
		}
	};

} // namespace WanderSpire

REFLECTABLE(WanderSpire::SpriteAnimationComponent,
	FIELD(Int, currentFrame, 0, 1000, 1),
	FIELD(Float, elapsedTime, 0, 10, 0.01f),
	FIELD(Bool, finished, 0, 1, 1),
	FIELD(Int, startFrame, 0, 1000, 1),
	FIELD(Int, frameCount, 1, 1000, 1),
	FIELD(Float, frameDuration, 0, 10, 0.01f),
	FIELD(Bool, loop, 0, 1, 1),
	FIELD(Int, frameWidth, 1, 4096, 1),
	FIELD(Int, frameHeight, 1, 4096, 1),
	FIELD(Int, columns, 1, 4096, 1),
	FIELD(Int, rows, 1, 4096, 1),
	FIELD(Float, worldWidth, 0, 1000, 0.1f),
	FIELD(Float, worldHeight, 0, 1000, 0.1f)
)
