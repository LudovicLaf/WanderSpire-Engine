#pragma once

#include <glad/glad.h>

namespace WanderSpire {

	/// RAII wrappers for OpenGL state management
	namespace GL {

		/// RAII VAO binding
		class VertexArrayBinder {
		public:
			explicit VertexArrayBinder(GLuint vao) : m_PrevVAO(GetCurrentVAO()) {
				if (vao != m_PrevVAO) {
					glBindVertexArray(vao);
				}
			}

			~VertexArrayBinder() {
				GLuint current = GetCurrentVAO();
				if (current != m_PrevVAO) {
					glBindVertexArray(m_PrevVAO);
				}
			}

		private:
			GLuint m_PrevVAO;

			static GLuint GetCurrentVAO() {
				GLint vao;
				glGetIntegerv(GL_VERTEX_ARRAY_BINDING, &vao);
				return static_cast<GLuint>(vao);
			}
		};

		/// RAII texture binding
		class TextureBinder {
		public:
			explicit TextureBinder(GLuint texture, GLenum target = GL_TEXTURE_2D, GLuint unit = 0)
				: m_Target(target), m_Unit(unit), m_PrevTexture(GetCurrentTexture(target)) {
				if (unit > 0) glActiveTexture(GL_TEXTURE0 + unit);
				if (texture != m_PrevTexture) {
					glBindTexture(target, texture);
				}
			}

			~TextureBinder() {
				GLuint current = GetCurrentTexture(m_Target);
				if (current != m_PrevTexture) {
					glBindTexture(m_Target, m_PrevTexture);
				}
			}

		private:
			GLenum m_Target;
			GLuint m_Unit;
			GLuint m_PrevTexture;

			static GLuint GetCurrentTexture(GLenum target) {
				GLint tex;
				GLenum pname = (target == GL_TEXTURE_2D) ? GL_TEXTURE_BINDING_2D : GL_TEXTURE_BINDING_2D;
				glGetIntegerv(pname, &tex);
				return static_cast<GLuint>(tex);
			}
		};

		/// RAII buffer binding
		class BufferBinder {
		public:
			explicit BufferBinder(GLuint buffer, GLenum target)
				: m_Target(target), m_PrevBuffer(GetCurrentBuffer(target)) {
				if (buffer != m_PrevBuffer) {
					glBindBuffer(target, buffer);
				}
			}

			~BufferBinder() {
				GLuint current = GetCurrentBuffer(m_Target);
				if (current != m_PrevBuffer) {
					glBindBuffer(m_Target, m_PrevBuffer);
				}
			}

		private:
			GLenum m_Target;
			GLuint m_PrevBuffer;

			static GLuint GetCurrentBuffer(GLenum target) {
				GLint buffer;
				GLenum pname;
				switch (target) {
				case GL_ARRAY_BUFFER: pname = GL_ARRAY_BUFFER_BINDING; break;
				case GL_ELEMENT_ARRAY_BUFFER: pname = GL_ELEMENT_ARRAY_BUFFER_BINDING; break;
				default: pname = GL_ARRAY_BUFFER_BINDING; break;
				}
				glGetIntegerv(pname, &buffer);
				return static_cast<GLuint>(buffer);
			}
		};

		/// RAII shader program binding
		class ProgramBinder {
		public:
			explicit ProgramBinder(GLuint program) : m_PrevProgram(GetCurrentProgram()) {
				if (program != m_PrevProgram) {
					glUseProgram(program);
				}
			}

			~ProgramBinder() {
				GLuint current = GetCurrentProgram();
				if (current != m_PrevProgram) {
					glUseProgram(m_PrevProgram);
				}
			}

		private:
			GLuint m_PrevProgram;

			static GLuint GetCurrentProgram() {
				GLint program;
				glGetIntegerv(GL_CURRENT_PROGRAM, &program);
				return static_cast<GLuint>(program);
			}
		};

	} // namespace GL

} // namespace WanderSpire