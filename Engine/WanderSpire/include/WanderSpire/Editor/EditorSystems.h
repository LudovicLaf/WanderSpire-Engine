#pragma once

// ========== Core Editor Systems ==========
#include "WanderSpire/Editor/ICommand.h"
#include "WanderSpire/Editor/CommandHistory.h"
#include "WanderSpire/Editor/SceneHierarchyManager.h"
#include "WanderSpire/Editor/SelectionManager.h"
#include "WanderSpire/Editor/LayerManager.h"
#include "WanderSpire/World/TilemapSystem.h"
#include "WanderSpire/Editor/AssetDependencyTracker.h"
#include "WanderSpire/Editor/SpatialPartitioner.h"

// ========== Editor Command Implementations ==========
#include "WanderSpire/Editor/Commands/TransformCommands.h"
#include "WanderSpire/Editor/Commands/HierarchyCommands.h"
#include "WanderSpire/Editor/Commands/SelectionCommands.h"
#include "WanderSpire/Editor/Commands/TilemapCommands.h"
#include "WanderSpire/Editor/Commands/ComponentCommands.h"
#include "WanderSpire/Editor/Commands/CompoundCommand.h"
#include "WanderSpire/Editor/Commands/LayerCommands.h"
#include "WanderSpire/Editor/Commands/PrefabCommands.h"
#include "WanderSpire/Editor/Commands/EditorCommandUtils.h"

// ==== API namespace marker (optional) ====
namespace WanderSpire {}
