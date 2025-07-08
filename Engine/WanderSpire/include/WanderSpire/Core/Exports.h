#pragma once

#if defined(_WIN32)
#if defined(WANDER_SPIRE_BUILD_DLL)
#define WANDER_SPIRE_API __declspec(dllexport)
#else
#define WANDER_SPIRE_API __declspec(dllimport)
#endif
#else
#define WANDER_SPIRE_API
#endif
