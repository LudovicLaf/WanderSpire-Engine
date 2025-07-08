// ─────────────────────────────────────────────────────────────────────────────
//  ReflectionMacros.h         -- dual-registry version
//      • Generates field metadata for your custom TypeRegistry
//      • *Also* registers the same type & fields in entt::meta
// ─────────────────────────────────────────────────────────────────────────────
#pragma once
#include <boost/preprocessor.hpp>
#include <entt/entt.hpp>
#include "WanderSpire/Core/Reflection.h"

// ─── utility helpers ────────────────────────────────────────────────────────
#define WS_CAT(a,b)            BOOST_PP_CAT(a,b)
#define WS_UNIQ(prefix)        WS_CAT(prefix, __COUNTER__)

// FIELD(ftype, member, min, max, step)  → turn into a Boost tuple
#define FIELD(ftype, member, minv, maxv, stepv) (ftype, member, minv, maxv, stepv)

// ─── expanders for the custom TypeRegistry ──────────────────────────────────
#define WS_EXPAND_FIELD(r, _, tup) WS_MAKE_FIELD tup
#define WS_MAKE_FIELD(ftype, member, minv, maxv, stepv)                             \
    ti.addField(                                                                    \
        #member, Reflect::FieldType::ftype,                                         \
        offsetof(Self, member),                                                     \
        float(minv), float(maxv), float(stepv)                                      \
    );

// ─── expanders for entt::meta registration ──────────────────────────────────
#define WS_EXPAND_META_FIELD(r, Self, tup) WS_MAKE_META_FIELD(Self, tup)
#define WS_MAKE_META_FIELD(Self, tup)                                              \
    type_node.data<&Self::BOOST_PP_TUPLE_ELEM(1, tup)>(                            \
        entt::hashed_string{ BOOST_PP_STRINGIZE(BOOST_PP_TUPLE_ELEM(1, tup)) }     \
    );

// ─────────────────────────────────────────────────────────────────────────────
//  REFLECTABLE -- use inside a struct/union/class that has *fields*
// ─────────────────────────────────────────────────────────────────────────────
#define REFLECTABLE(Type, ...)                                                     \
    inline static bool WS_UNIQ(_ws_reflect_) = []() {                              \
        using Self = Type;                                                         \
                                                                                   \
        /* ── 1) custom registry ───────────────────────────────────────── */      \
        auto& ti = Reflect::TypeRegistry::Get()                                    \
                       .template registerType<Self>(#Type);                        \
        if (ti.fields.empty()) {                                                   \
            BOOST_PP_SEQ_FOR_EACH(WS_EXPAND_FIELD, _,                              \
                BOOST_PP_VARIADIC_TO_SEQ(__VA_ARGS__))                             \
        }                                                                          \
                                                                                   \
        /* ── 2) entt::meta registration (only once) ───────────────────── */      \
        static bool _meta_once = [](){                                             \
            auto type_node = entt::meta<Self>()                                    \
                                 .type(entt::hashed_string{#Type});                \
            BOOST_PP_SEQ_FOR_EACH(WS_EXPAND_META_FIELD, Self,                      \
                BOOST_PP_VARIADIC_TO_SEQ(__VA_ARGS__))                             \
            return true;                                                           \
        }();                                                                       \
        (void)_meta_once;                                                          \
        return true;                                                               \
    }();

// ─────────────────────────────────────────────────────────────────────────────
//  REFLECT_TYPE -- for *tag / empty* components (no data members)
// ─────────────────────────────────────────────────────────────────────────────
#define REFLECT_TYPE(Type)                                                         \
    inline static bool WS_UNIQ(_ws_reflect_) = []() {                              \
        Reflect::TypeRegistry::Get()                                               \
               .template registerType<Type>(#Type);                                \
        entt::meta<Type>().type(entt::hashed_string{#Type});                       \
        return true;                                                               \
    }();
