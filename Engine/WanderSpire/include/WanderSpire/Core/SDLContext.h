#pragma once

#include <SDL3/SDL.h>
#include <string>

namespace WanderSpire {

	/// RAII for SDL_Window + SDL_GLContext
	class SDLContext {
	public:
		/// @param width      initial window width
		/// @param height     initial window height
		/// @param title      window title
		/// @param resizable  allow user to resize the window (default true)
		SDLContext(int width, int height, const std::string& title, bool resizable = true);
		~SDLContext();

		SDL_Window* GetWindow()  const noexcept { return m_Window; }
		SDL_GLContext  GetContext() const noexcept { return m_Context; }

	private:
		SDL_Window* m_Window = nullptr;
		SDL_GLContext  m_Context = nullptr;
	};

}
