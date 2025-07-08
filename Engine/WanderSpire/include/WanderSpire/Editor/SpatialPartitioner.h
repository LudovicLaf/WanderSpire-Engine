#pragma once
#include <memory>
#include <array>
#include <vector>
#include <unordered_map>
#include <glm/glm.hpp>
#include <entt/entt.hpp>

namespace WanderSpire {

	class SpatialPartitioner {
	public:
		static SpatialPartitioner& GetInstance();

		void Initialize(const glm::vec2& worldMin, const glm::vec2& worldMax, int maxDepth = 8);
		void Clear();

		void InsertObject(entt::entity entity, const glm::vec2& min, const glm::vec2& max);
		void UpdateObject(entt::entity entity, const glm::vec2& min, const glm::vec2& max);
		void RemoveObject(entt::entity entity);

		std::vector<entt::entity> QueryRegion(const glm::vec2& min, const glm::vec2& max);
		std::vector<entt::entity> QueryCircle(const glm::vec2& center, float radius);
		std::vector<entt::entity> QueryPoint(const glm::vec2& point);

		void Optimize();
		int GetNodeCount() const;
		int GetObjectCount() const;

	private:
		struct QuadNode {
			glm::vec2 min, max;
			std::vector<entt::entity> objects;
			std::array<std::unique_ptr<QuadNode>, 4> children;
			bool isLeaf = true;
			int depth = 0;
		};

		std::unique_ptr<QuadNode> root;
		std::unordered_map<entt::entity, glm::vec4> objectBounds; // min.xy, max.zw
		int maxDepth = 8;
		int maxObjectsPerNode = 10;

		void InsertIntoNode(QuadNode* node, entt::entity entity, const glm::vec2& min, const glm::vec2& max);
		void SubdivideNode(QuadNode* node);
		void QueryRegionRecursive(QuadNode* node, const glm::vec2& min, const glm::vec2& max, std::vector<entt::entity>& results);
		bool BoundsIntersect(const glm::vec2& min1, const glm::vec2& max1, const glm::vec2& min2, const glm::vec2& max2);
		void RemoveFromNode(QuadNode* node, entt::entity entity);
		int CountNodes(QuadNode* node) const;
	};

} // namespace WanderSpire
