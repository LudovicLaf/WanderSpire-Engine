#pragma once

#include <glad/glad.h>

namespace WanderSpire {

	/// RAII for a VAO + VBO + EBO trio
	struct GLObjects {
		GLuint VAO = 0, VBO = 0, EBO = 0;
		GLObjects();
		~GLObjects();
	};

}
