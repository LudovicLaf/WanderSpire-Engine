#include "WanderSpire/Core/GLObjects.h"
#include <spdlog/spdlog.h>

namespace WanderSpire {

	GLObjects::GLObjects() {
		glGenVertexArrays(1, &VAO);
		glGenBuffers(1, &VBO);
		glGenBuffers(1, &EBO);
		spdlog::info("[GLObjects] Generated VAO={}, VBO={}, EBO={}", VAO, VBO, EBO);
	}

	GLObjects::~GLObjects() {
		if (EBO) {
			glDeleteBuffers(1, &EBO);
			spdlog::info("[GLObjects] Deleted EBO={}", EBO);
		}
		if (VBO) {
			glDeleteBuffers(1, &VBO);
			spdlog::info("[GLObjects] Deleted VBO={}", VBO);
		}
		if (VAO) {
			glDeleteVertexArrays(1, &VAO);
			spdlog::info("[GLObjects] Deleted VAO={}", VAO);
		}
	}

}
