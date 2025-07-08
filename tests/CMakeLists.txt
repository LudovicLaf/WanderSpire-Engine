cmake_minimum_required(VERSION 3.30)
project(WanderSpireTests LANGUAGES CXX)

# 1) Find Catch2, SDL3 and OpenAL
find_package(Catch2 REQUIRED)
find_package(SDL3    CONFIG REQUIRED)
find_package(OpenAL  CONFIG REQUIRED)

# 2) Engine headers
include_directories(
  ${CMAKE_SOURCE_DIR}/Engine/WanderSpire/include
  ${CMAKE_SOURCE_DIR}/EngineCore/include
)

# 3) Build the test runner
add_executable(WanderSpireTests
  test_main.cpp
  test_pathfinding.cpp
  test_serialization.cpp
  test_reflection.cpp
  test_prefab_cycle.cpp
  
)

if (MSVC)
    target_compile_options(WanderSpireTests PRIVATE /bigobj)
endif()


# 4) Link against engine + C-API + SDL3 + OpenAL + Catch
target_link_libraries(WanderSpireTests PRIVATE
  WanderSpire
  EngineCore
  SDL3::SDL3
  OpenAL::OpenAL
  Catch2::Catch2WithMain
)

# 5) Post-build: copy each DLL into the same folder as WanderSpireTests.exe
add_custom_command(TARGET WanderSpireTests POST_BUILD
  # EngineCore.dll
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
          $<TARGET_FILE:EngineCore>
          $<TARGET_FILE_DIR:WanderSpireTests>/EngineCore.dll
  # SDL3.dll
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
          $<TARGET_FILE:SDL3::SDL3>
          $<TARGET_FILE_DIR:WanderSpireTests>/SDL3.dll
  # OpenAL32.dll (or whatever OpenAL::OpenAL yields)
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
          $<TARGET_FILE:OpenAL::OpenAL>
          $<TARGET_FILE_DIR:WanderSpireTests>/OpenAL32.dll
)

# 6) Enable CTest + auto-discover tests, but wait until after build so those DLLs are present
include(CTest)
include(Catch)
catch_discover_tests(WanderSpireTests DISCOVER_AFTER_BUILD)
