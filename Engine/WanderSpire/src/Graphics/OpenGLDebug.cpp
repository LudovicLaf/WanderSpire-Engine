#include "WanderSpire/Graphics/OpenGLDebug.h"
#include <spdlog/spdlog.h>
#include <cstring>

namespace WanderSpire {

	bool OpenGLDebug::CheckError(const char* operation) {
		GLenum error = glGetError();
		if (error != GL_NO_ERROR) {
			std::string errorStr = GetErrorString(error);
			if (operation) {
				spdlog::error("[OpenGL] Error after '{}': {} (0x{:X})", operation, errorStr, error);
			}
			else {
				spdlog::error("[OpenGL] Error: {} (0x{:X})", errorStr, error);
			}
			return false;
		}
		return true;
	}

	void OpenGLDebug::EnableDebugContext() {
		// Check if debug extensions are available
		bool hasDebugContext = false;

		if (GLAD_GL_VERSION_4_3) {
			hasDebugContext = true;
		}
		else {
			// Check for KHR_debug extension string
			GLint numExtensions;
			glGetIntegerv(GL_NUM_EXTENSIONS, &numExtensions);
			for (int i = 0; i < numExtensions; ++i) {
				const char* extension = reinterpret_cast<const char*>(glGetStringi(GL_EXTENSIONS, i));
				if (extension && strcmp(extension, "GL_KHR_debug") == 0) {
					hasDebugContext = true;
					break;
				}
			}
		}

		if (hasDebugContext) {
			glEnable(GL_DEBUG_OUTPUT);
			glEnable(GL_DEBUG_OUTPUT_SYNCHRONOUS);
			SetupDebugCallback();
			spdlog::info("[OpenGL] Debug context enabled");
		}
		else {
			spdlog::warn("[OpenGL] Debug context not available");
		}
	}

	void OpenGLDebug::SetupDebugCallback() {
		// Check if glDebugMessageCallback is available
		if (glDebugMessageCallback) {
			glDebugMessageCallback([](GLenum source, GLenum type, GLuint id, GLenum severity,
				GLsizei length, const GLchar* message, const void* userParam) {

					// Filter out low-priority messages
					if (severity == GL_DEBUG_SEVERITY_NOTIFICATION) return;

					std::string sourceStr, typeStr, severityStr;

					switch (source) {
					case GL_DEBUG_SOURCE_API: sourceStr = "API"; break;
					case GL_DEBUG_SOURCE_WINDOW_SYSTEM: sourceStr = "Window System"; break;
					case GL_DEBUG_SOURCE_SHADER_COMPILER: sourceStr = "Shader Compiler"; break;
					case GL_DEBUG_SOURCE_THIRD_PARTY: sourceStr = "Third Party"; break;
					case GL_DEBUG_SOURCE_APPLICATION: sourceStr = "Application"; break;
					case GL_DEBUG_SOURCE_OTHER: sourceStr = "Other"; break;
					default: sourceStr = "Unknown"; break;
					}

					switch (type) {
					case GL_DEBUG_TYPE_ERROR: typeStr = "Error"; break;
					case GL_DEBUG_TYPE_DEPRECATED_BEHAVIOR: typeStr = "Deprecated"; break;
					case GL_DEBUG_TYPE_UNDEFINED_BEHAVIOR: typeStr = "Undefined Behavior"; break;
					case GL_DEBUG_TYPE_PORTABILITY: typeStr = "Portability"; break;
					case GL_DEBUG_TYPE_PERFORMANCE: typeStr = "Performance"; break;
					case GL_DEBUG_TYPE_MARKER: typeStr = "Marker"; break;
					case GL_DEBUG_TYPE_OTHER: typeStr = "Other"; break;
					default: typeStr = "Unknown"; break;
					}

					switch (severity) {
					case GL_DEBUG_SEVERITY_HIGH:
						spdlog::error("[OpenGL Debug] {} {} ({}): {}", sourceStr, typeStr, id, message);
						break;
					case GL_DEBUG_SEVERITY_MEDIUM:
						spdlog::warn("[OpenGL Debug] {} {} ({}): {}", sourceStr, typeStr, id, message);
						break;
					case GL_DEBUG_SEVERITY_LOW:
						spdlog::info("[OpenGL Debug] {} {} ({}): {}", sourceStr, typeStr, id, message);
						break;
					default:
						spdlog::debug("[OpenGL Debug] {} {} ({}): {}", sourceStr, typeStr, id, message);
						break;
					}
				}, nullptr);
		}
		else {
			spdlog::warn("[OpenGL] glDebugMessageCallback not available");
		}

	}

	std::string OpenGLDebug::GetErrorString(GLenum error) {
		switch (error) {
		case GL_NO_ERROR: return "No error";
		case GL_INVALID_ENUM: return "Invalid enum";
		case GL_INVALID_VALUE: return "Invalid value";
		case GL_INVALID_OPERATION: return "Invalid operation";
		case GL_OUT_OF_MEMORY: return "Out of memory";
		case GL_INVALID_FRAMEBUFFER_OPERATION: return "Invalid framebuffer operation";
		default: return "Unknown error";
		}
	}

	// StateGuard implementation
	OpenGLDebug::StateGuard::StateGuard(const char* operation) : m_Operation(operation) {
		CheckError((std::string("Before ") + operation).c_str());
	}

	OpenGLDebug::StateGuard::~StateGuard() {
		CheckError((std::string("After ") + m_Operation).c_str());
	}

} // namespace WanderSpire