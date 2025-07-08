#pragma once

//==============================================================================
// SerializableComponents.h
//
// Centralise the “whitelist” of components we persist in scenes & prefabs.
// Used by SceneManager and PrefabManager via X-macro expansion.
//
// Whenever you add a new component that should be written to JSON,
// simply add another X(…) entry below.
//
// NOTE: order doesn’t matter.
//==============================================================================

#include "WanderSpire/Components/TagComponent.h"
#include "WanderSpire/Components/IDComponent.h"
#include "WanderSpire/Components/PrefabIdComponent.h" 
#include "WanderSpire/Components/CommentComponent.h"
#include "WanderSpire/Components/GridPositionComponent.h"
#include "WanderSpire/Components/TransformComponent.h"
#include "WanderSpire/Components/SceneNodeComponent.h"
#include "WanderSpire/Components/SpatialNodeComponent.h"
#include "WanderSpire/Components/GizmoComponent.h"
#include "WanderSpire/Components/SelectableComponent.h"
#include "WanderSpire/Components/EditorMetadataComponent.h"
#include "WanderSpire/Components/SpriteComponent.h"
#include "WanderSpire/Components/SpriteAnimationComponent.h"
#include "WanderSpire/Components/ObstacleComponent.h"
#include "WanderSpire/Components/AnimationStateComponent.h"
#include "WanderSpire/Components/AnimationClipsComponent.h"
#include "WanderSpire/Components/FacingComponent.h"
#include "WanderSpire/Components/PlayerTagComponent.h"
#include "WanderSpire/Components/ScriptDataComponent.h"
#include "WanderSpire/Components/LayerComponent.h"
#include "WanderSpire/Components/RenderBatchComponent.h"
#include "WanderSpire/Components/LODComponent.h"
#include "WanderSpire/Components/CullingComponent.h"
#include "WanderSpire/Components/TileComponent.h"
#include "WanderSpire/Components/TilemapLayerComponent.h"
#include "WanderSpire/Components/AssetReferenceComponent.h"
#include "WanderSpire/Components/PrefabInstanceComponent.h"
#include "WanderSpire/Components/TilePaletteComponent.h"
#include "WanderSpire/Components/TileBrushComponent.h"
#include "WanderSpire/Components/AutoTilingComponent.h"
#include "WanderSpire/Components/AnimatedTileComponent.h"

// X-macro invocation point.  
// In your .cpp you’ll do something like:
//    #define X(COMP) TrySaveComponent<COMP>(registry,e,ej);
//    SERIALIZABLE_COMPONENTS
//    #undef X
#define SERIALIZABLE_COMPONENTS           \
    X(TagComponent)                       \
    X(IDComponent)                        \
    X(PrefabIdComponent)                  \
    X(CommentComponent)                   \
    X(GridPositionComponent)              \
    X(TransformComponent)                 \
    X(SceneNodeComponent)                 \
    X(SpatialNodeComponent)               \
    X(GizmoComponent)                     \
    X(SelectableComponent)                \
    X(EditorMetadataComponent)            \
    X(SpriteComponent)                    \
    X(SpriteAnimationComponent)           \
    X(ObstacleComponent)                  \
    X(AnimationStateComponent)            \
    X(AnimationClipsComponent)            \
    X(FacingComponent)                    \
    X(ScriptDataComponent)                \
    X(PlayerTagComponent)                 \
    X(LayerComponent)                     \
    X(RenderBatchComponent)               \
    X(LODComponent)                       \
    X(CullingComponent)                   \
    X(TileComponent)                      \
    X(TilemapLayerComponent)              \
    X(AssetReferenceComponent)            \
    X(PrefabInstanceComponent)            \
    X(TilePaletteComponent)               \
    X(TileBrushComponent)                 \
    X(AutoTilingComponent)                \
    X(AnimatedTileComponent)              \
