// src/Graphics/Shader.cpp
#include "WanderSpire/Graphics/Shader.h"
#include "WanderSpire/Core/AssetManager.h"

namespace WanderSpire {

	Shader::Shader() = default;

	Shader::Shader(const std::string& vertexPath, const std::string& fragmentPath) {
		auto vsResult = AssetManager::LoadTextFile(vertexPath);
		auto fsResult = AssetManager::LoadTextFile(fragmentPath);

		if (!vsResult) {
			spdlog::error("[Shader] Failed to load vertex shader '{}': {}", vertexPath, vsResult.error);
			return;
		}

		if (!fsResult) {
			spdlog::error("[Shader] Failed to load fragment shader '{}': {}", fragmentPath, fsResult.error);
			return;
		}

		CompileFromSource(vsResult.content, fsResult.content);
	}

	Shader::~Shader() {
		if (m_ProgramID) {
			glDeleteProgram(m_ProgramID);
			spdlog::info("[Shader] Deleted program {}", m_ProgramID);
		}
	}

	void Shader::CompileFromSource(const std::string& vsSource, const std::string& fsSource) {
		if (m_ProgramID) {
			glDeleteProgram(m_ProgramID);
			m_UniformLocationCache.clear();
		}

		GLuint vs = CompileShader(GL_VERTEX_SHADER, vsSource);
		GLuint fs = CompileShader(GL_FRAGMENT_SHADER, fsSource);
		if (!vs || !fs) {
			spdlog::error("[Shader] Skipping link due to compile errors");
			if (vs) glDeleteShader(vs);
			if (fs) glDeleteShader(fs);
			return;
		}

		m_ProgramID = glCreateProgram();
		glAttachShader(m_ProgramID, vs);
		glAttachShader(m_ProgramID, fs);
		glLinkProgram(m_ProgramID);

		GLint success;
		glGetProgramiv(m_ProgramID, GL_LINK_STATUS, &success);
		if (!success) {
			char buf[512];
			glGetProgramInfoLog(m_ProgramID, 512, nullptr, buf);
			spdlog::error("[Shader] Link Error: {}", buf);
			glDeleteProgram(m_ProgramID);
			m_ProgramID = 0;
		}

		glDeleteShader(vs);
		glDeleteShader(fs);

		if (m_ProgramID)
			spdlog::info("[Shader] Linked program {}", m_ProgramID);
	}

	GLuint Shader::CompileShader(GLenum type, const std::string& src) {
		GLuint sh = glCreateShader(type);
		const char* c = src.c_str();
		glShaderSource(sh, 1, &c, nullptr);
		glCompileShader(sh);

		GLint ok;
		glGetShaderiv(sh, GL_COMPILE_STATUS, &ok);
		if (!ok) {
			char buf[512];
			glGetShaderInfoLog(sh, 512, nullptr, buf);
			spdlog::error("[Shader] Compile Error (type={}): {}", type, buf);
			glDeleteShader(sh);
			return 0;
		}
		return sh;
	}

	void Shader::Bind() const {
		if (m_ProgramID == 0) {
			spdlog::warn("[Shader] Bind() called on invalid program");
			return;
		}
		if (s_CurrentProgramID != m_ProgramID) {
			glUseProgram(m_ProgramID);
			s_CurrentProgramID = m_ProgramID;
		}
	}

	void Shader::Unbind() const {
		glUseProgram(0);
		s_CurrentProgramID = 0;
	}

	GLint Shader::GetUniformLocation(const std::string& name) {
		if (auto it = m_UniformLocationCache.find(name); it != m_UniformLocationCache.end())
			return it->second;
		GLint loc = glGetUniformLocation(m_ProgramID, name.c_str());
		if (loc == -1) spdlog::warn("[Shader] Uniform '{}' not found.", name);
		m_UniformLocationCache[name] = loc;
		return loc;
	}

	void Shader::SetUniformInt(const std::string& name, int val) { glUniform1i(GetUniformLocation(name), val); }
	void Shader::SetUniformFloat(const std::string& name, float v) { glUniform1f(GetUniformLocation(name), v); }
	void Shader::SetUniformVec2(const std::string& name, const glm::vec2& v) {
		glUniform2f(GetUniformLocation(name), v.x, v.y);
	}
	void Shader::SetUniformVec3(const std::string& name, const glm::vec3& v) {
		glUniform3f(GetUniformLocation(name), v.x, v.y, v.z);
	}
	void Shader::SetUniformMat4(const std::string& name, const glm::mat4& m) {
		glUniformMatrix4fv(GetUniformLocation(name), 1, GL_FALSE, &m[0][0]);
	}

}