#include <catch2/catch_test_macros.hpp>
#include "TestHelpers.h"

TEST_CASE("Serialize and deserialize GridPositionComponent", "[serialization]") {
	entt::registry reg;
	auto e = reg.create();
	reg.emplace<GridPositionComponent>(e, glm::ivec2{ 2,3 });

	// Serialize
	json j;
	auto const& ti = Reflect::TypeRegistry::Get().GetNameMap().at(
		getTypeInfo<GridPositionComponent>().name
	);
	ti.saveFn(reg, e, j);

	// should have the full type name as key
	REQUIRE(j.contains(ti.name));

	// round-trip with modified tile
	json modified = {
		{ ti.name, { { "tile", {4,5} } } }
	};
	ti.loadFn(reg, e, modified);

	auto comp = reg.get<GridPositionComponent>(e);
	REQUIRE(comp.tile.x == 4);
	REQUIRE(comp.tile.y == 5);
}
