#include "WanderSpire/Editor/SpatialPartitioner.h"
#include <algorithm>
#include <cmath>
#include <spdlog/spdlog.h>
#define GLM_ENABLE_EXPERIMENTAL
#include <glm/gtx/norm.hpp>

namespace WanderSpire {

	SpatialPartitioner& SpatialPartitioner::GetInstance() {
		static SpatialPartitioner instance;
		return instance;
	}

	void SpatialPartitioner::Initialize(const glm::vec2& worldMin, const glm::vec2& worldMax, int maxDepth) {
		this->maxDepth = maxDepth;

		root = std::make_unique<QuadNode>();
		root->min = worldMin;
		root->max = worldMax;
		root->isLeaf = true;
		root->depth = 0;

		objectBounds.clear();

		spdlog::info("[SpatialPartitioner] Initialized with bounds ({:.1f},{:.1f}) to ({:.1f},{:.1f}), max depth {}",
			worldMin.x, worldMin.y, worldMax.x, worldMax.y, maxDepth);
	}

	void SpatialPartitioner::Clear() {
		root.reset();
		objectBounds.clear();
		spdlog::debug("[SpatialPartitioner] Cleared all data");
	}

	void SpatialPartitioner::InsertObject(entt::entity entity, const glm::vec2& min, const glm::vec2& max) {
		if (!root) {
			spdlog::error("[SpatialPartitioner] Not initialized");
			return;
		}

		// Remove if already exists
		RemoveObject(entity);

		// Store bounds
		objectBounds[entity] = glm::vec4(min.x, min.y, max.x, max.y);

		// Insert into tree
		InsertIntoNode(root.get(), entity, min, max);

		spdlog::debug("[SpatialPartitioner] Inserted entity {} with bounds ({:.1f},{:.1f}) to ({:.1f},{:.1f})",
			entt::to_integral(entity), min.x, min.y, max.x, max.y);
	}

	void SpatialPartitioner::UpdateObject(entt::entity entity, const glm::vec2& min, const glm::vec2& max) {
		auto it = objectBounds.find(entity);
		if (it == objectBounds.end()) {
			// Object doesn't exist, just insert
			InsertObject(entity, min, max);
			return;
		}

		// Check if bounds changed significantly
		glm::vec4 oldBounds = it->second;
		glm::vec2 oldMin(oldBounds.x, oldBounds.y);
		glm::vec2 oldMax(oldBounds.z, oldBounds.w);

		const float threshold = 1.0f; // Avoid frequent updates for small movements
		if (glm::distance(oldMin, min) < threshold && glm::distance(oldMax, max) < threshold) {
			return;
		}

		// Remove and re-insert
		RemoveObject(entity);
		InsertObject(entity, min, max);
	}

	void SpatialPartitioner::RemoveObject(entt::entity entity) {
		auto it = objectBounds.find(entity);
		if (it == objectBounds.end()) {
			return;
		}

		objectBounds.erase(it);

		// Remove from tree nodes (recursive cleanup)
		RemoveFromNode(root.get(), entity);

		spdlog::debug("[SpatialPartitioner] Removed entity {}", entt::to_integral(entity));
	}

	std::vector<entt::entity> SpatialPartitioner::QueryRegion(const glm::vec2& min, const glm::vec2& max) {
		if (!root) {
			return {};
		}

		std::vector<entt::entity> results;
		QueryRegionRecursive(root.get(), min, max, results);

		spdlog::debug("[SpatialPartitioner] Query region ({:.1f},{:.1f}) to ({:.1f},{:.1f}) found {} objects",
			min.x, min.y, max.x, max.y, results.size());

		return results;
	}

	std::vector<entt::entity> SpatialPartitioner::QueryCircle(const glm::vec2& center, float radius) {
		// Query bounding box first, then filter by distance
		glm::vec2 min = center - glm::vec2(radius);
		glm::vec2 max = center + glm::vec2(radius);

		auto candidates = QueryRegion(min, max);
		std::vector<entt::entity> results;

		float radiusSquared = radius * radius;

		for (entt::entity entity : candidates) {
			auto it = objectBounds.find(entity);
			if (it != objectBounds.end()) {
				glm::vec4 bounds = it->second;
				glm::vec2 objMin(bounds.x, bounds.y);
				glm::vec2 objMax(bounds.z, bounds.w);
				glm::vec2 objCenter = (objMin + objMax) * 0.5f;

				if (glm::distance2(center, objCenter) <= radiusSquared) {
					results.push_back(entity);
				}
			}
		}

		spdlog::debug("[SpatialPartitioner] Query circle at ({:.1f},{:.1f}) radius {:.1f} found {} objects",
			center.x, center.y, radius, results.size());

		return results;
	}

	std::vector<entt::entity> SpatialPartitioner::QueryPoint(const glm::vec2& point) {
		return QueryRegion(point, point);
	}

	void SpatialPartitioner::Optimize() {
		if (!root) {
			return;
		}

		// Collect all objects
		std::vector<std::pair<entt::entity, glm::vec4>> allObjects;
		for (const auto& pair : objectBounds) {
			allObjects.push_back(pair);
		}

		// Clear and rebuild
		Clear();

		if (!allObjects.empty()) {
			// Calculate optimal world bounds
			glm::vec2 newMin(std::numeric_limits<float>::max());
			glm::vec2 newMax(std::numeric_limits<float>::lowest());

			for (const auto& obj : allObjects) {
				glm::vec4 bounds = obj.second;
				newMin = glm::min(newMin, glm::vec2(bounds.x, bounds.y));
				newMax = glm::max(newMax, glm::vec2(bounds.z, bounds.w));
			}

			// Add some padding
			glm::vec2 padding = (newMax - newMin) * 0.1f;
			newMin -= padding;
			newMax += padding;

			Initialize(newMin, newMax, maxDepth);

			// Re-insert all objects
			for (const auto& obj : allObjects) {
				glm::vec4 bounds = obj.second;
				InsertObject(obj.first, glm::vec2(bounds.x, bounds.y), glm::vec2(bounds.z, bounds.w));
			}
		}

		spdlog::info("[SpatialPartitioner] Optimized tree with {} objects", allObjects.size());
	}

	int SpatialPartitioner::GetNodeCount() const {
		if (!root) return 0;
		return CountNodes(root.get());
	}

	int SpatialPartitioner::GetObjectCount() const {
		return static_cast<int>(objectBounds.size());
	}

	void SpatialPartitioner::InsertIntoNode(QuadNode* node, entt::entity entity, const glm::vec2& min, const glm::vec2& max) {
		// Check if object fits entirely within this node
		if (!BoundsIntersect(min, max, node->min, node->max)) {
			return;
		}

		if (node->isLeaf) {
			node->objects.push_back(entity);

			// Subdivide if we have too many objects and haven't reached max depth
			if (static_cast<int>(node->objects.size()) > maxObjectsPerNode && node->depth < maxDepth) {
				SubdivideNode(node);
			}
		}
		else {
			// Insert into children
			for (auto& child : node->children) {
				if (child && BoundsIntersect(min, max, child->min, child->max)) {
					InsertIntoNode(child.get(), entity, min, max);
				}
			}
		}
	}

	void SpatialPartitioner::SubdivideNode(QuadNode* node) {
		if (!node->isLeaf) {
			return;
		}

		glm::vec2 center = (node->min + node->max) * 0.5f;

		// Create 4 children
		// 0: Top-left, 1: Top-right, 2: Bottom-left, 3: Bottom-right
		node->children[0] = std::make_unique<QuadNode>();
		node->children[0]->min = glm::vec2(node->min.x, center.y);
		node->children[0]->max = glm::vec2(center.x, node->max.y);
		node->children[0]->depth = node->depth + 1;

		node->children[1] = std::make_unique<QuadNode>();
		node->children[1]->min = center;
		node->children[1]->max = node->max;
		node->children[1]->depth = node->depth + 1;

		node->children[2] = std::make_unique<QuadNode>();
		node->children[2]->min = node->min;
		node->children[2]->max = center;
		node->children[2]->depth = node->depth + 1;

		node->children[3] = std::make_unique<QuadNode>();
		node->children[3]->min = glm::vec2(center.x, node->min.y);
		node->children[3]->max = glm::vec2(node->max.x, center.y);
		node->children[3]->depth = node->depth + 1;

		// Redistribute objects to children
		std::vector<entt::entity> objectsToRedistribute = std::move(node->objects);
		node->objects.clear();
		node->isLeaf = false;

		for (entt::entity entity : objectsToRedistribute) {
			auto it = objectBounds.find(entity);
			if (it != objectBounds.end()) {
				glm::vec4 bounds = it->second;
				glm::vec2 objMin(bounds.x, bounds.y);
				glm::vec2 objMax(bounds.z, bounds.w);

				for (auto& child : node->children) {
					if (BoundsIntersect(objMin, objMax, child->min, child->max)) {
						child->objects.push_back(entity);
					}
				}
			}
		}

		spdlog::debug("[SpatialPartitioner] Subdivided node at depth {}", node->depth);
	}

	void SpatialPartitioner::QueryRegionRecursive(QuadNode* node, const glm::vec2& min, const glm::vec2& max, std::vector<entt::entity>& results) {
		if (!BoundsIntersect(min, max, node->min, node->max)) {
			return;
		}

		if (node->isLeaf) {
			// Add all objects in this leaf
			for (entt::entity entity : node->objects) {
				auto it = objectBounds.find(entity);
				if (it != objectBounds.end()) {
					glm::vec4 bounds = it->second;
					glm::vec2 objMin(bounds.x, bounds.y);
					glm::vec2 objMax(bounds.z, bounds.w);

					if (BoundsIntersect(min, max, objMin, objMax)) {
						results.push_back(entity);
					}
				}
			}
		}
		else {
			// Recursively query children
			for (auto& child : node->children) {
				if (child) {
					QueryRegionRecursive(child.get(), min, max, results);
				}
			}
		}
	}

	bool SpatialPartitioner::BoundsIntersect(const glm::vec2& min1, const glm::vec2& max1, const glm::vec2& min2, const glm::vec2& max2) {
		return !(max1.x < min2.x || min1.x > max2.x || max1.y < min2.y || min1.y > max2.y);
	}

	void SpatialPartitioner::RemoveFromNode(QuadNode* node, entt::entity entity) {
		if (node->isLeaf) {
			auto it = std::find(node->objects.begin(), node->objects.end(), entity);
			if (it != node->objects.end()) {
				node->objects.erase(it);
			}
		}
		else {
			for (auto& child : node->children) {
				if (child) {
					RemoveFromNode(child.get(), entity);
				}
			}

			// Check if we can merge children back into this node
			int totalObjects = 0;
			for (auto& child : node->children) {
				if (child) {
					if (!child->isLeaf) {
						return; // Can't merge if any child is not a leaf
					}
					totalObjects += static_cast<int>(child->objects.size());
				}
			}

			if (totalObjects <= maxObjectsPerNode / 2) {
				// Merge children back into this node
				node->objects.clear();
				for (auto& child : node->children) {
					if (child) {
						node->objects.insert(node->objects.end(), child->objects.begin(), child->objects.end());
						child.reset();
					}
				}
				node->isLeaf = true;
			}
		}
	}

	int SpatialPartitioner::CountNodes(QuadNode* node) const {
		if (!node) return 0;

		int count = 1;
		if (!node->isLeaf) {
			for (auto& child : node->children) {
				count += CountNodes(child.get());
			}
		}
		return count;
	}

} // namespace WanderSpire