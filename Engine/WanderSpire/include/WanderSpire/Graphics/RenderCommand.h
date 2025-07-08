#pragma once

#include <glm/glm.hpp>
#include <glad/glad.h>
#include <functional>
#include <memory>

namespace WanderSpire {

	/// Standard render layers - developers can use custom values between these
	enum class RenderLayer : int {
		Background = -1000,    ///< Clear operations, skybox
		Terrain = 0,        ///< Ground tiles, background elements  
		Entities = 100,      ///< Game objects, sprites, characters
		Effects = 200,      ///< Particles, visual effects
		UI = 1000,     ///< User interface elements
		Debug = 2000,     ///< Debug overlays, gizmos
		PostProcess = 3000      ///< Screen effects, filters
	};

	/// Types of render operations
	enum class RenderCommandType {
		Clear,              ///< Clear screen/buffers
		DrawSprite,         ///< Single sprite/quad
		DrawInstanced,      ///< Instanced rendering (terrain)
		DrawCustom,         ///< Custom user callback
		BeginFrame,         ///< Setup frame state
		EndFrame            ///< Finalize frame
	};

	/// Base render command - all operations derive from this
	struct RenderCommand {
		RenderCommandType type;
		RenderLayer layer;
		int order;              ///< Sub-ordering within layer

		RenderCommand(RenderCommandType t, RenderLayer l, int o = 0)
			: type(t), layer(l), order(o) {
		}
		virtual ~RenderCommand() = default;
		virtual void Execute() = 0;
	};

	/// Clear screen command
	struct ClearCommand : public RenderCommand {
		glm::vec3 color;
		bool clearColor;
		bool clearDepth;

		ClearCommand(glm::vec3 col = { 0.2f, 0.3f, 0.3f }, bool color = true, bool depth = false)
			: RenderCommand(RenderCommandType::Clear, RenderLayer::Background, -1000)
			, color(col), clearColor(color), clearDepth(depth) {
		}

		void Execute() override {
			GLbitfield mask = 0;
			if (clearColor) {
				glClearColor(color.r, color.g, color.b, 1.0f);
				mask |= GL_COLOR_BUFFER_BIT;
			}
			if (clearDepth) {
				mask |= GL_DEPTH_BUFFER_BIT;
			}
			if (mask) glClear(mask);
		}
	};

	/// Single sprite rendering command
	struct SpriteCommand : public RenderCommand {
		GLuint textureID;
		glm::vec2 position;
		glm::vec2 size;
		float rotation;
		glm::vec3 color;
		glm::vec2 uvOffset;
		glm::vec2 uvSize;

		SpriteCommand(RenderLayer layer, int order = 0)
			: RenderCommand(RenderCommandType::DrawSprite, layer, order)
			, textureID(0), position(0), size(1), rotation(0)
			, color(1), uvOffset(0), uvSize(1) {
		}

		void Execute() override;
	};

	/// Instanced rendering command (for terrain)
	struct InstancedCommand : public RenderCommand {
		GLuint textureID;
		std::vector<glm::vec2> positions;
		std::vector<glm::vec4> uvRects;  // offset.xy, size.zw
		float tileSize;

		InstancedCommand(RenderLayer layer = RenderLayer::Terrain)
			: RenderCommand(RenderCommandType::DrawInstanced, layer)
			, textureID(0), tileSize(64.0f) {
		}

		void Execute() override;
	};

	/// Custom render callback command
	struct CustomCommand : public RenderCommand {
		std::function<void()> callback;

		CustomCommand(std::function<void()> cb, RenderLayer layer, int order = 0)
			: RenderCommand(RenderCommandType::DrawCustom, layer, order)
			, callback(std::move(cb)) {
		}

		void Execute() override {
			if (callback) callback();
		}
	};

	/// Frame setup command
	struct BeginFrameCommand : public RenderCommand {
		glm::mat4 viewProjection;

		BeginFrameCommand(const glm::mat4& vp)
			: RenderCommand(RenderCommandType::BeginFrame, RenderLayer::Background, -999)
			, viewProjection(vp) {
		}

		void Execute() override;
	};

	/// Frame finalization command  
	struct EndFrameCommand : public RenderCommand {
		EndFrameCommand()
			: RenderCommand(RenderCommandType::EndFrame, RenderLayer::PostProcess, 1000) {
		}

		void Execute() override;
	};

} // namespace WanderSpire