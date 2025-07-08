// include/WanderSpire/Graphics/OpenGLDebug.h
#pragma once

#include <glad/glad.h>
#include <string>

namespace WanderSpire {

	/// OpenGL debugging utilities for better error detection
	class OpenGLDebug {
	public:
		/// Check for OpenGL errors and log them
		static bool CheckError(const char* operation = nullptr);

		/// Enable OpenGL debug context if available
		static void EnableDebugContext();

		/// Set up debug message callback
		static void SetupDebugCallback();

		/// Get OpenGL error string
		static std::string GetErrorString(GLenum error);

		/// RAII wrapper for OpenGL state debugging
		class StateGuard {
		public:
			explicit StateGuard(const char* operation);
			~StateGuard();
		private:
			const char* m_Operation;
		};
	};

} // namespace WanderSpire

// Debug macros (only active in debug builds)
#ifdef _DEBUG
#define GL_CHECK(call) \
        do { \
            call; \
            WanderSpire::OpenGLDebug::CheckError(#call); \
        } while(0)

#define GL_STATE_GUARD(op) \
        WanderSpire::OpenGLDebug::StateGuard _guard(op)
#else
#define GL_CHECK(call) call
#define GL_STATE_GUARD(op)
#endif