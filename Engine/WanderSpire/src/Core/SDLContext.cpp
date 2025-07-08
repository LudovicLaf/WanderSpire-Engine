#include "WanderSpire/Core/SDLContext.h"
#include <spdlog/spdlog.h>
#include <glad/glad.h>

namespace WanderSpire {

	SDLContext::SDLContext(int width, int height, const std::string& title, bool resizable)
	{
		// request double buffering
		SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 1);

		// build window flags
		SDL_WindowFlags flags = static_cast<SDL_WindowFlags>(SDL_WINDOW_OPENGL);
		if (resizable) {
			flags = static_cast<SDL_WindowFlags>(flags | SDL_WINDOW_RESIZABLE);
		}

		// correct SDL3 signature: title, w, h, flags
		m_Window = SDL_CreateWindow(
			title.c_str(),
			width,
			height,
			flags
		);
		if (!m_Window) {
			spdlog::error("[SDLContext] SDL_CreateWindow failed: {}", SDL_GetError());
			return;
		}

		// create GL context
		m_Context = SDL_GL_CreateContext(m_Window);
		if (!m_Context) {
			spdlog::error("[SDLContext] SDL_GL_CreateContext failed: {}", SDL_GetError());
			return;
		}

		// load GL entry points via GLAD
		if (!gladLoadGLLoader((GLADloadproc)SDL_GL_GetProcAddress)) {
			spdlog::error("[SDLContext] Failed to initialize GLAD");
			return;
		}

		// vsync on
		if (SDL_GL_SetSwapInterval(1) < 0) {
			spdlog::warn("[SDLContext] VSync unavailable: {}", SDL_GetError());
		}

		// set initial viewport
		glViewport(0, 0, width, height);

		// clear color
		glClearColor(0.2f, 0.3f, 0.3f, 1.0f);

		// enable alpha blending
		glEnable(GL_BLEND);
		glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

		// disable depth test for 2D
		glDisable(GL_DEPTH_TEST);
	}

	SDLContext::~SDLContext()
	{
		if (m_Context) {
			SDL_GL_DestroyContext(m_Context);
			spdlog::info("[SDLContext] OpenGL context destroyed");
		}
		if (m_Window) {
			SDL_DestroyWindow(m_Window);
			spdlog::info("[SDLContext] SDL window destroyed");
		}
	}

}
