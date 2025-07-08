#pragma once
#include "WanderSpire/Editor/ICommand.h"
#include <glm/glm.hpp>
#include <entt/entt.hpp>
#include <string>
#include <vector>
#include <chrono>

namespace WanderSpire {

	class TransformCommand : public ICommand {
	public:
		TransformCommand(entt::registry& registry, entt::entity entity,
			const glm::vec2& newPos, const glm::vec2& newScale, float newRotation);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
		bool CanMerge(const ICommand* other) const override;
		void MergeWith(const ICommand* other) override;

	private:
		entt::registry* registry;
		entt::entity entity;
		std::string entityName;
		glm::vec2 oldPosition, newPosition;
		glm::vec2 oldScale, newScale;
		float oldRotation, newRotation;
		void ApplyTransform(const glm::vec2& pos, const glm::vec2& scale, float rotation);
	};

	class MoveCommand : public ICommand {
	public:
		MoveCommand(entt::registry& registry, const std::vector<entt::entity>& entities, const glm::vec2& delta);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
		bool CanMerge(const ICommand* other) const override;
		void MergeWith(const ICommand* other) override;
	private:
		entt::registry* registry;
		std::vector<entt::entity> entities;
		glm::vec2 totalDelta;
		std::chrono::steady_clock::time_point commandTime;
		static constexpr float MERGE_TIME_THRESHOLD = 0.5f;
	};

} // namespace WanderSpire
