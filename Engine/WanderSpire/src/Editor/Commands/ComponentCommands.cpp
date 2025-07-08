#include "WanderSpire/Editor/Commands/ComponentCommands.h"
#include "WanderSpire/Core/Reflection.h"
#include "WanderSpire/ECS/Serialization.h"
#include <spdlog/spdlog.h>
#include <chrono>

namespace WanderSpire {

	// --- AddComponentCommand ---
	AddComponentCommand::AddComponentCommand(entt::registry& registry, entt::entity entity,
		const std::string& componentType, const nlohmann::json& componentData)
		: registry(&registry), entity(entity), componentType(componentType), componentData(componentData)
	{
		auto& typeRegistry = Reflect::TypeRegistry::Get();
		auto it = typeRegistry.GetNameMap().find(componentType);
		if (it != typeRegistry.GetNameMap().end() && it->second.saveFn) {
			nlohmann::json existingData;
			it->second.saveFn(registry, entity, existingData); // << Use reference!
			if (existingData.contains(componentType)) {
				previousComponentData = existingData[componentType];
				componentExisted = true;
			}
		}
	}

	void AddComponentCommand::Execute() {
		auto& typeRegistry = Reflect::TypeRegistry::Get();
		auto it = typeRegistry.GetNameMap().find(componentType);
		if (it != typeRegistry.GetNameMap().end() && it->second.loadFn) {
			nlohmann::json wrapper;
			wrapper[componentType] = componentData;
			it->second.loadFn(*registry, entity, wrapper); // << Pointer, so dereference
		}
	}

	void AddComponentCommand::Undo() {
		if (componentExisted) {
			auto& typeRegistry = Reflect::TypeRegistry::Get();
			auto it = typeRegistry.GetNameMap().find(componentType);
			if (it != typeRegistry.GetNameMap().end() && it->second.loadFn) {
				nlohmann::json wrapper;
				wrapper[componentType] = previousComponentData;
				it->second.loadFn(*registry, entity, wrapper); // << Pointer, so dereference
			}
		}
		else {
			spdlog::warn("[AddComponentCommand] Component removal not fully implemented for: {}", componentType);
		}
	}

	std::string AddComponentCommand::GetDescription() const {
		return "Add " + componentType + " component";
	}

	// --- RemoveComponentCommand ---
	RemoveComponentCommand::RemoveComponentCommand(entt::registry& registry, entt::entity entity, const std::string& componentType)
		: registry(&registry), entity(entity), componentType(componentType)
	{
		auto& typeRegistry = Reflect::TypeRegistry::Get();
		auto it = typeRegistry.GetNameMap().find(componentType);
		if (it != typeRegistry.GetNameMap().end() && it->second.saveFn) {
			it->second.saveFn(registry, entity, savedComponentData); // << Use reference!
		}
	}

	void RemoveComponentCommand::Execute() {
		spdlog::warn("[RemoveComponentCommand] Component removal not fully implemented for: {}", componentType);
	}

	void RemoveComponentCommand::Undo() {
		if (savedComponentData.contains(componentType)) {
			auto& typeRegistry = Reflect::TypeRegistry::Get();
			auto it = typeRegistry.GetNameMap().find(componentType);
			if (it != typeRegistry.GetNameMap().end() && it->second.loadFn) {
				it->second.loadFn(*registry, entity, savedComponentData); // << Pointer, so dereference
			}
		}
	}

	std::string RemoveComponentCommand::GetDescription() const {
		return "Remove " + componentType + " component";
	}

	// --- ModifyComponentCommand ---
	ModifyComponentCommand::ModifyComponentCommand(entt::registry& registry, entt::entity entity,
		const std::string& componentType, const std::string& fieldName,
		const nlohmann::json& oldValue, const nlohmann::json& newValue)
		: registry(&registry), entity(entity), componentType(componentType), fieldName(fieldName),
		oldValue(oldValue), newValue(newValue), commandTime(std::chrono::steady_clock::now()) {
	}

	void ModifyComponentCommand::Execute() {
		spdlog::debug("[ModifyComponentCommand] Setting {}.{} to new value", componentType, fieldName);
		// TODO: Actually set the field value using reflection
	}

	void ModifyComponentCommand::Undo() {
		spdlog::debug("[ModifyComponentCommand] Restoring {}.{} to old value", componentType, fieldName);
		// TODO: Actually restore the field value using reflection
	}

	std::string ModifyComponentCommand::GetDescription() const {
		return "Modify " + componentType + "." + fieldName;
	}

	bool ModifyComponentCommand::CanMerge(const ICommand* other) const {
		auto* otherModify = dynamic_cast<const ModifyComponentCommand*>(other);
		if (!otherModify) return false;
		if (otherModify->entity != entity ||
			otherModify->componentType != componentType ||
			otherModify->fieldName != fieldName) return false;
		auto timeDiff = std::chrono::duration_cast<std::chrono::milliseconds>(
			otherModify->commandTime - commandTime).count();
		return timeDiff < 1000;
	}

	void ModifyComponentCommand::MergeWith(const ICommand* other) {
		auto* otherModify = static_cast<const ModifyComponentCommand*>(other);
		newValue = otherModify->newValue;
		commandTime = otherModify->commandTime;
	}

} // namespace WanderSpire
