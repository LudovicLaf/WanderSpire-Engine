#include "WanderSpire/Scene/JsonSceneLoader.h"
#include "WanderSpire/ECS/Serialization.h"
#include "WanderSpire/ECS/SerializableComponents.h"
#include "WanderSpire/Components/AllComponents.h"
#include "WanderSpire/Components/ScriptDataComponent.h"
#include "WanderSpire/Core/Reflection.h"
#include <fstream>
#include <spdlog/spdlog.h>

namespace WanderSpire::Scene {

	SceneLoadResult JsonSceneLoader::LoadScene(const std::string& filePath, entt::registry& registry) {
		SceneLoadResult result;

		try {
			nlohmann::json sceneJson;
			std::ifstream file(filePath);
			if (!file.is_open()) {
				result.error = "Failed to open file: " + filePath;
				return result;
			}

			file >> sceneJson;
			registry.clear();

			LoadContext context{ &registry, {}, {}, result };

			CreateEntities(sceneJson, context);
			LoadComponents(sceneJson, context);
			RestoreHierarchy(sceneJson, context);
			FindSpecialEntities(context);

			result.success = true;
			result.loadedEntities = std::move(context.loadedEntities);

		}
		catch (const std::exception& e) {
			result.error = "Scene loading failed: " + std::string(e.what());
			spdlog::error("[JsonSceneLoader] {}", result.error);
		}

		return result;
	}

	bool JsonSceneLoader::SupportsFormat(const std::string& extension) const {
		return extension == ".json";
	}

	void JsonSceneLoader::CreateEntities(const nlohmann::json& json, LoadContext& context) {
		if (!json.contains("entities")) return;

		for (const auto& entityJson : json["entities"]) {
			entt::entity entity = context.registry->create();
			context.loadedEntities.push_back(entity);

			if (entityJson.contains("id")) {
				context.idMapping[entityJson["id"].get<uint32_t>()] = entity;
			}
		}
	}

	void JsonSceneLoader::LoadComponents(const nlohmann::json& json, LoadContext& context) {
		if (!json.contains("entities")) return;

		size_t entityIndex = 0;
		for (const auto& entityJson : json["entities"]) {
			if (entityIndex < context.loadedEntities.size() && entityJson.contains("components")) {
				LoadEntityComponents(context.loadedEntities[entityIndex],
					entityJson["components"], *context.registry);
			}
			entityIndex++;
		}
	}

	void JsonSceneLoader::RestoreHierarchy(const nlohmann::json& json, LoadContext& context) {
		auto tilemapLayers = context.registry->view<TilemapLayerComponent, SceneNodeComponent>();
		auto tilemapChunks = context.registry->view<TilemapChunkComponent, SceneNodeComponent>();
		auto allNodes = context.registry->view<SceneNodeComponent>();

		// Find tilemaps and assign layers to them
		for (auto entity : allNodes) {
			auto& node = allNodes.get<SceneNodeComponent>(entity);

			if (node.name.find("Tilemap") != std::string::npos &&
				!context.registry->any_of<TilemapLayerComponent>(entity)) {

				// Assign orphaned layers to this tilemap
				for (auto layer : tilemapLayers) {
					auto& layerNode = tilemapLayers.get<SceneNodeComponent>(layer);
					if (layerNode.parent == entt::null) {
						layerNode.parent = entity;
						node.children.push_back(layer);
					}
				}
			}
		}

		// Assign orphaned chunks to layers
		for (auto layer : tilemapLayers) {
			auto& layerNode = tilemapLayers.get<SceneNodeComponent>(layer);

			for (auto chunk : tilemapChunks) {
				auto& chunkNode = tilemapChunks.get<SceneNodeComponent>(chunk);
				if (chunkNode.parent == entt::null) {
					chunkNode.parent = layer;
					layerNode.children.push_back(chunk);
				}
			}
		}
	}

	void JsonSceneLoader::FindSpecialEntities(LoadContext& context) {
		// Find player
		for (auto entity : context.registry->view<PlayerTagComponent>()) {
			context.result.playerEntity = entity;
			if (auto* transform = context.registry->try_get<TransformComponent>(entity)) {
				context.result.playerPosition = transform->localPosition;
			}
			break;
		}

		// Find main tilemap
		auto nodeView = context.registry->view<SceneNodeComponent>();
		for (auto entity : nodeView) {
			const auto& node = nodeView.get<SceneNodeComponent>(entity);

			if (node.name.find("Tilemap") != std::string::npos && !node.children.empty()) {
				bool hasLayerChildren = std::any_of(node.children.begin(), node.children.end(),
					[this, &context](entt::entity child) {
						return context.registry->any_of<TilemapLayerComponent>(child);
					});

				if (hasLayerChildren) {
					context.result.mainTilemap = entity;
					break;
				}
			}
		}
	}

	void JsonSceneLoader::LoadEntityComponents(entt::entity entity, const nlohmann::json& components,
		entt::registry& registry) {

		nlohmann::json scriptData;

		for (const auto& [componentName, componentData] : components.items()) {

			if (componentName == "AnimationClipsComponent") {
				AnimationClipsComponent acc;
				acc.LoadFromJson(componentData);
				registry.emplace_or_replace<AnimationClipsComponent>(entity, std::move(acc));
			}
			else if (componentName == "TilemapChunkComponent") {
				TilemapChunkComponent chunk;
				from_json(componentData, chunk);
				chunk.dirty = true;
				chunk.loaded = true;
				chunk.visible = true;
				registry.emplace_or_replace<TilemapChunkComponent>(entity, std::move(chunk));
			}
			else if (IsNativeComponent(componentName)) {
				LoadReflectedComponent(componentName, componentData, entity, registry);
			}
			else {
				scriptData[componentName] = componentData;
			}
		}

		if (!scriptData.empty()) {
			registry.emplace_or_replace<ScriptDataComponent>(entity, scriptData.dump());
		}
	}

	bool JsonSceneLoader::IsNativeComponent(const std::string& componentName) {
		const auto& typeRegistry = Reflect::TypeRegistry::Get().GetNameMap();
		return typeRegistry.find(componentName) != typeRegistry.end();
	}

	void JsonSceneLoader::LoadReflectedComponent(const std::string& name, const nlohmann::json& data,
		entt::entity entity, entt::registry& registry) {

		const auto& typeRegistry = Reflect::TypeRegistry::Get().GetNameMap();
		auto it = typeRegistry.find(name);
		if (it != typeRegistry.end() && it->second.loadFn) {
			nlohmann::json wrapper;
			wrapper[name] = data;
			it->second.loadFn(registry, entity, wrapper);
		}
	}

} // namespace WanderSpire::Scene