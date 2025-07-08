#pragma once

#include <string>
#include <unordered_map>
#include <glm/glm.hpp>
#include <glad/glad.h>
#include <spdlog/spdlog.h>

namespace WanderSpire {

	class Shader {
	public:
		/// Placeholder ctor
		Shader();

		/// Blocking ctor (legacy)
		Shader(const std::string& vertexPath, const std::string& fragmentPath);

		~Shader();

		void Bind() const;
		void Unbind() const;

		GLuint GetID() const { return m_ProgramID; }

		// Uniform setters
		void SetUniformInt(const std::string& name, int value);
		void SetUniformFloat(const std::string& name, float value);
		void SetUniformVec2(const std::string& name, const glm::vec2& v);
		void SetUniformVec3(const std::string& name, const glm::vec3& v);
		void SetUniformMat4(const std::string& name, const glm::mat4& m);

		/// Async/hot-reload entry: compile & link on main thread.
		void CompileFromSource(const std::string& vsSource, const std::string& fsSource);

	private:
		GLuint CompileShader(GLenum type, const std::string& src);
		GLint  GetUniformLocation(const std::string& name);

		GLuint m_ProgramID = 0;
		mutable std::unordered_map<std::string, GLint> m_UniformLocationCache;
		static inline GLuint s_CurrentProgramID = 0;
	};

}
