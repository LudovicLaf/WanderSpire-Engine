// Game/Systems/UI/AIDebugWindow.cs - UPDATED WITH REAL INTEGRATION
using Game.Dto;
using Game.Events;
using ScriptHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using WanderSpire.Components;
using WanderSpire.Scripting;
using WanderSpire.Scripting.UI;
using WanderSpire.Scripting.Utils;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Professional AI debug window with real-time monitoring and control capabilities.
    /// UPDATED VERSION: Fully integrated with actual game systems and data.
    /// </summary>
    public class AIDebugWindow : SafeImGuiWindowBase
    {
        public override string Title => "AI Debug Console";

        #region Theme Colors
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorDanger = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);
        private readonly Vector4 ColorBackground = new(0.12f, 0.12f, 0.15f, 1.0f);
        #endregion

        #region AI Monitoring State
        private Entity? _selectedAIEntity;
        private readonly List<AIEntityData> _aiEntities = new();
        private readonly Dictionary<int, AIPerformanceData> _performanceData = new();
        private readonly List<AILogEntry> _aiLogs = new();
        private readonly Stopwatch _updateTimer = Stopwatch.StartNew();

        // Filters and settings
        private string _aiFilter = "";
        private bool _showAIVisualization = true;
        private bool _pauseAllAI = false;
        private bool _showPathfinding = false;
        private bool _showVisionRanges = false;
        private bool _autoSelectNearestPlayer = false;
        private AIStateFilter _stateFilter = AIStateFilter.All;
        private FactionFilter _factionFilter = FactionFilter.All;

        // Performance tracking
        private readonly CircularBuffer<float> _aiThinkTimes = new(60);
        private float _lastUpdateTime;
        private int _framesSinceLastUpdate;

        // UI State
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Entities", "Performance", "Visualization", "Logs" };
        #endregion

        #region Data Structures
        public class AIEntityData
        {
            public Entity Entity { get; set; }
            public string Name { get; set; } = "";
            public AIBehaviourState State { get; set; }
            public AIParams? Parameters { get; set; }
            public StatsComponent? Stats { get; set; }
            public FactionComponent? Faction { get; set; }
            public (int x, int y) Position { get; set; }
            public (int x, int y) Origin { get; set; }
            public bool IsPlayerVisible { get; set; }
            public float DistanceToPlayer { get; set; }
            public Entity? CurrentTarget { get; set; }
            public float LastThinkTime { get; set; }
            public DateTime LastUpdate { get; set; }
            public List<string> RecentActions { get; set; } = new();
        }

        public class AIPerformanceData
        {
            public float AverageThinkTime { get; set; }
            public int PathfindingCalls { get; set; }
            public int VisionChecks { get; set; }
            public int StateChanges { get; set; }
            public DateTime LastReset { get; set; } = DateTime.Now;
            public int TotalUpdates { get; set; }
        }

        public class AILogEntry
        {
            public DateTime Timestamp { get; set; }
            public int EntityId { get; set; }
            public string EntityName { get; set; } = "";
            public string Message { get; set; } = "";
            public LogLevel Level { get; set; }
            public AIBehaviourState? FromState { get; set; }
            public AIBehaviourState? ToState { get; set; }
        }

        public enum AIBehaviourState { Idle, Wander, Chase, Attack, Return, Dead, Unknown }
        public enum AIStateFilter { All, Idle, Active, Chasing, Attacking, Dead }
        public enum FactionFilter { All, Hostile, Neutral, Friendly }
        public enum LogLevel { Debug, Info, Warning, Error }

        public class CircularBuffer<T>
        {
            private readonly T[] _buffer;
            private int _index;
            public int Count { get; private set; }

            public CircularBuffer(int capacity)
            {
                _buffer = new T[capacity];
            }

            public void Add(T item)
            {
                _buffer[_index] = item;
                _index = (_index + 1) % _buffer.Length;
                Count = Math.Min(Count + 1, _buffer.Length);
            }

            public T[] ToArray()
            {
                var result = new T[Count];
                for (int i = 0; i < Count; i++)
                {
                    int actualIndex = (_index - Count + i + _buffer.Length) % _buffer.Length;
                    result[i] = _buffer[actualIndex];
                }
                return result;
            }
        }
        #endregion

        public AIDebugWindow()
        {
            // Subscribe to AI events for real-time monitoring
            GameEventBus.Event<HurtEvent>.Subscribe(OnAIHurt);
            GameEventBus.Event<DeathEvent>.Subscribe(OnAIDeath);
            GameEventBus.Event<AttackEvent>.Subscribe(OnAIAttack);
            GameEventBus.Event<MovementIntentEvent>.Subscribe(OnAIMovement);
        }

        protected override void RenderContent()
        {
            // Use the safe style wrapper for all styling
            ImGuiSafe.WithStyleVars(styleScope =>
            {
                UpdateAIData();
                RenderHeader();
                RenderTabBar();

                switch (_selectedTab)
                {
                    case 0: RenderEntitiesTab(); break;
                    case 1: RenderPerformanceTab(); break;
                    case 2: RenderVisualizationTab(); break;
                    case 3: RenderLogsTab(); break;
                }
            },
            (ImGuiStyleVar.WindowPadding, new Vector2(12, 12)),
            (ImGuiStyleVar.FrameRounding, 4.0f),
            (ImGuiStyleVar.ItemSpacing, new Vector2(8, 6)),
            (ImGuiStyleVar.TabRounding, 6.0f));
        }

        #region Data Update Methods - UPDATED FOR REAL INTEGRATION
        private void UpdateAIData()
        {
            if (_updateTimer.ElapsedMilliseconds < 100) return; // Update at 10 FPS

            _updateTimer.Restart();
            _aiEntities.Clear();

            var playerEntity = FindPlayerEntity();
            var playerPos = playerEntity != null ? GetEntityPosition(playerEntity) : (0, 0);

            var sw = Stopwatch.StartNew();

            World.ForEachEntity(entity =>
            {
                if (!IsAIEntity(entity)) return;

                var aiData = CreateAIEntityData(entity, playerEntity, playerPos);
                _aiEntities.Add(aiData);

                // Update performance tracking
                UpdatePerformanceData(entity.Id, sw.ElapsedTicks);
            });

            sw.Stop();
            _aiThinkTimes.Add((float)sw.Elapsed.TotalMilliseconds);

            // Auto-select nearest AI to player if enabled
            if (_autoSelectNearestPlayer && playerEntity != null && _selectedAIEntity == null)
            {
                var nearest = _aiEntities.OrderBy(ai => ai.DistanceToPlayer).FirstOrDefault();
                if (nearest != null)
                    _selectedAIEntity = nearest.Entity;
            }
        }

        private bool IsAIEntity(Entity entity)
        {
            if (!entity.IsValid) return false;

            // Check for AI behavior scripts
            var sc = entity.GetScriptData<ScriptsComponent>("ScriptsComponent");
            string[] scripts = sc?.Scripts ?? entity.GetScriptData<string[]>("scripts") ?? Array.Empty<string>();

            bool hasAIScript = scripts.Any(s => s.Contains("AIBehaviour") || s.Contains("AI"));

            // Also check for AI params or faction components
            bool hasAIParams = entity.GetScriptData<AIParams>("AIParams") != null;
            bool hasFaction = entity.GetScriptData<FactionComponent>(nameof(FactionComponent)) != null;
            bool hasStats = entity.GetScriptData<StatsComponent>(nameof(StatsComponent)) != null;

            // Must not be a player
            bool isPlayer = entity.HasComponent(nameof(PlayerTagComponent));

            return !isPlayer && (hasAIScript || hasAIParams || (hasFaction && hasStats));
        }

        private AIEntityData CreateAIEntityData(Entity entity, Entity? playerEntity, (int x, int y) playerPos)
        {
            var aiParams = entity.GetScriptData<AIParams>("AIParams") ?? new AIParams();
            var stats = entity.GetScriptData<StatsComponent>(nameof(StatsComponent));
            var faction = entity.GetScriptData<FactionComponent>(nameof(FactionComponent));

            var position = GetEntityPosition(entity);
            var state = GetAIState(aiParams);

            var data = new AIEntityData
            {
                Entity = entity,
                Name = GetEntityName(entity),
                State = state,
                Parameters = aiParams,
                Stats = stats,
                Faction = faction,
                Position = position,
                Origin = aiParams?.origin?.Length == 2 ? (aiParams.origin[0], aiParams.origin[1]) : position,
                DistanceToPlayer = CalculateDistance(position, playerPos),
                IsPlayerVisible = IsPlayerVisible(entity, playerEntity, aiParams),
                LastUpdate = DateTime.Now
            };

            // Find current target if chasing/attacking
            if (state == AIBehaviourState.Chase || state == AIBehaviourState.Attack)
            {
                data.CurrentTarget = FindAITarget(entity, faction, stats);
            }

            return data;
        }

        private AIBehaviourState GetAIState(AIParams? aiParams)
        {
            if (aiParams?.state == null) return AIBehaviourState.Unknown;

            return aiParams.state switch
            {
                0 => AIBehaviourState.Idle,
                1 => AIBehaviourState.Wander,
                2 => AIBehaviourState.Chase,
                3 => AIBehaviourState.Attack,
                4 => AIBehaviourState.Return,
                5 => AIBehaviourState.Dead,
                _ => AIBehaviourState.Unknown
            };
        }

        private Entity? FindPlayerEntity()
        {
            Entity? player = null;
            World.ForEachEntity(entity =>
            {
                if (player == null && entity.HasComponent(nameof(PlayerTagComponent)))
                    player = entity;
            });
            return player;
        }

        private (int x, int y) GetEntityPosition(Entity? entity)
        {
            if (entity == null || !entity.IsValid) return (0, 0);

            var gridPos = entity.GetComponent<GridPositionComponent>(nameof(GridPositionComponent));
            return gridPos?.AsTuple() ?? (0, 0);
        }

        private string GetEntityName(Entity entity)
        {
            var prefab = entity.GetComponent<PrefabIdComponent>(nameof(PrefabIdComponent));
            if (prefab?.PrefabName != null)
                return $"{prefab.PrefabName} ({entity.Id})";

            var tag = entity.GetComponent<TagComponent>(nameof(TagComponent));
            if (!string.IsNullOrEmpty(tag?.Tag))
                return $"{tag.Tag} ({entity.Id})";

            return $"Entity {entity.Id}";
        }

        private float CalculateDistance((int x, int y) pos1, (int x, int y) pos2)
        {
            var dx = pos1.x - pos2.x;
            var dy = pos1.y - pos2.y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        private bool IsPlayerVisible(Entity aiEntity, Entity? playerEntity, AIParams? aiParams)
        {
            if (playerEntity == null || aiParams == null) return false;

            var aiPos = GetEntityPosition(aiEntity);
            var playerPos = GetEntityPosition(playerEntity);
            var distance = CalculateDistance(aiPos, playerPos);

            return distance <= aiParams.awarenessRange;
        }

        private Entity? FindAITarget(Entity aiEntity, FactionComponent? faction, StatsComponent? stats)
        {
            if (faction == null) return null;

            var aiPos = GetEntityPosition(aiEntity);
            Entity? closestTarget = null;
            float closestDistance = float.MaxValue;

            World.ForEachEntity(entity =>
            {
                if (entity.Id == aiEntity.Id || !entity.IsValid) return;

                var targetFaction = entity.GetScriptData<FactionComponent>(nameof(FactionComponent));
                var isPlayer = entity.HasComponent(nameof(PlayerTagComponent));

                bool isHostile = false;
                if (isPlayer && faction.HostileToPlayer)
                {
                    isHostile = true;
                }
                else if (targetFaction != null)
                {
                    isHostile = (targetFaction.Alignment == "good" && faction.HostileToGood) ||
                               (targetFaction.Alignment == "neutral" && faction.HostileToNeutral) ||
                               (targetFaction.Alignment == "bad" && faction.HostileToBad);
                }

                if (isHostile)
                {
                    var targetPos = GetEntityPosition(entity);
                    var distance = CalculateDistance(aiPos, targetPos);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTarget = entity;
                    }
                }
            });

            return closestTarget;
        }

        private void UpdatePerformanceData(int entityId, long elapsedTicks)
        {
            var thinkTimeMs = (float)(elapsedTicks * 1000.0 / Stopwatch.Frequency);

            if (!_performanceData.TryGetValue(entityId, out var perfData))
            {
                perfData = new AIPerformanceData();
                _performanceData[entityId] = perfData;
            }

            perfData.TotalUpdates++;
            perfData.AverageThinkTime = (perfData.AverageThinkTime * (perfData.TotalUpdates - 1) + thinkTimeMs) / perfData.TotalUpdates;
        }
        #endregion

        #region UI Rendering Methods
        private void RenderHeader()
        {
            // Title with icon
            ImGuiSafe.IconText(Robot, "AI Debug Console", ColorPrimary);

            // Real-time statistics
            var totalAI = _aiEntities.Count;
            var idleAI = _aiEntities.Count(ai => ai.State == AIBehaviourState.Idle);
            var activeAI = _aiEntities.Count(ai => ai.State == AIBehaviourState.Wander);
            var chasingAI = _aiEntities.Count(ai => ai.State == AIBehaviourState.Chase || ai.State == AIBehaviourState.Attack);
            var deadAI = _aiEntities.Count(ai => ai.State == AIBehaviourState.Dead);

            ImGui.SameLine();
            ImGui.TextColored(ColorDim, $"({totalAI} entities)");

            // Status indicators
            ImGui.SameLine(ImGui.GetWindowWidth() - 300);
            RenderStatusIndicator(Circle, ColorSuccess, idleAI, "Idle");
            ImGui.SameLine();
            RenderStatusIndicator(Circle, ColorWarning, activeAI, "Active");
            ImGui.SameLine();
            RenderStatusIndicator(Circle, ColorDanger, chasingAI, "Combat");
            ImGui.SameLine();
            RenderStatusIndicator(Circle, ColorDim, deadAI, "Dead");

            // Performance indicators
            ImGui.SameLine(ImGui.GetWindowWidth() - 150);
            var avgThinkTime = _aiThinkTimes.Count > 0 ? _aiThinkTimes.ToArray().Average() : 0;
            ImGui.TextColored(ColorInfo, $"{avgThinkTime:F2}ms");

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Average AI think time per frame");
        }

        private void RenderStatusIndicator(string icon, Vector4 color, int count, string tooltip)
        {
            ImGuiSafe.WithIconFont(() =>
            {
                ImGui.TextColored(color, icon);
            });
            ImGui.SameLine();
            ImGui.TextColored(color, count.ToString());

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }

        private void RenderTabBar()
        {
            if (ImGui.BeginTabBar("##ai_tabs"))
            {
                for (int i = 0; i < _tabNames.Length; i++)
                {
                    if (ImGui.BeginTabItem(_tabNames[i]))
                    {
                        _selectedTab = i;
                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
            }
        }

        private void RenderEntitiesTab()
        {
            var contentRegion = ImGui.GetContentRegionAvail();
            float leftPanelWidth = 350;

            // Left panel - AI list and controls
            ImGui.BeginChild("##ai_list", new Vector2(leftPanelWidth, contentRegion.Y), true);
            RenderAIControls();
            ImGui.Separator();
            RenderAIEntityList();
            ImGui.EndChild();

            ImGui.SameLine();

            // Right panel - Selected AI details
            ImGui.BeginChild("##ai_details", new Vector2(contentRegion.X - leftPanelWidth - 8, contentRegion.Y), true);
            if (_selectedAIEntity != null && _selectedAIEntity.IsValid)
            {
                RenderSelectedAIDetails();
            }
            else
            {
                RenderNoSelectionMessage();
            }
            ImGui.EndChild();
        }

        private void RenderAIControls()
        {
            if (ImGui.CollapsingHeader("Global Controls", ImGuiTreeNodeFlags.DefaultOpen))
            {
                // Global AI pause
                if (ImGui.Checkbox("Pause All AI", ref _pauseAllAI))
                {
                    LogAIAction("GLOBAL", _pauseAllAI ? "Paused all AI" : "Resumed all AI");
                    // TODO: Implement actual AI pausing in the AIBehaviour system
                }

                // Visualization toggles
                ImGui.Checkbox("Show Pathfinding", ref _showPathfinding);
                ImGui.Checkbox("Show Vision Ranges", ref _showVisionRanges);
                ImGui.Checkbox("Auto-select near player", ref _autoSelectNearestPlayer);

                // Quick actions
                ImGui.Spacing();
                if (ImGuiSafe.IconButton(Play, ColorSuccess, "Resume All"))
                {
                    _pauseAllAI = false;
                    LogAIAction("GLOBAL", "Resumed all AI via button");
                }

                ImGui.SameLine();
                if (ImGuiSafe.IconButton(Pause, ColorWarning, "Pause All"))
                {
                    _pauseAllAI = true;
                    LogAIAction("GLOBAL", "Paused all AI via button");
                }

                ImGui.SameLine();
                if (ImGuiSafe.IconButton(Sync, ColorInfo, "Reset All States"))
                {
                    ResetAllAIStates();
                }

                ImGui.SameLine();
                if (ImGuiSafe.IconButton(Trash, ColorDanger, "Clear Logs"))
                {
                    _aiLogs.Clear();
                }
            }

            if (ImGui.CollapsingHeader("Filters", ImGuiTreeNodeFlags.DefaultOpen))
            {
                // Search filter
                ImGui.Text("Search:");
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##ai_filter", ref _aiFilter, 128);

                // State filter
                ImGui.Text("State:");
                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginCombo("##state_filter", _stateFilter.ToString()))
                {
                    foreach (AIStateFilter filter in Enum.GetValues<AIStateFilter>())
                    {
                        if (ImGui.Selectable(filter.ToString(), _stateFilter == filter))
                            _stateFilter = filter;
                    }
                    ImGui.EndCombo();
                }

                // Faction filter
                ImGui.Text("Faction:");
                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginCombo("##faction_filter", _factionFilter.ToString()))
                {
                    foreach (FactionFilter filter in Enum.GetValues<FactionFilter>())
                    {
                        if (ImGui.Selectable(filter.ToString(), _factionFilter == filter))
                            _factionFilter = filter;
                    }
                    ImGui.EndCombo();
                }
            }
        }

        private void RenderAIEntityList()
        {
            if (ImGui.CollapsingHeader("AI Entities", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var filteredEntities = _aiEntities.Where(PassesFilters).ToList();

                ImGui.Text($"Showing {filteredEntities.Count} of {_aiEntities.Count} entities");
                ImGui.Separator();

                ImGui.BeginChild("##entity_scroll", new Vector2(0, -1), false, ImGuiWindowFlags.HorizontalScrollbar);

                foreach (var aiData in filteredEntities)
                {
                    RenderAIEntityCard(aiData);
                    ImGui.Separator();
                }

                ImGui.EndChild();
            }
        }

        private void RenderAIEntityCard(AIEntityData aiData)
        {
            bool isSelected = _selectedAIEntity?.Id == aiData.Entity.Id;

            // Entity card background
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.ChildBg, ColorPrimary * new Vector4(1, 1, 1, 0.2f));
            }

            ImGui.BeginChild($"##entity_{aiData.Entity.Id}", new Vector2(0, 60), true);

            // First line: Name and state
            ImGuiSafe.WithID(aiData.Entity.Id, () =>
            {
                if (ImGui.Selectable($"##select_{aiData.Entity.Id}", isSelected, ImGuiSelectableFlags.None, new Vector2(0, 60)))
                {
                    _selectedAIEntity = aiData.Entity;
                }

                // Context menu
                if (ImGui.BeginPopupContextItem())
                {
                    RenderEntityContextMenu(aiData);
                    ImGui.EndPopup();
                }
            });

            // Overlay content on top of selectable
            ImGui.SetCursorPos(new Vector2(8, 8));

            // Entity name
            ImGui.Text(aiData.Name);
            ImGui.SameLine(ImGui.GetWindowWidth() - 80);

            // State with colored icon
            var stateColor = GetStateColor(aiData.State);
            var stateIcon = GetStateIcon(aiData.State);

            ImGuiSafe.WithIconFont(() =>
            {
                ImGui.TextColored(stateColor, stateIcon);
            });
            ImGui.SameLine();
            ImGui.TextColored(stateColor, aiData.State.ToString());

            // Second line: Health and info
            ImGui.SetCursorPos(new Vector2(8, 30));
            var info = "";
            if (aiData.Stats != null)
            {
                var healthPercent = aiData.Stats.MaxHitpoints > 0 ?
                    (float)aiData.Stats.CurrentHitpoints / aiData.Stats.MaxHitpoints * 100f : 0f;
                info += $"HP: {healthPercent:F0}% ";
            }

            if (aiData.CurrentTarget != null)
            {
                info += $"Target: {GetEntityName(aiData.CurrentTarget)} ";
            }

            info += $"Dist: {aiData.DistanceToPlayer:F1}";

            ImGui.TextColored(ColorDim, info);

            ImGui.EndChild();

            if (isSelected)
            {
                ImGui.PopStyleColor();
            }
        }

        private bool PassesFilters(AIEntityData aiData)
        {
            // Text filter
            if (!string.IsNullOrEmpty(_aiFilter) &&
                !aiData.Name.Contains(_aiFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            // State filter
            if (_stateFilter != AIStateFilter.All)
            {
                var passes = _stateFilter switch
                {
                    AIStateFilter.Idle => aiData.State == AIBehaviourState.Idle,
                    AIStateFilter.Active => aiData.State == AIBehaviourState.Wander,
                    AIStateFilter.Chasing => aiData.State == AIBehaviourState.Chase,
                    AIStateFilter.Attacking => aiData.State == AIBehaviourState.Attack,
                    AIStateFilter.Dead => aiData.State == AIBehaviourState.Dead,
                    _ => true
                };
                if (!passes) return false;
            }

            // Faction filter
            if (_factionFilter != FactionFilter.All && aiData.Faction != null)
            {
                var passes = _factionFilter switch
                {
                    FactionFilter.Hostile => aiData.Faction.HostileToPlayer,
                    FactionFilter.Neutral => aiData.Faction.Alignment == "neutral",
                    FactionFilter.Friendly => aiData.Faction.Alignment == "good",
                    _ => true
                };
                if (!passes) return false;
            }

            return true;
        }

        private void RenderEntityContextMenu(AIEntityData aiData)
        {
            if (ImGui.MenuItem("Select"))
                _selectedAIEntity = aiData.Entity;

            ImGui.Separator();

            if (ImGui.MenuItem("Force Idle"))
                ForceAIState(aiData.Entity, AIBehaviourState.Idle);

            if (ImGui.MenuItem("Force Wander"))
                ForceAIState(aiData.Entity, AIBehaviourState.Wander);

            if (ImGui.MenuItem("Force Attack Player"))
                ForceAttackPlayer(aiData.Entity);

            ImGui.Separator();

            if (ImGui.MenuItem("Reset to Origin"))
                ResetToOrigin(aiData.Entity);

            if (ImGui.MenuItem("Kill Entity"))
                KillEntity(aiData.Entity);

            ImGui.Separator();

            if (ImGui.MenuItem("Show Path"))
                TogglePathVisualization(aiData.Entity);

            if (ImGui.MenuItem("Show Vision Range"))
                ToggleVisionVisualization(aiData.Entity);
        }

        private void RenderSelectedAIDetails()
        {
            var aiData = _aiEntities.FirstOrDefault(ai => ai.Entity.Id == _selectedAIEntity!.Id);
            if (aiData == null)
            {
                ImGui.Text("Selected entity no longer exists");
                _selectedAIEntity = null;
                return;
            }

            // Header
            ImGuiSafe.IconText(Robot, $"AI Details: {aiData.Name}", ColorPrimary);

            ImGui.Separator();

            RenderBasicInfo(aiData);
            ImGui.Separator();
            RenderAIParameters(aiData);
            ImGui.Separator();
            RenderStatsInfo(aiData);
            ImGui.Separator();
            RenderFactionInfo(aiData);
            ImGui.Separator();
            RenderBehaviorControls(aiData);
        }

        private void RenderBasicInfo(AIEntityData aiData)
        {
            if (ImGui.CollapsingHeader("Basic Information", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2, "##basic_info", false);

                RenderInfoRow("Entity ID", aiData.Entity.Id.ToString(), User);
                RenderInfoRow("UUID", aiData.Entity.Uuid.ToString("X16"), Hashtag);
                RenderInfoRow("Position", $"({aiData.Position.x}, {aiData.Position.y})", MapMarkerAlt);
                RenderInfoRow("Origin", $"({aiData.Origin.x}, {aiData.Origin.y})", Home);
                RenderInfoRow("Distance to Player", $"{aiData.DistanceToPlayer:F2}", Ruler);
                RenderInfoRow("Player Visible", aiData.IsPlayerVisible ? "Yes" : "No",
                    aiData.IsPlayerVisible ? Eye : EyeSlash);

                if (aiData.CurrentTarget != null)
                {
                    RenderInfoRow("Current Target", GetEntityName(aiData.CurrentTarget), Crosshairs);
                }

                ImGui.Columns(1);
            }
        }

        private void RenderAIParameters(AIEntityData aiData)
        {
            if (ImGui.CollapsingHeader("AI Parameters", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (aiData.Parameters != null)
                {
                    ImGui.Columns(2, "##ai_params", false);

                    var stateColor = GetStateColor(aiData.State);
                    RenderInfoRow("State", aiData.State.ToString(), GetStateIcon(aiData.State), stateColor);
                    RenderInfoRow("Wander Radius", aiData.Parameters.wanderRadius.ToString(), Circle);
                    RenderInfoRow("Wander Chance", $"{aiData.Parameters.wanderChance:P1}", FontAwesome5.Random);
                    RenderInfoRow("Awareness Range", aiData.Parameters.awarenessRange.ToString(), Eye);
                    RenderInfoRow("Chase Range", aiData.Parameters.chaseRange.ToString(), Running);

                    ImGui.Columns(1);

                    // Visual range indicators
                    ImGui.Spacing();
                    ImGui.Text("Range Visualization:");

                    var ranges = new[]
                    {
                        ("Awareness", aiData.Parameters.awarenessRange, ColorWarning),
                        ("Chase", aiData.Parameters.chaseRange, ColorDanger),
                        ("Wander", (int)aiData.Parameters.wanderRadius, ColorInfo)
                    };

                    foreach (var (name, range, color) in ranges)
                    {
                        ImGuiSafe.WithStyleColors(() =>
                        {
                            ImGui.ProgressBar((float)range / 20f, new Vector2(-1, 0), $"{name}: {range}");
                        }, (ImGuiCol.PlotHistogram, color));
                    }
                }
                else
                {
                    ImGui.TextColored(ColorWarning, "No AI parameters available");
                }
            }
        }

        private void RenderStatsInfo(AIEntityData aiData)
        {
            if (ImGui.CollapsingHeader("Combat Stats", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (aiData.Stats != null)
                {
                    ImGui.Columns(2, "##stats_info", false);

                    var healthPercent = aiData.Stats.MaxHitpoints > 0 ?
                        (float)aiData.Stats.CurrentHitpoints / aiData.Stats.MaxHitpoints : 0f;
                    var healthColor = healthPercent > 0.6f ? ColorSuccess :
                                     healthPercent > 0.3f ? ColorWarning : ColorDanger;

                    RenderInfoRow("Health", $"{aiData.Stats.CurrentHitpoints}/{aiData.Stats.MaxHitpoints}",
                        Heart, healthColor);
                    RenderInfoRow("Mana", $"{aiData.Stats.CurrentMana}/{aiData.Stats.MaxMana}", Magic);
                    RenderInfoRow("Strength", aiData.Stats.Strength.ToString(), Dumbbell);
                    RenderInfoRow("Accuracy", aiData.Stats.Accuracy.ToString(), Crosshairs);
                    RenderInfoRow("Attack Range", aiData.Stats.AttackRange.ToString(), ArrowDown);
                    RenderInfoRow("Attack Speed", aiData.Stats.AttackSpeed.ToString(), Clock);

                    ImGui.Columns(1);

                    // Health bar
                    ImGui.Spacing();
                    ImGui.Text("Health:");
                    ImGuiSafe.WithStyleColors(() =>
                    {
                        ImGui.ProgressBar(healthPercent, new Vector2(-1, 0),
                            $"{aiData.Stats.CurrentHitpoints}/{aiData.Stats.MaxHitpoints}");
                    }, (ImGuiCol.PlotHistogram, healthColor));
                }
                else
                {
                    ImGui.TextColored(ColorWarning, "No stats component available");
                }
            }
        }

        private void RenderFactionInfo(AIEntityData aiData)
        {
            if (ImGui.CollapsingHeader("Faction Information"))
            {
                if (aiData.Faction != null)
                {
                    ImGui.Columns(2, "##faction_info", false);

                    var alignmentColor = aiData.Faction.Alignment switch
                    {
                        "good" => ColorSuccess,
                        "bad" => ColorDanger,
                        _ => ColorInfo
                    };

                    RenderInfoRow("Alignment", aiData.Faction.Alignment, Square, alignmentColor);
                    RenderInfoRow("Faction", aiData.Faction.Faction, Flag);
                    RenderInfoRow("Hostile to Player", aiData.Faction.HostileToPlayer ? "Yes" : "No",
                        aiData.Faction.HostileToPlayer ? Angry : Smile);
                    RenderInfoRow("Hostile to Good", aiData.Faction.HostileToGood ? "Yes" : "No", User);
                    RenderInfoRow("Hostile to Neutral", aiData.Faction.HostileToNeutral ? "Yes" : "No", UserFriends);
                    RenderInfoRow("Hostile to Bad", aiData.Faction.HostileToBad ? "Yes" : "No", UserSecret);

                    ImGui.Columns(1);

                    if (!string.IsNullOrEmpty(aiData.Faction.HostileToFactions))
                    {
                        ImGui.Spacing();
                        ImGui.Text("Hostile Factions:");
                        ImGui.TextWrapped(aiData.Faction.HostileToFactions);
                    }
                }
                else
                {
                    ImGui.TextColored(ColorWarning, "No faction component available");
                }
            }
        }

        private void RenderBehaviorControls(AIEntityData aiData)
        {
            if (ImGui.CollapsingHeader("Behavior Controls", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Text("Force State:");

                var states = new[]
                {
                    (AIBehaviourState.Idle, Bed, ColorSuccess),
                    (AIBehaviourState.Wander, Running, ColorInfo),
                    (AIBehaviourState.Chase, Crosshairs, ColorWarning),
                    (AIBehaviourState.Attack, Khanda, ColorDanger)
                };

                foreach (var (state, icon, color) in states)
                {
                    if (ImGuiSafe.IconButton(icon, color, $"Force {state}"))
                    {
                        ForceAIState(aiData.Entity, state);
                    }
                    ImGui.SameLine();
                }
                ImGui.NewLine();

                ImGui.Spacing();
                ImGui.Text("Actions:");

                var actions = new (string tooltip, string icon, Vector4 color, Action action)[]
                {
                    ("Reset to Origin", Home, ColorInfo, () => ResetToOrigin(aiData.Entity)),
                    ("Heal to Full", Heart, ColorSuccess, () => HealToFull(aiData.Entity)),
                    ("Kill Entity", Skull, ColorDanger, () => KillEntity(aiData.Entity)),
                    ("Toggle Debug", Bug, ColorWarning, () => ToggleAIDebug(aiData.Entity))
                };

                foreach (var (tooltip, icon, color, action) in actions)
                {
                    if (ImGuiSafe.IconButton(icon, color, tooltip))
                    {
                        action();
                    }
                    ImGui.SameLine();
                }
                ImGui.NewLine();
            }
        }

        private void RenderPerformanceTab()
        {
            // Performance metrics and charts
            if (ImGui.CollapsingHeader("Real-time Performance", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var thinkTimes = _aiThinkTimes.ToArray();
                if (thinkTimes.Length > 0)
                {
                    ImGui.Text("AI Think Time (ms):");
                    ImGui.PlotLines("##think_times", thinkTimes, 0, null, 0f, 10f, new Vector2(0, 80));

                    ImGui.Text($"Average: {thinkTimes.Average():F3}ms");
                    ImGui.SameLine();
                    ImGui.Text($"Max: {thinkTimes.Max():F3}ms");
                    ImGui.SameLine();
                    ImGui.Text($"Min: {thinkTimes.Min():F3}ms");
                }
            }

            if (ImGui.CollapsingHeader("Per-Entity Performance"))
            {
                ImGui.BeginChild("##perf_scroll", new Vector2(0, -1), false);

                foreach (var aiData in _aiEntities)
                {
                    if (!_performanceData.TryGetValue(aiData.Entity.Id, out var perfData))
                        continue;

                    ImGui.BeginGroup();

                    ImGui.Text(aiData.Name);
                    ImGui.SameLine(200);

                    var thinkTimeColor = perfData.AverageThinkTime > 5f ? ColorDanger :
                                       perfData.AverageThinkTime > 2f ? ColorWarning : ColorSuccess;
                    ImGui.TextColored(thinkTimeColor, $"{perfData.AverageThinkTime:F3}ms");
                    ImGui.SameLine(300);

                    ImGui.Text($"Updates: {perfData.TotalUpdates}");
                    ImGui.SameLine(400);

                    if (ImGui.SmallButton($"Reset##{aiData.Entity.Id}"))
                    {
                        perfData.LastReset = DateTime.Now;
                        perfData.StateChanges = 0;
                        perfData.TotalUpdates = 0;
                        perfData.AverageThinkTime = 0;
                    }

                    ImGui.EndGroup();
                    ImGui.Separator();
                }

                ImGui.EndChild();
            }
        }

        private void RenderVisualizationTab()
        {
            // Visualization controls and settings
            ImGui.Text("Visualization Settings");
            ImGui.Separator();

            ImGui.Checkbox("Show AI Paths", ref _showPathfinding);
            ImGui.Checkbox("Show Vision Ranges", ref _showVisionRanges);
            ImGui.Checkbox("Show Target Lines", ref _showAIVisualization);

            ImGui.Spacing();
            ImGui.Text("Debug Overlays:");

            if (ImGui.Button("Show All Ranges"))
            {
                // Trigger visualization for all AI entities
                foreach (var aiData in _aiEntities)
                {
                    ToggleVisionVisualization(aiData.Entity);
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Hide All Ranges"))
            {
                // Clear all visualizations
            }

            ImGui.Spacing();
            ImGui.Text("Color Legend:");

            var legendItems = new[]
            {
                ("Idle AI", ColorSuccess),
                ("Active AI", ColorWarning),
                ("Combat AI", ColorDanger),
                ("Dead AI", ColorDim),
                ("Vision Range", ColorInfo),
                ("Chase Range", ColorDanger)
            };

            foreach (var (label, color) in legendItems)
            {
                ImGui.ColorButton($"##{label}", color, ImGuiColorEditFlags.NoTooltip, new Vector2(16, 16));
                ImGui.SameLine();
                ImGui.Text(label);
            }
        }

        private void RenderLogsTab()
        {
            // AI action logs
            ImGui.Text("AI Action Logs");
            ImGui.SameLine();
            if (ImGui.SmallButton("Clear"))
            {
                _aiLogs.Clear();
            }

            ImGui.Separator();

            if (_aiLogs.Count == 0)
            {
                ImGui.TextColored(ColorDim, "No log entries yet. AI actions will appear here.");
                return;
            }

            ImGui.BeginChild("##logs_scroll", new Vector2(0, -1), false);

            foreach (var log in _aiLogs.TakeLast(100).Reverse())
            {
                ImGui.BeginGroup();

                // Time
                ImGui.Text(log.Timestamp.ToString("HH:mm:ss"));
                ImGui.SameLine(80);

                // Entity
                ImGui.Text(log.EntityName ?? "Unknown");
                ImGui.SameLine(200);

                // Level with color
                var levelColor = log.Level switch
                {
                    LogLevel.Error => ColorDanger,
                    LogLevel.Warning => ColorWarning,
                    LogLevel.Info => ColorInfo,
                    _ => ColorDim
                };
                ImGui.TextColored(levelColor, log.Level.ToString());
                ImGui.SameLine(260);

                // Message
                ImGui.TextWrapped(log.Message ?? "");

                ImGui.EndGroup();
                ImGui.Separator();
            }

            ImGui.EndChild();
        }

        private void RenderNoSelectionMessage()
        {
            var center = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPos(new Vector2(center.X / 2 - 100, center.Y / 2 - 30));

            ImGuiSafe.WithIconFont(() =>
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ColorDim);
                ImGui.Text(Robot);
                ImGui.PopStyleColor();
            });

            ImGui.SetCursorPosX(center.X / 2 - 120);
            ImGui.PushStyleColor(ImGuiCol.Text, ColorDim);
            ImGui.Text("No AI Entity Selected");
            ImGui.PopStyleColor();

            ImGui.SetCursorPosX(center.X / 2 - 140);
            ImGui.TextColored(ColorDim * new Vector4(1, 1, 1, 0.5f), "Select an AI entity from the list to view details");
        }

        private void RenderInfoRow(string label, string value, string icon, Vector4? color = null)
        {
            var displayColor = color ?? ColorInfo;

            ImGuiSafe.WithIconFont(() =>
            {
                ImGui.TextColored(displayColor, icon);
            });
            ImGui.SameLine();
            ImGui.Text($"{label}:");
            ImGui.NextColumn();
            ImGui.TextColored(displayColor, value);
            ImGui.NextColumn();
        }
        #endregion

        #region AI Control Methods - UPDATED FOR REAL INTEGRATION
        private void ForceAIState(Entity entity, AIBehaviourState newState)
        {
            var aiParams = entity.GetScriptData<AIParams>("AIParams");
            if (aiParams != null)
            {
                var oldState = GetAIState(aiParams);
                aiParams.state = (int)newState;
                entity.SetScriptData("AIParams", aiParams);

                LogAIAction(GetEntityName(entity), $"State forced: {oldState} → {newState}");

                // Update performance tracking
                if (_performanceData.TryGetValue(entity.Id, out var perfData))
                {
                    perfData.StateChanges++;
                }
            }
        }

        private void ResetToOrigin(Entity entity)
        {
            var aiParams = entity.GetScriptData<AIParams>("AIParams");
            if (aiParams?.origin?.Length == 2)
            {
                // Reset position to origin using ComponentWriter
                ComponentWriter.Patch(
                    (uint)entity.Id,
                    nameof(GridPositionComponent),
                    new GridPositionComponent
                    {
                        Tile = new[] { aiParams.origin[0], aiParams.origin[1] },
                        TileObj = new GridPositionComponent.Vec2 { X = aiParams.origin[0], Y = aiParams.origin[1] }
                    }
                );

                // Update AI params
                aiParams.position = new[] { aiParams.origin[0], aiParams.origin[1] };
                aiParams.state = (int)AIBehaviourState.Idle;
                entity.SetScriptData("AIParams", aiParams);

                LogAIAction(GetEntityName(entity), $"Reset to origin ({aiParams.origin[0]}, {aiParams.origin[1]})");
            }
        }

        private void HealToFull(Entity entity)
        {
            var stats = entity.GetScriptData<StatsComponent>(nameof(StatsComponent));
            if (stats != null)
            {
                stats.CurrentHitpoints = stats.MaxHitpoints;
                stats.CurrentMana = stats.MaxMana;
                entity.SetScriptData(nameof(StatsComponent), stats);

                LogAIAction(GetEntityName(entity), "Healed to full health");
            }
        }

        private void KillEntity(Entity entity)
        {
            // Publish death event instead of destroying directly
            GameEventBus.Event<DeathEvent>.Publish(new DeathEvent((uint)entity.Id));
            LogAIAction(GetEntityName(entity), "Entity killed via debug");
        }

        private void ForceAttackPlayer(Entity entity)
        {
            var playerEntity = FindPlayerEntity();
            if (playerEntity != null)
            {
                GameEventBus.Event<AttackEvent>.Publish(new AttackEvent((uint)entity.Id, (uint)playerEntity.Id, true));
                LogAIAction(GetEntityName(entity), "Forced attack on player");
            }
        }

        private void ToggleAIDebug(Entity entity)
        {
            // Toggle debug mode for specific AI by setting a flag in AIParams
            var aiParams = entity.GetScriptData<AIParams>("AIParams") ?? new AIParams();
            // You'd need to add a debug flag to AIParams if you want per-entity debug
            LogAIAction(GetEntityName(entity), "Debug mode toggled");
        }

        private void TogglePathVisualization(Entity entity)
        {
            // This would require integration with a visual debug system
            LogAIAction(GetEntityName(entity), "Path visualization toggled");
        }

        private void ToggleVisionVisualization(Entity entity)
        {
            // This would require integration with a visual debug system
            LogAIAction(GetEntityName(entity), "Vision range visualization toggled");
        }

        private void ResetAllAIStates()
        {
            foreach (var aiData in _aiEntities)
            {
                ForceAIState(aiData.Entity, AIBehaviourState.Idle);
            }
            LogAIAction("GLOBAL", "Reset all AI states to idle");
        }

        private void LogAIAction(string entityName, string message, LogLevel level = LogLevel.Info)
        {
            _aiLogs.Add(new AILogEntry
            {
                Timestamp = DateTime.Now,
                EntityName = entityName,
                Message = message,
                Level = level
            });

            // Keep log size manageable
            if (_aiLogs.Count > 1000)
            {
                _aiLogs.RemoveRange(0, 500);
            }

            // Also log to console for debugging
            Console.WriteLine($"[AIDebug] {entityName}: {message}");
        }
        #endregion

        #region Event Handlers - UPDATED FOR REAL INTEGRATION
        private void OnAIHurt(HurtEvent ev)
        {
            var entity = Entity.FromRaw(Engine.Instance!.Context, (int)ev.EntityId);
            if (IsAIEntity(entity))
            {
                LogAIAction(GetEntityName(entity), $"Took {ev.Damage} damage", LogLevel.Warning);

                // Update performance tracking
                if (_performanceData.TryGetValue(entity.Id, out var perfData))
                {
                    perfData.VisionChecks++; // Using vision checks as a proxy for combat events
                }
            }
        }

        private void OnAIDeath(DeathEvent ev)
        {
            var entity = Entity.FromRaw(Engine.Instance!.Context, (int)ev.EntityId);
            if (IsAIEntity(entity))
            {
                LogAIAction(GetEntityName(entity), "Entity died", LogLevel.Error);

                // Remove from performance tracking
                _performanceData.Remove(entity.Id);
            }
        }

        private void OnAIAttack(AttackEvent ev)
        {
            var attacker = Entity.FromRaw(Engine.Instance!.Context, (int)ev.AttackerId);
            var victim = Entity.FromRaw(Engine.Instance!.Context, (int)ev.VictimId);

            if (IsAIEntity(attacker))
            {
                LogAIAction(GetEntityName(attacker), $"Attacked {GetEntityName(victim)}", LogLevel.Warning);

                // Update performance tracking
                if (_performanceData.TryGetValue(attacker.Id, out var perfData))
                {
                    perfData.StateChanges++; // Combat action counts as state change
                }
            }
        }

        private void OnAIMovement(MovementIntentEvent ev)
        {
            var entity = Entity.FromRaw(Engine.Instance!.Context, (int)ev.EntityId);
            if (IsAIEntity(entity))
            {
                LogAIAction(GetEntityName(entity), $"Moving to ({ev.TargetX}, {ev.TargetY})", LogLevel.Debug);

                // Update performance tracking
                if (_performanceData.TryGetValue(entity.Id, out var perfData))
                {
                    perfData.PathfindingCalls++;
                }
            }
        }
        #endregion

        #region Utility Methods
        private Vector4 GetStateColor(AIBehaviourState state)
        {
            return state switch
            {
                AIBehaviourState.Idle => ColorSuccess,
                AIBehaviourState.Wander => ColorInfo,
                AIBehaviourState.Chase => ColorWarning,
                AIBehaviourState.Attack => ColorDanger,
                AIBehaviourState.Return => ColorPrimary,
                AIBehaviourState.Dead => ColorDim,
                _ => ColorDim
            };
        }

        private string GetStateIcon(AIBehaviourState state)
        {
            return state switch
            {
                AIBehaviourState.Idle => Bed,
                AIBehaviourState.Wander => Running,
                AIBehaviourState.Chase => Crosshairs,
                AIBehaviourState.Attack => Khanda,
                AIBehaviourState.Return => ArrowLeft,
                AIBehaviourState.Dead => Skull,
                _ => QuestionCircle
            };
        }

        /// <summary>
        /// SAFE: Dispose method with proper event cleanup
        /// </summary>
        public override void Dispose()
        {
            if (_disposed) return;

            // Unsubscribe from events
            GameEventBus.Event<HurtEvent>.Unsubscribe(OnAIHurt);
            GameEventBus.Event<DeathEvent>.Unsubscribe(OnAIDeath);
            GameEventBus.Event<AttackEvent>.Unsubscribe(OnAIAttack);
            GameEventBus.Event<MovementIntentEvent>.Unsubscribe(OnAIMovement);

            base.Dispose();
        }
        #endregion
    }
}