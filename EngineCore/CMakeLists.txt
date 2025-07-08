cmake_minimum_required(VERSION 3.30)
project(EngineCore LANGUAGES CXX)

# ── dependencies ────────────────────────────────────────────────────────
find_package(EnTT           CONFIG REQUIRED)
find_package(nlohmann_json  CONFIG REQUIRED)
find_package(spdlog         CONFIG REQUIRED)
find_package(SDL3           CONFIG REQUIRED)
find_package(OpenAL         CONFIG REQUIRED)
find_package(glad           CONFIG REQUIRED)
find_package(glm            CONFIG REQUIRED)
find_package(imgui          CONFIG REQUIRED)

# ── EngineCore (native C++ shared library) ─────────────────────────────
add_library(EngineCore SHARED
    src/EngineCore.cpp
    src/SceneEditorAPI.cpp   # <---- Added this line
    src/cimgui.cpp                
)

target_compile_definitions(EngineCore PRIVATE ENGINECORE_EXPORTS)

if(NOT WIN32)
  target_link_libraries(EngineCore PUBLIC dl)
endif()

if (MSVC)
    target_compile_options(EngineCore PRIVATE /bigobj)
endif()

target_include_directories(EngineCore
  PUBLIC
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src>  
    $<BUILD_INTERFACE:${CMAKE_SOURCE_DIR}/Engine/WanderSpire/include>
)

target_link_libraries(EngineCore
  PUBLIC
    WanderSpire
    SDL3::SDL3
    glad::glad
    OpenAL::OpenAL
    spdlog::spdlog
    EnTT::EnTT
    nlohmann_json::nlohmann_json
    imgui::imgui
)

set_target_properties(EngineCore PROPERTIES
  WINDOWS_EXPORT_ALL_SYMBOLS ON
  POSITION_INDEPENDENT_CODE  ON
)

install(TARGETS EngineCore
        ARCHIVE DESTINATION lib
        LIBRARY DESTINATION lib
        RUNTIME DESTINATION bin)

install(FILES
        include/EngineAPI.h
        include/CSAPI.h
        include/ScriptHostBridge.h
        DESTINATION include)
