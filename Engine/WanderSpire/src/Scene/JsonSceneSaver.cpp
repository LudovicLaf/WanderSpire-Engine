#include "WanderSpire/Scene/JsonSceneSaver.h"
#include "WanderSpire/ECS/Serialization.h"
#include "WanderSpire/ECS/SerializableComponents.h"
#include "WanderSpire/Components/AllComponents.h"
#include "WanderSpire/Components/ScriptDataComponent.h"
#include "WanderSpire/Core/Reflection.h"
#include <fstream>
#include <spdlog/spdlog.h>
#include <filesystem>

namespace WanderSpire::Scene {

	SceneSaveResult JsonSceneSaver::SaveScene(const std::string& filePath,
		const entt::registry& registry,
		const SceneMetadata& metadata) {

		try {
			SaveContext context{ &registry, {}, {}, 0 };

			GatherEntities(context);
			SaveMetadata(metadata, context);
			SaveEntities(context);

			std::filesystem::create_directories(std::filesystem::path(filePath).parent_path());
			std::ofstream file(filePath);
			if (!file.is_open()) {
				return { false, "Failed to open file for writing" };
			}

			file << context.sceneJson.dump(4);
			return { true, "", context.entitiesSaved };

		}
		catch (const std::exception& e) {
			return { false, "Save failed: " + std::string(e.what()) };
		}
	}

	bool JsonSceneSaver::SupportsFormat(const std::string& extension) const {
		return extension == ".json";
	}

	void JsonSceneSaver::GatherEntities(SaveContext& context) {
		std::unordered_set<entt::entity> entities;

		// Collect from reflection system
		for (const auto& [name, typeInfo] : Reflect::TypeRegistry::Get().GetNameMap()) {
			if (typeInfo.collectFn) {
				typeInfo.collectFn(*context.registry, entities);
			}
		}

		// Include script data entities
		for (auto entity : context.registry->view<ScriptDataComponent>()) {
			entities.insert(entity);
		}

		context.entitiesToSave.assign(entities.begin(), entities.end());
		std::sort(context.entitiesToSave.begin(), context.entitiesToSave.end(),
			[](entt::entity a, entt::entity b) {
				return entt::to_integral(a) < entt::to_integral(b);
			});
	}

	void JsonSceneSaver::SaveMetadata(const SceneMetadata& metadata, SaveContext& context) {
		context.sceneJson["metadata"] = {
			{"name", metadata.name},
			{"version", metadata.version},
			{"author", metadata.author},
			{"description", metadata.description},
			{"tags", metadata.tags},
			{"lastModified", std::time(nullptr)},
			{"worldMin", {metadata.worldMin.x, metadata.worldMin.y}},
			{"worldMax", {metadata.worldMax.x, metadata.worldMax.y}}
		};
	}

	void JsonSceneSaver::SaveEntities(SaveContext& context) {
		context.sceneJson["entities"] = nlohmann::json::array();

		for (auto entity : context.entitiesToSave) {
			nlohmann::json entityJson = SerializeEntity(entity, *context.registry);
			if (!entityJson.empty()) {
				context.sceneJson["entities"].push_back(entityJson);
				context.entitiesSaved++;
			}
		}
	}

	nlohmann::json JsonSceneSaver::SerializeEntity(entt::entity entity, const entt::registry& registry) {
		if (!registry.valid(entity)) {
			return {};
		}

		nlohmann::json entityJson;
		entityJson["id"] = entt::to_integral(entity);
		entityJson["components"] = nlohmann::json::object();

		// Handle TilemapChunkComponent specially
		if (auto* chunk = registry.try_get<TilemapChunkComponent>(entity)) {
			nlohmann::json chunkJson;
			to_json(chunkJson, *chunk);
			entityJson["components"]["TilemapChunkComponent"] = chunkJson;
		}

		SaveReflectedComponents(entity, registry, entityJson);

		// Handle script data
		if (auto* scriptData = registry.try_get<ScriptDataComponent>(entity)) {
			try {
				nlohmann::json managedComponents = nlohmann::json::parse(scriptData->data);
				for (auto& [key, value] : managedComponents.items()) {
					if (key != "TilemapChunkComponent") {
						entityJson["components"][key] = value;
					}
				}
			}
			catch (...) {
				// Skip malformed script data
			}
		}

		return entityJson;
	}

	void JsonSceneSaver::SaveReflectedComponents(entt::entity entity, const entt::registry& registry,
		nlohmann::json& entityJson) {

		// Special handling for AnimationClipsComponent (before reflection loop)
		if (auto* animClips = registry.try_get<AnimationClipsComponent>(entity)) {
			entityJson["components"]["AnimationClipsComponent"] = animClips->ToJson();
		}

		for (const auto& [typeName, typeInfo] : Reflect::TypeRegistry::Get().GetNameMap()) {
			if (typeName.find("TilemapChunkComponent") != std::string::npos) {
				continue; // Skip - handled above
			}

			// Skip AnimationClipsComponent since we handled it specially above
			if (typeName == "AnimationClipsComponent") {
				continue;
			}

			if (typeInfo.saveFn) {
				nlohmann::json componentJson;
				typeInfo.saveFn(registry, entity, componentJson);

				if (componentJson.contains(typeName) &&
					!entityJson["components"].contains(typeName)) {
					entityJson["components"][typeName] = componentJson[typeName];
				}
			}
		}
	}

} // namespace WanderSpire::Scene