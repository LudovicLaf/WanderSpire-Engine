#include <catch2/catch_test_macros.hpp>
#include "TestHelpers.h"

TEST_CASE("Reflection registry contains GridPositionComponent", "[reflection]") {
	auto const& map = Reflect::TypeRegistry::Get().GetNameMap();
	auto const& name = getTypeInfo<GridPositionComponent>().name;
	REQUIRE(map.find(name) != map.end());

	auto const& ti = map.at(name);
	// look for the 'tile' field
	auto it = std::find_if(
		ti.fields.begin(), ti.fields.end(),
		[&](auto const& f) { return f.name == "tile"; }
	);
	REQUIRE(it != ti.fields.end());
}
