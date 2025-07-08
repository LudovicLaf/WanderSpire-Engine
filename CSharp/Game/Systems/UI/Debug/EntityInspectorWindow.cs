// File: Game/Systems/UI/EntityInspectorWindow.cs
using Game.Dto;
using ScriptHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using WanderSpire.Components;
using WanderSpire.Scripting;
using WanderSpire.Scripting.UI;
using WanderSpire.Scripting.Utils;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Professional-grade entity inspector window with modern UI and advanced features.
    /// </summary>
    public class EntityInspectorWindow : ImGuiWindowBase, IDisposable
    {
        public override string Title => "Entity Inspector";

        // Entity selection
        private Entity? _selectedEntity;
        private int _selectedEntityId = -1;
        private readonly List<Entity> _entityCache = new();
        private DateTime _lastCacheUpdate = DateTime.MinValue;

        // UI State
        private string _searchFilter = "";
        private EntityFilterType _filterType = EntityFilterType.All;
        private bool _autoRefresh = true;
        private bool _showRawData = false;
        private int _selectedTab = 0;
        private readonly Dictionary<string, bool> _componentExpanded = new();
        private readonly Dictionary<string, object> _editHistory = new();

        // Component editing
        private readonly Dictionary<string, string> _editBuffers = new();
        private readonly HashSet<string> _modifiedComponents = new();
        private readonly Dictionary<string, DateTime> _lastModified = new();

        // Visual customization
        private readonly Dictionary<string, string> _componentIcons = new()
        {
            { nameof(PlayerTagComponent)    , HospitalUser               }, // \uf007
            { nameof(StatsComponent)        , Icons           }, // \uf44b
            { nameof(GridPositionComponent) , IceCream       }, // \uf3c5
            { nameof(TransformComponent)    , Hotdog               }, // \uf021
            { nameof(SpriteComponent)       , PaintBrush         }, // \uf1fc
            { nameof(AnimationStateComponent), Film             }, // \uf008
            { nameof(FactionComponent)      , BalanceScale       }, // \uf24e
            { nameof(ObstacleComponent)     , Road               }, // \uf018
            { nameof(PrefabIdComponent)     , Box                }, // \uf466
            { nameof(IDComponent)           , Hashtag            }, // \uf292
        };

        // Theme colors
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorDanger = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);
        private readonly Vector4 ColorAccent = new(0.75f, 0.45f, 0.98f, 1.0f);
        private readonly Vector4 ColorBackground = new(0.08f, 0.08f, 0.10f, 1.0f);

        private readonly string[] _knownComponents = new[]
        {
            nameof(GridPositionComponent),
            nameof(StatsComponent),
            nameof(FactionComponent),
            nameof(AnimationStateComponent),
            nameof(PlayerTagComponent),
            nameof(SpriteComponent),
            nameof(SpriteAnimationComponent),
            nameof(AnimationClipsComponent),
            nameof(FacingComponent),
            nameof(PrefabIdComponent),
            nameof(IDComponent),
            nameof(TransformComponent),
            nameof(ObstacleComponent),
            nameof(TagComponent),
            nameof(TargetGridPositionComponent)
        };

        private readonly string[] _tabNames = { "Components", "Script Data", "Raw Data", "History" };

        public override void Render()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16, 16));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 6.0f);

            if (!BeginWindow())
            {
                ImGui.PopStyleVar(3);
                EndWindow();
                return;
            }

            RenderHeader();
            ImGui.Separator();

            // Main content area with splitter
            var contentRegion = ImGui.GetContentRegionAvail();
            float leftPanelWidth = Math.Min(300, contentRegion.X * 0.35f);

            // Left panel - Entity list
            RenderEntityListPanel(leftPanelWidth, contentRegion.Y - 10);

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Separator, ColorPrimary * new Vector4(1, 1, 1, 0.3f));
            ImGuiExtensions.SeparatorEx(ImGuiSeparatorFlags.Vertical);
            ImGui.PopStyleColor();
            ImGui.SameLine();

            // Right panel - Entity details
            if (_selectedEntity != null && _selectedEntity.IsValid)
            {
                RenderEntityDetailsPanel(contentRegion.X - leftPanelWidth - 20, contentRegion.Y - 10);
            }
            else
            {
                RenderNoSelectionPanel();
            }

            ImGui.PopStyleVar(3);
            EndWindow();

            // Auto-refresh
            if (_autoRefresh && (DateTime.Now - _lastCacheUpdate).TotalSeconds > 0.5)
            {
                RefreshEntityCache();
            }
        }

        private void RenderHeader()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text("Entity Inspector" + Peace);
            ImGui.PopStyleColor();

            ImGui.SameLine();
            ImGui.TextColored(ColorDim, $"({_entityCache.Count} entities)");

            ImGui.SameLine(ImGui.GetWindowWidth() - 200);

            // Header buttons
            if (ImGuiButton("↻", ColorSuccess, "Refresh (Auto: " + (_autoRefresh ? "ON" : "OFF") + ")"))
            {
                _autoRefresh = !_autoRefresh;
                if (_autoRefresh) RefreshEntityCache();
            }

            ImGui.SameLine();
            if (RenderIconButton(Clipboard, ColorInfo, "Copy entity data"))
                CopyEntityToClipboard();

            ImGui.SameLine();
            if (RenderIconButton(Trash, ColorDanger, "Clear selection"))
            {
                _selectedEntity = null;
                _selectedEntityId = -1;
            }
        }

        private bool RenderIconButton(string icon, Vector4 color, string tooltip = null)
        {
            // Switch to FontAwesome
            ImGuiManager.Instance?.PushIconFont();

            // Tint the button and text
            ImGui.PushStyleColor(ImGuiCol.Button, color * new Vector4(1, 1, 1, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));

            // Measure glyph size
            Vector2 textSize = ImGui.CalcTextSize(icon);
            // Add a bit of padding so it’s easy to click
            Vector2 btnSize = new Vector2(textSize.X + 8, textSize.Y + 4);

            // Use the icon itself as the button’s label (never empty)
            bool clicked = ImGui.Button(icon, btnSize);

            // Optional tooltip
            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            // Restore colors and font
            ImGui.PopStyleColor(3);
            ImGuiManager.Instance?.PopIconFont();

            return clicked;
        }


        private bool ImGuiButton(string label, Vector4 color, string tooltip = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, color * new Vector4(1, 1, 1, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));

            bool clicked = ImGui.Button(label);

            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            ImGui.PopStyleColor(3);
            return clicked;
        }

        private void RenderEntityListPanel(float width, float height)
        {
            ImGui.BeginChild("##entity_list", new Vector2(width, height), true);

            // Search and filter
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0, 0, 0.3f));
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##search", ref _searchFilter, 128))
                RefreshEntityCache();
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Search by ID, name, or component");

            // Filter buttons
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 3));
            if (RenderFilterButton("All", EntityFilterType.All)) RefreshEntityCache();
            ImGui.SameLine();
            if (RenderFilterButton("Player", EntityFilterType.Player)) RefreshEntityCache();
            ImGui.SameLine();
            if (RenderFilterButton("NPCs", EntityFilterType.NPCs)) RefreshEntityCache();
            ImGui.SameLine();
            if (RenderFilterButton("Items", EntityFilterType.Items)) RefreshEntityCache();
            ImGui.PopStyleVar();

            ImGui.Separator();

            // Entity list
            ImGui.BeginChild("##entities", new Vector2(-1, -1));

            var filteredEntities = GetFilteredEntities();
            foreach (var entity in filteredEntities)
            {
                RenderEntityListItem(entity);
            }

            ImGui.EndChild();
            ImGui.EndChild();
        }

        private void RenderEntityListItem(Entity entity)
        {
            bool isSelected = _selectedEntity?.Id == entity.Id;
            string icon = GetEntityIcon(entity);
            string name = GetEntityDisplayName(entity);

            ImGui.PushID(entity.Id.ToString());

            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Header, ColorPrimary * new Vector4(1, 1, 1, 0.3f));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ColorPrimary * new Vector4(1, 1, 1, 0.4f));
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, ColorPrimary * new Vector4(1, 1, 1, 0.5f));
            }

            // Render icon with font, then name
            ImGuiManager.Instance?.PushIconFont();
            ImGui.Text(icon);
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            if (ImGui.Selectable(name, isSelected))
            {
                _selectedEntity = entity;
                _selectedEntityId = entity.Id;
                _editBuffers.Clear();
                _modifiedComponents.Clear();
            }

            if (isSelected)
                ImGui.PopStyleColor(3);

            // Context menu
            if (ImGui.BeginPopupContextItem($"##ctx_{entity.Id}"))
            {
                if (ImGui.MenuItem("Copy ID"))
                    Console.WriteLine($"Entity ID: {entity.Id}");
                if (ImGui.MenuItem("Copy UUID"))
                    Console.WriteLine($"Entity UUID: {entity.Uuid:X16}");
                ImGui.Separator();
                if (ImGui.MenuItem("Destroy", null, false, !entity.HasComponent(nameof(PlayerTagComponent))))
                {
                    Engine.Instance?.DestroyEntity(entity);
                    if (_selectedEntity?.Id == entity.Id)
                    {
                        _selectedEntity = null;
                        _selectedEntityId = -1;
                    }
                }
                ImGui.EndPopup();
            }

            ImGui.PopID();
        }

        private void RenderEntityDetailsPanel(float width, float height)
        {
            ImGui.BeginChild("##entity_details", new Vector2(width, height), false);

            // Entity header
            RenderEntityHeader();

            ImGui.Separator();

            // Tab bar
            ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 4.0f);
            if (ImGui.BeginTabBar("##entity_tabs"))
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
            ImGui.PopStyleVar();

            // Tab content
            ImGui.BeginChild("##tab_content", new Vector2(-1, -50));
            switch (_selectedTab)
            {
                case 0: RenderComponentsTab(); break;
                case 1: RenderScriptDataTab(); break;
                case 2: RenderRawDataTab(); break;
                case 3: RenderHistoryTab(); break;
            }
            ImGui.EndChild();

            // Action bar
            ImGui.Separator();
            RenderActionBar();

            ImGui.EndChild();
        }

        private void RenderEntityHeader()
        {
            var entity = _selectedEntity!;
            string icon = GetEntityIcon(entity);
            string name = GetEntityDisplayName(entity);

            // Render icon with font, then name
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(icon);
            ImGui.PopStyleColor();
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(name);
            ImGui.PopStyleColor();

            ImGui.SameLine();
            ImGui.TextColored(ColorDim, $"ID: {entity.Id}");

            ImGui.TextColored(ColorDim, $"UUID: {entity.Uuid:X16}");

            // Quick stats
            if (entity.HasComponent(nameof(StatsComponent)))
            {
                var stats = entity.GetScriptData<StatsComponent>(nameof(StatsComponent));
                if (stats != null)
                {
                    float hpPercent = (float)stats.CurrentHitpoints / stats.MaxHitpoints;
                    Vector4 hpColor = hpPercent > 0.5f ? ColorSuccess : (hpPercent > 0.25f ? ColorWarning : ColorDanger);

                    ImGui.TextColored(hpColor, $"HP: {stats.CurrentHitpoints}/{stats.MaxHitpoints}");
                    ImGui.SameLine();
                    ImGui.TextColored(ColorInfo, $"MP: {stats.CurrentMana}/{stats.MaxMana}");
                }
            }

            if (entity.HasComponent(nameof(GridPositionComponent)))
            {
                var pos = entity.GetComponent<GridPositionComponent>(nameof(GridPositionComponent));
                if (pos != null)
                {
                    ImGui.SameLine();
                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.TextColored(ColorAccent, MapMarkerAlt);
                    ImGuiManager.Instance?.PopIconFont();
                    ImGui.SameLine();
                    ImGui.TextColored(ColorAccent, $"({pos.Tile[0]}, {pos.Tile[1]})");
                }
            }
        }

        private void RenderComponentsTab()
        {
            // First render native components
            foreach (var componentName in _knownComponents)
            {
                if (_selectedEntity!.HasComponent(componentName))
                {
                    RenderComponentPanel(componentName);
                }
            }

            // Then render managed components stored in script data
            RenderManagedComponents();
        }

        private void RenderManagedComponents()
        {
            // Check for StatsComponent in script data
            var stats = _selectedEntity!.GetScriptData<StatsComponent>(nameof(StatsComponent));
            if (stats != null)
            {
                RenderManagedComponentPanel(nameof(StatsComponent), () => RenderStatsComponent());
            }

            // Check for FactionComponent in script data
            var faction = _selectedEntity.GetScriptData<FactionComponent>(nameof(FactionComponent));
            if (faction != null)
            {
                RenderManagedComponentPanel(nameof(FactionComponent), () => RenderFactionComponent());
            }

            // Check for AIParams in script data
            var aiParams = _selectedEntity.GetScriptData<AIParams>("AIParams");
            if (aiParams != null)
            {
                RenderManagedComponentPanel("AIParams", () => RenderAIParamsPanel(aiParams));
            }
        }

        private void RenderManagedComponentPanel(string componentName, Action renderContent)
        {
            string icon = _componentIcons.GetValueOrDefault(componentName, Cog);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 4));

            // Render icon with font, then component name with "(Managed)"
            ImGuiManager.Instance?.PushIconFont();
            ImGui.Text(icon);
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            bool expanded = ImGui.CollapsingHeader($"{componentName} (Managed)");

            ImGui.PopStyleVar();

            if (expanded)
            {
                ImGui.Indent();
                ImGui.PushID(componentName);

                try
                {
                    renderContent();
                }
                catch (Exception ex)
                {
                    ImGui.TextColored(ColorDanger, $"Error: {ex.Message}");
                }

                ImGui.PopID();
                ImGui.Unindent();
            }
        }

        private void RenderComponentPanel(string componentName)
        {
            string icon = _componentIcons.GetValueOrDefault(componentName, Cog);
            bool isModified = _modifiedComponents.Contains(componentName);

            if (isModified)
            {
                ImGui.PushStyleColor(ImGuiCol.Header, ColorWarning * new Vector4(1, 1, 1, 0.2f));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ColorWarning * new Vector4(1, 1, 1, 0.3f));
            }

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 4));

            // Render icon with font, then component name
            ImGuiManager.Instance?.PushIconFont();
            ImGui.Text(icon);
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            bool expanded = ImGui.CollapsingHeader(componentName);

            ImGui.PopStyleVar();

            if (isModified)
            {
                ImGui.PopStyleColor(2);
                ImGui.SameLine();
                ImGui.TextColored(ColorWarning, "[Modified]");
            }

            if (expanded)
            {
                ImGui.Indent();
                ImGui.PushID(componentName);

                try
                {
                    RenderComponentContent(componentName);
                }
                catch (Exception ex)
                {
                    ImGui.TextColored(ColorDanger, $"Error: {ex.Message}");
                }

                ImGui.PopID();
                ImGui.Unindent();
            }
        }

        private void RenderComponentContent(string componentName)
        {
            switch (componentName)
            {
                case nameof(GridPositionComponent):
                    RenderGridPositionComponent();
                    break;

                case nameof(StatsComponent):
                    RenderStatsComponent();
                    break;

                case nameof(FactionComponent):
                    RenderFactionComponent();
                    break;

                case nameof(AnimationStateComponent):
                    RenderAnimationStateComponent();
                    break;

                case nameof(TransformComponent):
                    RenderTransformComponent();
                    break;

                case nameof(SpriteComponent):
                    RenderSpriteComponent();
                    break;

                case nameof(SpriteAnimationComponent):
                    RenderSpriteAnimationComponent();
                    break;

                case nameof(AnimationClipsComponent):
                    RenderAnimationClipsComponent();
                    break;

                case nameof(FacingComponent):
                    RenderFacingComponent();
                    break;

                case nameof(PrefabIdComponent):
                    RenderPrefabIdComponent();
                    break;

                case nameof(IDComponent):
                    RenderIDComponent();
                    break;

                case nameof(PlayerTagComponent):
                    ImGui.TextColored(ColorSuccess, "This is the player entity");
                    break;

                default:
                    RenderGenericComponent(componentName);
                    break;
            }
        }

        private void RenderGridPositionComponent()
        {
            var comp = _selectedEntity!.GetComponent<GridPositionComponent>(nameof(GridPositionComponent));
            if (comp == null) return;

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 3));

            int[] pos = { comp.Tile[0], comp.Tile[1] };
            bool changed = false;

            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt("X", ref pos[0]))
            {
                comp.Tile[0] = pos[0];
                changed = true;
            }

            ImGui.SetNextItemWidth(100);
            if (ImGui.InputInt("Y", ref pos[1]))
            {
                comp.Tile[1] = pos[1];
                changed = true;
            }

            // Immediately apply changes
            if (changed)
            {
                UpdateComponent(nameof(GridPositionComponent), comp);
            }

            ImGui.PopStyleVar();
        }

        private void RenderStatsComponent()
        {
            var stats = _selectedEntity!.GetScriptData<StatsComponent>(nameof(StatsComponent));
            if (stats == null) return;

            bool changed = false;

            // HP Bar
            float hpPercent = (float)stats.CurrentHitpoints / stats.MaxHitpoints;
            Vector4 hpColor = hpPercent > 0.5f ? ColorSuccess : (hpPercent > 0.25f ? ColorWarning : ColorDanger);
            ImGui.Text("Health");
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, hpColor);
            ImGui.ProgressBar(hpPercent, new Vector2(-1, 20), $"{stats.CurrentHitpoints}/{stats.MaxHitpoints}");
            ImGui.PopStyleColor();

            // MP Bar
            if (stats.MaxMana > 0)
            {
                float mpPercent = (float)stats.CurrentMana / stats.MaxMana;
                ImGui.Text("Mana");
                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, ColorInfo);
                ImGui.ProgressBar(mpPercent, new Vector2(-1, 20), $"{stats.CurrentMana}/{stats.MaxMana}");
                ImGui.PopStyleColor();
            }

            ImGui.Separator();

            // Editable values
            ImGui.Columns(2, "##stats", false);

            // Current HP
            int currentHP = stats.CurrentHitpoints;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputInt("Current HP", ref currentHP))
            {
                currentHP = Math.Clamp(currentHP, 0, stats.MaxHitpoints);
                stats.CurrentHitpoints = currentHP;
                changed = true;
            }

            // Max HP
            int maxHP = stats.MaxHitpoints;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputInt("Max HP", ref maxHP))
            {
                maxHP = Math.Max(1, maxHP);
                stats.MaxHitpoints = maxHP;
                changed = true;
            }

            ImGui.NextColumn();

            // Current MP
            int currentMP = stats.CurrentMana;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputInt("Current MP", ref currentMP))
            {
                currentMP = Math.Clamp(currentMP, 0, stats.MaxMana);
                stats.CurrentMana = currentMP;
                changed = true;
            }

            // Max MP
            int maxMP = stats.MaxMana;
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputInt("Max MP", ref maxMP))
            {
                maxMP = Math.Max(0, maxMP);
                stats.MaxMana = maxMP;
                changed = true;
            }

            ImGui.Columns(1);
            ImGui.Separator();

            // Combat stats (read-only for now)
            ImGui.Text("Combat Stats");
            ImGui.Columns(2, "##combat", false);

            ImGui.Text($"Strength: {stats.Strength}");
            ImGui.Text($"Accuracy: {stats.Accuracy}");
            ImGui.Text($"Attack Speed: {stats.AttackSpeed}");

            ImGui.NextColumn();

            ImGui.Text($"Attack Type: {stats.AttackType}");
            ImGui.Text($"Attack Range: {stats.AttackRange:F1}");

            ImGui.Columns(1);

            // Quick actions
            ImGui.Separator();
            if (ImGuiButton("Full Heal", ColorSuccess))
            {
                stats.CurrentHitpoints = stats.MaxHitpoints;
                stats.CurrentMana = stats.MaxMana;
                changed = true;
            }

            ImGui.SameLine();
            if (ImGuiButton("Damage -10", ColorWarning))
            {
                stats.CurrentHitpoints = Math.Max(0, stats.CurrentHitpoints - 10);
                changed = true;
            }

            ImGui.SameLine();
            if (ImGuiButton("Kill", ColorDanger))
            {
                stats.CurrentHitpoints = 0;
                changed = true;
            }

            if (changed)
            {
                _selectedEntity.SetScriptData(nameof(StatsComponent), stats);
                _lastModified[nameof(StatsComponent)] = DateTime.Now;
            }
        }

        private void RenderFactionComponent()
        {
            var faction = _selectedEntity!.GetScriptData<FactionComponent>(nameof(FactionComponent));
            if (faction == null) return;

            bool changed = false;

            ImGui.Columns(2, "##faction", false);

            ImGui.Text("Alignment:");
            ImGui.NextColumn();
            ImGui.TextColored(GetAlignmentColor(faction.Alignment), faction.Alignment);
            ImGui.NextColumn();

            ImGui.Text("Faction:");
            ImGui.NextColumn();
            ImGui.Text(faction.Faction ?? "None");
            ImGui.NextColumn();

            ImGui.Columns(1);
            ImGui.Separator();

            ImGui.Text("Hostility:");
            ImGui.Indent();

            // Use lambda functions to capture changes and immediately apply them
            if (RenderHostilityCheckbox("Player", faction.HostileToPlayer, v => { faction.HostileToPlayer = v; changed = true; }))
                changed = true;
            if (RenderHostilityCheckbox("Good", faction.HostileToGood, v => { faction.HostileToGood = v; changed = true; }))
                changed = true;
            if (RenderHostilityCheckbox("Neutral", faction.HostileToNeutral, v => { faction.HostileToNeutral = v; changed = true; }))
                changed = true;
            if (RenderHostilityCheckbox("Bad", faction.HostileToBad, v => { faction.HostileToBad = v; changed = true; }))
                changed = true;

            if (!string.IsNullOrEmpty(faction.HostileToFactions))
            {
                ImGui.Text($"Hostile Factions: {faction.HostileToFactions}");
            }

            ImGui.Unindent();

            // Immediately apply changes
            if (changed)
            {
                _selectedEntity.SetScriptData(nameof(FactionComponent), faction);
                _lastModified[nameof(FactionComponent)] = DateTime.Now;
            }
        }

        private void RenderAnimationStateComponent()
        {
            var anim = _selectedEntity!.GetComponent<AnimationStateComponent>(nameof(AnimationStateComponent));
            if (anim == null) return;

            ImGui.Text($"Current State: ");
            ImGui.SameLine();
            ImGui.TextColored(ColorInfo, anim.state);

            ImGui.Separator();

            // Quick state buttons
            string[] quickStates = { "Idle", "Walk", "AttackHorizontal", "AttackVertical", "Hurt", "Death" };
            int buttonsPerRow = 3;

            for (int i = 0; i < quickStates.Length; i++)
            {
                if (i % buttonsPerRow != 0) ImGui.SameLine();

                bool isCurrent = anim.state == quickStates[i];
                if (isCurrent) ImGui.PushStyleColor(ImGuiCol.Button, ColorPrimary * new Vector4(1, 1, 1, 0.5f));

                if (ImGui.Button(quickStates[i]))
                {
                    SetAnimationState(quickStates[i]);
                    anim.state = quickStates[i];
                }

                if (isCurrent) ImGui.PopStyleColor();
            }

            // Custom state input
            ImGui.Separator();
            string customState = anim.state;
            ImGui.SetNextItemWidth(-80);
            if (ImGui.InputText("##state", ref customState, 64))
            {
                _editBuffers[$"anim_state_{_selectedEntity.Id}"] = customState;
            }
            ImGui.SameLine();
            if (ImGui.Button("Set"))
            {
                SetAnimationState(customState);
            }
        }

        private void RenderTransformComponent()
        {
            var transform = _selectedEntity!.GetComponent<TransformComponent>(nameof(TransformComponent));
            if (transform == null) return;

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 3));

            bool changed = false;

            // Position
            float[] pos = { transform.LocalPosition[0], transform.LocalPosition[1] };
            ImGui.SetNextItemWidth(200);
            if (ImGui.DragFloat2("Position", ref pos[0], 0.1f))
            {
                transform.LocalPosition[0] = pos[0];
                transform.LocalPosition[1] = pos[1];
                changed = true;
            }

            // Rotation (use a local variable)
            float rotation = transform.LocalRotation;
            ImGui.SetNextItemWidth(200);
            if (ImGui.DragFloat("Rotation", ref rotation, 1.0f, 0, 360))
            {
                transform.LocalRotation = rotation;
                changed = true;
            }

            // Scale
            float[] scale = { transform.LocalScale[0], transform.LocalScale[1] };
            ImGui.SetNextItemWidth(200);
            if (ImGui.DragFloat2("Scale", ref scale[0], 0.01f, 0.1f, 10.0f))
            {
                transform.LocalScale[0] = scale[0];
                transform.LocalScale[1] = scale[1];
                changed = true;
            }

            // Immediately apply changes
            if (changed)
            {
                UpdateComponent(nameof(TransformComponent), transform);
            }

            ImGui.PopStyleVar();
        }

        private void RenderSpriteComponent()
        {
            var sprite = _selectedEntity!.GetComponent<SpriteComponent>(nameof(SpriteComponent));
            if (sprite == null) return;

            ImGui.Columns(2, "##sprite", false);

            ImGui.Text("Atlas:");
            ImGui.NextColumn();
            ImGui.TextColored(ColorInfo, sprite.AtlasName ?? "None");
            ImGui.NextColumn();

            ImGui.Text("Frame:");
            ImGui.NextColumn();
            ImGui.TextColored(ColorInfo, sprite.FrameName ?? "None");
            ImGui.NextColumn();

            ImGui.Columns(1);
        }

        private void RenderSpriteAnimationComponent()
        {
            var anim = _selectedEntity!.GetComponent<SpriteAnimationComponent>(nameof(SpriteAnimationComponent));
            if (anim == null) return;

            ImGui.Columns(2, "##spriteanim", false);

            ImGui.Text("Frame Range:");
            ImGui.NextColumn();
            ImGui.Text($"{anim.StartFrame} - {anim.StartFrame + anim.FrameCount - 1}");
            ImGui.NextColumn();

            ImGui.Text("Frame Duration:");
            ImGui.NextColumn();
            ImGui.Text($"{anim.FrameDuration:F3}s");
            ImGui.NextColumn();

            ImGui.Text("Loop:");
            ImGui.NextColumn();
            ImGui.TextColored(anim.Loop ? ColorSuccess : ColorDim, anim.Loop ? "Yes" : "No");
            ImGui.NextColumn();

            ImGui.Text("Frame Size:");
            ImGui.NextColumn();
            ImGui.Text($"{anim.FrameWidth}x{anim.FrameHeight}");
            ImGui.NextColumn();

            ImGui.Text("World Size:");
            ImGui.NextColumn();
            ImGui.Text($"{anim.WorldWidth}x{anim.WorldHeight}");
            ImGui.NextColumn();

            ImGui.Columns(1);
        }

        private void RenderAnimationClipsComponent()
        {
            var clips = _selectedEntity!.GetComponent<AnimationClipsComponent>(nameof(AnimationClipsComponent));
            if (clips?.Clips == null) return;

            ImGui.Text($"Total Clips: {clips.Clips.Count}");
            ImGui.Separator();

            foreach (var kvp in clips.Clips)
            {
                var clip = kvp.Value;

                ImGui.PushStyleColor(ImGuiCol.Header, ColorAccent * new Vector4(1, 1, 1, 0.2f));

                // Render icon with font, then clip name
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(Film);
                ImGuiManager.Instance?.PopIconFont();

                ImGui.SameLine();
                if (ImGui.CollapsingHeader(kvp.Key))
                {
                    ImGui.Indent();
                    ImGui.Columns(2, $"##clip_{kvp.Key}", false);

                    ImGui.Text("Frames:");
                    ImGui.NextColumn();
                    ImGui.Text($"{clip.StartFrame} - {clip.StartFrame + clip.FrameCount - 1}");
                    ImGui.NextColumn();

                    ImGui.Text("Duration:");
                    ImGui.NextColumn();
                    ImGui.Text($"{clip.FrameDuration:F3}s");
                    ImGui.NextColumn();

                    ImGui.Text("Loop:");
                    ImGui.NextColumn();
                    ImGui.TextColored(clip.Loop ? ColorSuccess : ColorDim, clip.Loop ? "Yes" : "No");
                    ImGui.NextColumn();

                    ImGui.Columns(1);
                    ImGui.Unindent();
                }
                ImGui.PopStyleColor();
            }
        }

        private void RenderFacingComponent()
        {
            var facing = _selectedEntity!.GetComponent<FacingComponent>(nameof(FacingComponent));
            if (facing == null) return;

            string[] directionLabels = { "Right", "Left", "Down", "Up" };
            string[] directionIcons = { ArrowRight, ArrowLeft, ArrowDown, ArrowUp };
            string currentDir = facing.Facing < directionLabels.Length ? directionLabels[facing.Facing] : "Unknown";

            ImGui.Text($"Direction: {currentDir}");
            ImGui.Separator();

            // Direction buttons with FontAwesome icons
            for (int i = 0; i < directionIcons.Length; i++)
            {
                if (i % 2 != 0) ImGui.SameLine();

                bool isCurrent = facing.Facing == i;
                if (isCurrent) ImGui.PushStyleColor(ImGuiCol.Button, ColorPrimary * new Vector4(1, 1, 1, 0.5f));

                if (RenderIconButton(directionIcons[i], isCurrent ? ColorPrimary : ColorDim, directionLabels[i]))
                {
                    facing.Facing = i;
                    UpdateComponent(nameof(FacingComponent), facing);
                }

                if (isCurrent) ImGui.PopStyleColor();
            }
        }

        private void RenderPrefabIdComponent()
        {
            var prefab = _selectedEntity!.GetComponent<PrefabIdComponent>(nameof(PrefabIdComponent));
            if (prefab == null) return;

            ImGui.Columns(2, "##prefab", false);

            ImGui.Text("Prefab ID:");
            ImGui.NextColumn();
            ImGui.TextColored(ColorInfo, prefab.PrefabId.ToString());
            ImGui.NextColumn();

            ImGui.Text("Prefab Name:");
            ImGui.NextColumn();
            ImGui.TextColored(ColorAccent, prefab.PrefabName ?? "Unknown");
            ImGui.NextColumn();

            ImGui.Columns(1);
        }

        private void RenderIDComponent()
        {
            var id = _selectedEntity!.GetComponent<IDComponent>(nameof(IDComponent));
            if (id == null) return;

            ImGui.Text("UUID:");
            ImGui.SameLine();
            ImGui.TextColored(ColorInfo, $"{id.Uuid:X16}");

            ImGui.SameLine();
            if (RenderIconButton(Clipboard, ColorPrimary, "Copy UUID"))
            {
                Console.WriteLine($"UUID: {id.Uuid:X16}");
            }
        }

        private void RenderGenericComponent(string componentName)
        {
            try
            {
                var buffer = new byte[4096];
                int len = EngineInterop.GetComponentJson(
                    Engine.Instance!.Context,
                    new EntityId { id = (uint)_selectedEntity!.Id },
                    componentName, buffer, buffer.Length);

                if (len > 0)
                {
                    string json = Encoding.UTF8.GetString(buffer, 0, len);

                    // Pretty print JSON
                    try
                    {
                        var doc = JsonDocument.Parse(json);
                        json = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                    }
                    catch { }

                    ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0, 0, 0.3f));
                    ImGui.InputTextMultiline("##json", ref json, (uint)json.Length,
                        new Vector2(-1, 100), ImGuiInputTextFlags.ReadOnly);
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.TextColored(ColorDim, "No data available");
                }
            }
            catch (Exception ex)
            {
                ImGui.TextColored(ColorDanger, $"Error: {ex.Message}");
            }
        }

        private void RenderScriptDataTab()
        {
            var aiParams = _selectedEntity!.GetScriptData<AIParams>("AIParams");
            if (aiParams != null)
            {
                RenderAIParamsPanel(aiParams);
            }

            var origin = _selectedEntity.GetScriptData<int[]>("origin");
            if (origin?.Length == 2)
            {
                ImGui.Text("Origin:");
                ImGui.SameLine();
                ImGui.TextColored(ColorInfo, $"({origin[0]}, {origin[1]})");
            }

            var scripts = _selectedEntity.GetScriptData<string[]>("scripts");
            if (scripts?.Length > 0)
            {
                ImGui.Separator();
                ImGui.Text("Attached Scripts:");
                ImGui.Indent();
                foreach (var script in scripts)
                {
                    ImGui.TextColored(ColorAccent, $"• {script}");
                }
                ImGui.Unindent();
            }
        }

        private void RenderAIParamsPanel(AIParams ai)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorAccent * new Vector4(1, 1, 1, 0.2f));

            // Render icon with font, then header text
            ImGuiManager.Instance?.PushIconFont();
            ImGui.Text(Robot);
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            if (ImGui.CollapsingHeader("AI Parameters", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2, "##ai", false);

                ImGui.Text("State:");
                ImGui.NextColumn();
                ImGui.TextColored(GetAIStateColor(ai.state.ToString()), ai.state.ToString());
                ImGui.NextColumn();

                ImGui.Text("Wander Radius:");
                ImGui.NextColumn();
                ImGui.Text(ai.wanderRadius.ToString());
                ImGui.NextColumn();

                ImGui.Text("Wander Chance:");
                ImGui.NextColumn();
                ImGui.Text($"{ai.wanderChance:P0}");
                ImGui.NextColumn();

                ImGui.Text("Awareness Range:");
                ImGui.NextColumn();
                ImGui.Text(ai.awarenessRange.ToString());
                ImGui.NextColumn();

                ImGui.Text("Chase Range:");
                ImGui.NextColumn();
                ImGui.Text(ai.chaseRange.ToString());
                ImGui.NextColumn();

                if (ai.position?.Length == 2)
                {
                    ImGui.Text("Position:");
                    ImGui.NextColumn();
                    ImGui.Text($"({ai.position[0]}, {ai.position[1]})");
                    ImGui.NextColumn();
                }

                if (ai.origin?.Length == 2)
                {
                    ImGui.Text("Origin:");
                    ImGui.NextColumn();
                    ImGui.Text($"({ai.origin[0]}, {ai.origin[1]})");
                    ImGui.NextColumn();
                }

                ImGui.Columns(1);
            }
            ImGui.PopStyleColor();
        }

        private void RenderRawDataTab()
        {
            if (ImGui.Checkbox("Show Raw Component Data", ref _showRawData))
            {
                // Toggle raw data view
            }

            if (_showRawData)
            {
                foreach (var componentName in _knownComponents)
                {
                    if (_selectedEntity!.HasComponent(componentName))
                    {
                        ImGui.Separator();
                        ImGui.TextColored(ColorPrimary, componentName);
                        RenderGenericComponent(componentName);
                    }
                }
            }
        }

        private void RenderHistoryTab()
        {
            ImGui.Text("Recent Modifications:");
            ImGui.Separator();

            foreach (var kvp in _lastModified.OrderByDescending(x => x.Value).Take(10))
            {
                var timeAgo = DateTime.Now - kvp.Value;
                string timeStr = timeAgo.TotalSeconds < 60 ? $"{timeAgo.TotalSeconds:F0}s ago" :
                               timeAgo.TotalMinutes < 60 ? $"{timeAgo.TotalMinutes:F0}m ago" :
                               $"{timeAgo.TotalHours:F0}h ago";

                ImGui.TextColored(ColorDim, timeStr);
                ImGui.SameLine();
                ImGui.Text($"{kvp.Key} modified");
            }
        }

        private void RenderActionBar()
        {
            if (RenderIconButton(Trash, ColorDanger, "Destroy this entity"))
            {
                if (_selectedEntity != null && _selectedEntity.IsValid)
                {
                    Engine.Instance?.DestroyEntity(_selectedEntity);
                    _selectedEntity = null;
                    _selectedEntityId = -1;
                }
            }

            ImGui.SameLine();
            if (RenderIconButton(Clone, ColorInfo, "Clone this entity"))
            {
                // TODO: Implement cloning
                Console.WriteLine("[EntityInspector] Clone not implemented yet");
            }

            ImGui.SameLine();
            if (RenderIconButton(Save, ColorSuccess, "Export entity data"))
            {
                ExportEntityData();
            }

            ImGui.SameLine();
            if (RenderIconButton(Sync, ColorPrimary, "Refresh entity data"))
            {
                // Force refresh
                var id = _selectedEntityId;
                _selectedEntity = null;
                _selectedEntityId = id;
                if (id >= 0)
                {
                    _selectedEntity = Entity.FromRaw(Engine.Instance!.Context, id);
                }
            }
        }

        private void RenderNoSelectionPanel()
        {
            var center = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPos(new Vector2(center.X / 2 - 100, center.Y / 2 - 50));

            ImGui.PushStyleColor(ImGuiCol.Text, ColorDim);
            ImGui.Text("No Entity Selected");
            ImGui.PopStyleColor();

            ImGui.SetCursorPosX(center.X / 2 - 80);
            ImGui.TextColored(ColorDim * new Vector4(1, 1, 1, 0.5f), "Select an entity from the list");
        }

        // Helper methods
        private void RefreshEntityCache()
        {
            _entityCache.Clear();
            World.ForEachEntity(_entityCache.Add);
            _lastCacheUpdate = DateTime.Now;
        }

        private List<Entity> GetFilteredEntities()
        {
            var filtered = _entityCache.AsEnumerable();

            // Apply type filter
            switch (_filterType)
            {
                case EntityFilterType.Player:
                    filtered = filtered.Where(e => e.HasComponent(nameof(PlayerTagComponent)));
                    break;
                case EntityFilterType.NPCs:
                    filtered = filtered.Where(e => e.HasComponent(nameof(StatsComponent)) &&
                                                  !e.HasComponent(nameof(PlayerTagComponent)));
                    break;
                case EntityFilterType.Items:
                    filtered = filtered.Where(e => !e.HasComponent(nameof(StatsComponent)));
                    break;
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                var search = _searchFilter.ToLower();
                filtered = filtered.Where(e =>
                {
                    // Search by ID
                    if (e.Id.ToString().Contains(search))
                        return true;

                    // Search by name
                    var name = GetEntityDisplayName(e).ToLower();
                    if (name.Contains(search))
                        return true;

                    // Search by component
                    foreach (var comp in _knownComponents)
                    {
                        if (comp.ToLower().Contains(search) && e.HasComponent(comp))
                            return true;
                    }

                    return false;
                });
            }

            return filtered.OrderBy(e => e.Id).ToList();
        }

        private string GetEntityIcon(Entity e)
        {
            if (e.HasComponent(nameof(PlayerTagComponent))) return User;
            if (e.HasComponent(nameof(StatsComponent))) return Dumbbell;
            if (e.HasComponent(nameof(ObstacleComponent))) return Road;
            if (e.HasComponent(nameof(AnimationStateComponent))) return Film;
            return Box;
        }

        private string GetEntityDisplayName(Entity entity)
        {
            if (entity.HasComponent(nameof(PlayerTagComponent)))
                return $"Player #{entity.Id}";

            if (entity.HasComponent(nameof(PrefabIdComponent)))
            {
                try
                {
                    var prefab = entity.GetComponent<PrefabIdComponent>(nameof(PrefabIdComponent));
                    if (prefab?.PrefabName != null)
                        return $"{prefab.PrefabName} #{entity.Id}";
                }
                catch { }
            }

            if (entity.HasComponent(nameof(TagComponent)))
            {
                try
                {
                    var tag = entity.GetComponent<TagComponent>(nameof(TagComponent));
                    if (!string.IsNullOrEmpty(tag?.Tag))
                        return $"{tag.Tag} #{entity.Id}";
                }
                catch { }
            }

            return $"Entity #{entity.Id}";
        }

        private Vector4 GetAlignmentColor(string alignment)
        {
            return alignment?.ToLower() switch
            {
                "good" => ColorSuccess,
                "evil" or "bad" => ColorDanger,
                "neutral" => ColorWarning,
                _ => ColorDim
            };
        }

        private Vector4 GetAIStateColor(string state)
        {
            return state?.ToLower() switch
            {
                "idle" => ColorSuccess,
                "wander" => ColorInfo,
                "chase" or "attack" => ColorDanger,
                "flee" => ColorWarning,
                _ => ColorDim
            };
        }

        private bool RenderFilterButton(string label, EntityFilterType type)
        {
            bool isActive = _filterType == type;
            if (isActive)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ColorPrimary * new Vector4(1, 1, 1, 0.5f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorPrimary * new Vector4(1, 1, 1, 0.6f));
            }

            bool clicked = ImGui.Button(label);
            if (clicked) _filterType = type;

            if (isActive)
                ImGui.PopStyleColor(2);

            return clicked;
        }

        private bool RenderHostilityCheckbox(string label, bool currentValue, Action<bool> onValueChanged)
        {
            bool value = currentValue;
            if (ImGui.Checkbox(label, ref value))
            {
                onValueChanged(value);
                return true;
            }
            return false;
        }

        private void SetAnimationState(string state)
        {
            ComponentWriter.Patch((uint)_selectedEntity!.Id, nameof(AnimationStateComponent),
                new AnimationStateComponent { state = state });
            _lastModified[nameof(AnimationStateComponent)] = DateTime.Now;
        }

        private void UpdateComponent<T>(string componentName, T component)
        {
            ComponentWriter.Patch((uint)_selectedEntity!.Id, componentName, component);
            _lastModified[componentName] = DateTime.Now;
        }

        private void CopyEntityToClipboard()
        {
            if (_selectedEntity == null) return;

            var sb = new StringBuilder();
            sb.AppendLine($"Entity ID: {_selectedEntity.Id}");
            sb.AppendLine($"UUID: {_selectedEntity.Uuid:X16}");
            sb.AppendLine($"Components:");

            foreach (var comp in _knownComponents)
            {
                if (_selectedEntity.HasComponent(comp))
                    sb.AppendLine($"  - {comp}");
            }

            Console.WriteLine(sb.ToString());
        }

        private void ExportEntityData()
        {
            if (_selectedEntity == null) return;

            try
            {
                var data = new Dictionary<string, object>
                {
                    ["id"] = _selectedEntity.Id,
                    ["uuid"] = _selectedEntity.Uuid.ToString("X16"),
                    ["components"] = new Dictionary<string, object>()
                };

                var components = data["components"] as Dictionary<string, object>;

                foreach (var compName in _knownComponents)
                {
                    if (_selectedEntity.HasComponent(compName))
                    {
                        var buffer = new byte[4096];
                        int len = EngineInterop.GetComponentJson(
                            Engine.Instance!.Context,
                            new EntityId { id = (uint)_selectedEntity.Id },
                            compName, buffer, buffer.Length);

                        if (len > 0)
                        {
                            string json = Encoding.UTF8.GetString(buffer, 0, len);
                            components[compName] = JsonSerializer.Deserialize<object>(json);
                        }
                    }
                }

                string exportJson = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                Console.WriteLine($"[EntityInspector] Exported entity data:");
                Console.WriteLine(exportJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EntityInspector] Export failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _componentExpanded.Clear();
            _editBuffers.Clear();
            _modifiedComponents.Clear();
            _entityCache.Clear();
            _editHistory.Clear();
            _lastModified.Clear();
        }
    }

    public enum EntityFilterType
    {
        All,
        Player,
        NPCs,
        Items
    }

    // ImGui extensions
    public enum ImGuiSeparatorFlags
    {
        None = 0,
        Horizontal = 1 << 0,
        Vertical = 1 << 1,
        SpanAllColumns = 1 << 2
    }

    public static class ImGuiExtensions
    {
        public static void SeparatorEx(ImGuiSeparatorFlags flags)
        {
            // This would normally call the native ImGui function
            // For now, just use regular separator
            ImGui.Separator();
        }
    }
}