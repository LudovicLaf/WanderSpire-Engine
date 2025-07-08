cmake_minimum_required(VERSION 3.30)
project(WanderSpireSolution LANGUAGES CXX)

# ── vcpkg integration ───────────────────────────────────────────────
if(DEFINED ENV{VCPKG_ROOT} AND NOT DEFINED CMAKE_TOOLCHAIN_FILE)
  set(CMAKE_TOOLCHAIN_FILE
      "$ENV{VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake"
      CACHE STRING "")
endif()

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 1) Core engine (native C++ WanderSpire)
add_subdirectory(Engine/WanderSpire)

# 2) C ABI shim (EngineCore.dll)
add_subdirectory(EngineCore)

# 3) C++ unit-tests
add_subdirectory(tests)
