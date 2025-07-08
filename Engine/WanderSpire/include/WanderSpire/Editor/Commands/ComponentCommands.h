#pragma once
#include "WanderSpire/Editor/ICommand.h"
#include <entt/entt.hpp>
#include <nlohmann/json.hpp>
#include <string>

namespace WanderSpire {

	class AddComponentCommand : public ICommand {
	public:
		AddComponentCommand(entt::registry& registry, entt::entity entity,
			const std::string& componentType, const nlohmann::json& componentData = {});
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		entt::entity entity;
		std::string componentType;
		nlohmann::json componentData;
		nlohmann::json previousComponentData;
		bool componentExisted = false;
	};

	class RemoveComponentCommand : public ICommand {
	public:
		RemoveComponentCommand(entt::registry& registry, entt::entity entity, const std::string& componentType);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
	private:
		entt::registry* registry;
		entt::entity entity;
		std::string componentType;
		nlohmann::json savedComponentData;
	};

	class ModifyComponentCommand : public ICommand {
	public:
		ModifyComponentCommand(entt::registry& registry, entt::entity entity,
			const std::string& componentType, const std::string& fieldName,
			const nlohmann::json& oldValue, const nlohmann::json& newValue);
		void Execute() override;
		void Undo() override;
		std::string GetDescription() const override;
		bool CanMerge(const ICommand* other) const override;
		void MergeWith(const ICommand* other) override;
	private:
		entt::registry* registry;
		entt::entity entity;
		std::string componentType, fieldName;
		nlohmann::json oldValue, newValue;
		std::chrono::steady_clock::time_point commandTime;
	};

} // namespace WanderSpire
