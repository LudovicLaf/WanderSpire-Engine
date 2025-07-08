#include "WanderSpire/Graphics/Camera2D.h"
#include <glm/gtc/matrix_transform.hpp>

namespace WanderSpire {

	Camera2D::Camera2D(float width, float height)
		: m_Position(0.0f)
		, m_Zoom(1.0f)
		, m_TargetZoom(1.0f)
		, m_Width(width)
		, m_Height(height)
	{
		RecalculateViewProjection();
	}

	void Camera2D::SetPosition(const glm::vec2& position) {
		m_Position = position;
		RecalculateViewProjection();
	}

	void Camera2D::Move(const glm::vec2& delta) {
		m_Position += delta;
		RecalculateViewProjection();
	}

	void Camera2D::SetZoom(float zoom) {
		m_Zoom = std::clamp(zoom, 0.1f, 10.0f);
		m_TargetZoom = m_Zoom;
		RecalculateViewProjection();
	}

	void Camera2D::AddZoom(float delta) {
		m_TargetZoom = std::clamp(m_TargetZoom + delta, 0.1f, 10.0f);
	}

	void Camera2D::Update(float deltaTime) {
		float diff = m_TargetZoom - m_Zoom;
		if (std::abs(diff) > 0.001f) {
			float step = std::clamp(m_ZoomLerpSpeed * deltaTime, 0.0f, 1.0f);
			m_Zoom += diff * step;
			RecalculateViewProjection();
		}
	}

	const glm::mat4& Camera2D::GetViewProjectionMatrix() const {
		return m_ViewProjectionMatrix;
	}

	void Camera2D::RecalculateViewProjection() {
		// orthographic projection that maps [-(w/2)/zoom, +(w/2)/zoom] to clip space
		glm::mat4 projection = glm::ortho(
			-m_Width * 0.5f / m_Zoom,
			m_Width * 0.5f / m_Zoom,
			m_Height * 0.5f / m_Zoom,
			-m_Height * 0.5f / m_Zoom,
			-1.0f, 1.0f
		);
		glm::mat4 view = glm::translate(glm::mat4(1.0f), glm::vec3(-m_Position, 0.0f));
		m_ViewProjectionMatrix = projection * view;
	}

}
