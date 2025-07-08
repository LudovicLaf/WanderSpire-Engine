#pragma once

#include <glm/glm.hpp>
#include <glm/gtc/quaternion.hpp>
#include <algorithm>

namespace WanderSpire {

	class Camera2D {
	public:
		Camera2D(float width, float height);

		// Position
		void SetPosition(const glm::vec2& position);
		void Move(const glm::vec2& delta);

		// Instant zoom
		void SetZoom(float zoom);

		// Smooth zoom target
		void AddZoom(float delta);
		void Update(float deltaTime);

		/// Change the camera's nominal screen size (e.g. on window resize)
		void SetScreenSize(float width, float height) {
			m_Width = width;
			m_Height = height;
			RecalculateViewProjection();
		}

		// Accessors
		float GetZoom()       const { return m_Zoom; }
		float GetWidth()      const { return m_Width; }
		float GetHeight()     const { return m_Height; }
		const glm::vec2& GetPosition() const { return m_Position; }
		const glm::mat4& GetViewProjectionMatrix() const;

	private:
		void RecalculateViewProjection();

		glm::vec2 m_Position;
		float     m_Zoom;
		float     m_TargetZoom;
		float     m_ZoomLerpSpeed = 8.0f;  // units per second

		float     m_Width;
		float     m_Height;
		glm::mat4 m_ViewProjectionMatrix;
	};

}
