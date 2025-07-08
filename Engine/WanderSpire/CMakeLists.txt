cmake_minimum_required(VERSION 3.30)
project(WanderSpire LANGUAGES CXX)

# Gather all engine sources and headers
file(GLOB_RECURSE WANDERSPIRE_SOURCES
     ${CMAKE_CURRENT_SOURCE_DIR}/src/**/*.cpp
     ${CMAKE_CURRENT_SOURCE_DIR}/src/*.cpp
     ${CMAKE_CURRENT_SOURCE_DIR}/Editor/*.cpp
)
file(GLOB_RECURSE WANDERSPIRE_HEADERS
     ${CMAKE_CURRENT_SOURCE_DIR}/include/**/*.h
)

add_library(WanderSpire STATIC
    ${WANDERSPIRE_SOURCES}
    ${WANDERSPIRE_HEADERS}
)
set_target_properties(WanderSpire PROPERTIES
    POSITION_INDEPENDENT_CODE ON
)

if (MSVC)
    target_compile_options(WanderSpire PRIVATE /bigobj)
endif()

# find dependencies
find_package(SDL3    CONFIG REQUIRED)
find_package(OpenAL  CONFIG REQUIRED)
find_package(glm     CONFIG REQUIRED)
find_package(glad    CONFIG REQUIRED)
find_package(spdlog  CONFIG REQUIRED)
find_package(EnTT      CONFIG REQUIRED)
find_package(nlohmann_json CONFIG REQUIRED)
find_package(imgui          CONFIG REQUIRED)

# public include dirs for engine consumers
target_include_directories(WanderSpire
  PUBLIC
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include/WanderSpire/Core>
)

# link all dependencies PUBLICly so that EngineCore & ChudScape pick them up transitively
target_link_libraries(WanderSpire
  PUBLIC SDL3::SDL3
  PUBLIC glad::glad
  PUBLIC OpenAL::OpenAL
  PUBLIC spdlog::spdlog
  PUBLIC EnTT::EnTT
  PUBLIC nlohmann_json::nlohmann_json
  PUBLIC imgui::imgui
)

install(TARGETS WanderSpire
        ARCHIVE DESTINATION lib
        LIBRARY DESTINATION lib
        RUNTIME DESTINATION bin
)
