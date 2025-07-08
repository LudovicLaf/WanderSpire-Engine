using Game.Dto;
using ScriptHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WanderSpire.Components;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Professional component registry window for viewing all component types and their usage.
    /// </summary>
    public class ComponentRegistryWindow : ImGuiWindowBase
    {
        public override string Title => "Component Registry";

        // Theme colors
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorDanger = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);

        // Component tracking
        private readonly Dictionary<string, ComponentInfo> _componentInfo = new();
        private ComponentInfo? _selectedComponent;
        private string _searchFilter = "";
        private ComponentCategory _filterCategory = ComponentCategory.All;

        // Component categories
        private readonly Dictionary<string, ComponentCategory> _componentCategories = new()
        {
            { nameof(GridPositionComponent), ComponentCategory.Transform },
            { nameof(TransformComponent), ComponentCategory.Transform },
            { nameof(TargetGridPositionComponent), ComponentCategory.Transform },
            { nameof(SpriteComponent), ComponentCategory.Rendering },
            { nameof(SpriteAnimationComponent), ComponentCategory.Rendering },
            { nameof(AnimationClipsComponent), ComponentCategory.Rendering },
            { nameof(AnimationStateComponent), ComponentCategory.Rendering },
            { nameof(FacingComponent), ComponentCategory.Rendering },
            { nameof(StatsComponent), ComponentCategory.Gameplay },
            { nameof(FactionComponent), ComponentCategory.Gameplay },
            { nameof(PlayerTagComponent), ComponentCategory.Gameplay },
            { nameof(ObstacleComponent), ComponentCategory.Gameplay },
            { nameof(IDComponent), ComponentCategory.Core },
            { nameof(PrefabIdComponent), ComponentCategory.Core },
            { nameof(TagComponent), ComponentCategory.Core },
            { nameof(CommentComponent), ComponentCategory.Debug },
            { nameof(EditorOnlyComponent), ComponentCategory.Debug },
        };

        public override void Render()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16, 16));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 6));

            if (!BeginWindow())
            {
                ImGui.PopStyleVar(3);
                EndWindow();
                return;
            }

            UpdateComponentInfo();

            RenderHeader();
            ImGui.Separator();

            var contentRegion = ImGui.GetContentRegionAvail();
            float leftPanelWidth = 300;

            // Left panel - Component list
            RenderComponentListPanel(leftPanelWidth, contentRegion.Y - 10);

            ImGui.SameLine();
            ImGuiExtensions.SeparatorEx(ImGuiSeparatorFlags.Vertical);
            ImGui.SameLine();

            // Right panel - Component details
            RenderComponentDetailsPanel(contentRegion.X - leftPanelWidth - 20, contentRegion.Y - 10);

            ImGui.PopStyleVar(3);
            EndWindow();
        }

        private void UpdateComponentInfo()
        {
            _componentInfo.Clear();

            // Scan all entities to gather component usage statistics
            World.ForEachEntity(entity =>
            {
                foreach (var componentName in _componentCategories.Keys)
                {
                    if (entity.HasComponent(componentName))
                    {
                        if (!_componentInfo.ContainsKey(componentName))
                        {
                            _componentInfo[componentName] = new ComponentInfo
                            {
                                Name = componentName,
                                Category = _componentCategories.GetValueOrDefault(componentName, ComponentCategory.Other),
                                Usage = new List<int>()
                            };
                        }

                        _componentInfo[componentName].Usage.Add(entity.Id);
                    }
                }
            });
        }

        private void RenderHeader()
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(Cogs);
            ImGui.PopStyleColor();
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text("Component Registry");
            ImGui.PopStyleColor();

            ImGui.SameLine();
            ImGui.TextColored(ColorDim, $"({_componentInfo.Count} types)");

            // Total entities using components
            var totalUsage = _componentInfo.Values.Sum(c => c.Usage.Count);
            ImGui.SameLine(ImGui.GetWindowWidth() - 150);
            ImGui.TextColored(ColorInfo, $"{totalUsage} instances");
        }

        private void RenderComponentListPanel(float width, float height)
        {
            ImGui.BeginChild("##component_list", new Vector2(width, height), true);

            RenderComponentControls();
            ImGui.Separator();

            RenderComponentStatistics();
            ImGui.Separator();

            RenderComponentList();

            ImGui.EndChild();
        }

        private void RenderComponentControls()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorPrimary * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Component Browser", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                // Search filter
                ImGui.Text("Search:");
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##component_search", ref _searchFilter, 128);

                // Category filter
                ImGui.Text("Category:");
                ImGui.SetNextItemWidth(-1);
                string[] categoryNames = Enum.GetNames<ComponentCategory>();
                int categoryIndex = (int)_filterCategory;
                if (ImGui.Combo("##category", ref categoryIndex, categoryNames, categoryNames.Length))
                {
                    _filterCategory = (ComponentCategory)categoryIndex;
                }

                ImGui.Spacing();

                // Quick actions
                if (RenderIconButton(Sync, ColorPrimary, "Refresh Component Data"))
                {
                    UpdateComponentInfo();
                    Console.WriteLine("[ComponentRegistry] Refreshed component data");
                }

                ImGui.SameLine();
                if (RenderIconButton(Save, ColorSuccess, "Export Component Report"))
                {
                    ExportComponentReport();
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderComponentStatistics()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorInfo * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Statistics", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                // Category breakdown
                var categoryStats = new Dictionary<ComponentCategory, int>();
                foreach (var component in _componentInfo.Values)
                {
                    categoryStats[component.Category] = categoryStats.GetValueOrDefault(component.Category, 0) + 1;
                }

                foreach (var (category, count) in categoryStats.OrderByDescending(x => x.Value))
                {
                    if (category == ComponentCategory.All) continue;

                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.Text(GetCategoryIcon(category));
                    ImGuiManager.Instance?.PopIconFont();
                    ImGui.SameLine();
                    ImGui.Text($"{category}:");
                    ImGui.SameLine(120);
                    ImGui.TextColored(ColorInfo, count.ToString());

                    // Usage bar
                    float fraction = (float)count / Math.Max(_componentInfo.Count, 1);
                    ImGui.SameLine(150);
                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, GetCategoryColor(category));
                    ImGui.ProgressBar(fraction, new Vector2(80, 0), "");
                    ImGui.PopStyleColor();
                }

                ImGui.Separator();

                // Most used components
                ImGui.Text("Most Used:");
                var mostUsed = _componentInfo.Values
                    .OrderByDescending(c => c.Usage.Count)
                    .Take(3);

                foreach (var component in mostUsed)
                {
                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.TextColored(ColorSuccess, Trophy);
                    ImGuiManager.Instance?.PopIconFont();
                    ImGui.SameLine();
                    ImGui.Text($"{component.Name}:");
                    ImGui.SameLine(150);
                    ImGui.TextColored(ColorSuccess, $"{component.Usage.Count}");
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderComponentList()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorSuccess * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Component Types", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                var filteredComponents = GetFilteredComponents();

                foreach (var component in filteredComponents)
                {
                    RenderComponentListItem(component);
                }

                if (filteredComponents.Count == 0)
                {
                    ImGui.TextColored(ColorDim, "No components match the current filter.");
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderComponentListItem(ComponentInfo component)
        {
            bool isSelected = _selectedComponent?.Name == component.Name;
            var categoryColor = GetCategoryColor(component.Category);
            var categoryIcon = GetCategoryIcon(component.Category);

            ImGui.PushID(component.Name);

            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Header, ColorPrimary * new Vector4(1, 1, 1, 0.3f));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ColorPrimary * new Vector4(1, 1, 1, 0.4f));
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, ColorPrimary * new Vector4(1, 1, 1, 0.5f));
            }

            // Category icon
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(categoryColor, categoryIcon);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();

            // Component name
            if (ImGui.Selectable(component.Name, isSelected))
            {
                _selectedComponent = component;
            }

            if (isSelected)
                ImGui.PopStyleColor(3);

            // Usage count on the same line
            ImGui.SameLine(200);
            ImGui.TextColored(ColorDim, $"({component.Usage.Count})");

            // Context menu
            if (ImGui.BeginPopupContextItem($"##comp_ctx_{component.Name}"))
            {
                if (ImGui.MenuItem("Copy Component Name"))
                {
                    Console.WriteLine($"Component name: {component.Name}");
                }

                if (ImGui.MenuItem("Find Entities Using This"))
                {
                    Console.WriteLine($"[ComponentRegistry] Finding entities using {component.Name}");
                }

                if (ImGui.MenuItem("Component Documentation"))
                {
                    Console.WriteLine($"[ComponentRegistry] Opening documentation for {component.Name}");
                }

                ImGui.EndPopup();
            }

            ImGui.PopID();
        }

        private void RenderComponentDetailsPanel(float width, float height)
        {
            ImGui.BeginChild("##component_details", new Vector2(width, height), true);

            if (_selectedComponent != null)
            {
                RenderSelectedComponentDetails();
            }
            else
            {
                RenderNoComponentSelectedPanel();
            }

            ImGui.EndChild();
        }

        private void RenderSelectedComponentDetails()
        {
            var component = _selectedComponent!;
            var categoryColor = GetCategoryColor(component.Category);
            var categoryIcon = GetCategoryIcon(component.Category);

            // Header
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(categoryColor, categoryIcon);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text($"Component: {component.Name}");
            ImGui.PopStyleColor();

            ImGui.Separator();

            RenderComponentInformation(component);
            ImGui.Separator();

            RenderComponentUsage(component);
            ImGui.Separator();

            RenderComponentActions(component);
        }

        private void RenderComponentInformation(ComponentInfo component)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorInfo * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Component Information", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                ImGui.Columns(2, "##comp_info", false);

                // Name
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(Tag);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Name:");
                ImGui.NextColumn();
                ImGui.TextColored(ColorInfo, component.Name);
                ImGui.NextColumn();

                // Category
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(GetCategoryIcon(component.Category));
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Category:");
                ImGui.NextColumn();
                ImGui.TextColored(GetCategoryColor(component.Category), component.Category.ToString());
                ImGui.NextColumn();

                // Usage count
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(Cubes);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Usage Count:");
                ImGui.NextColumn();
                ImGui.TextColored(ColorSuccess, component.Usage.Count.ToString());
                ImGui.NextColumn();

                // Assembly info (simulated)
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(Archive);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Assembly:");
                ImGui.NextColumn();
                ImGui.TextColored(ColorDim, "WanderSpire.Components");
                ImGui.NextColumn();

                ImGui.Columns(1);

                // Component description
                ImGui.Spacing();
                ImGui.Text("Description:");
                ImGui.TextWrapped(GetComponentDescription(component.Name));
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderComponentUsage(ComponentInfo component)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorSuccess * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader($"Entity Usage ({component.Usage.Count})", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                if (component.Usage.Count > 0)
                {
                    ImGui.Text("Entities using this component:");
                    ImGui.Separator();

                    // Show first 20 entities
                    var entitiesToShow = component.Usage.Take(20);
                    foreach (var entityId in entitiesToShow)
                    {
                        ImGuiManager.Instance?.PushIconFont();
                        ImGui.Text(Cube);
                        ImGuiManager.Instance?.PopIconFont();
                        ImGui.SameLine();

                        if (ImGui.Selectable($"Entity #{entityId}"))
                        {
                            Console.WriteLine($"[ComponentRegistry] Selected entity {entityId}");
                        }

                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip($"Click to inspect Entity #{entityId}");
                    }

                    if (component.Usage.Count > 20)
                    {
                        ImGui.TextColored(ColorDim, $"... and {component.Usage.Count - 20} more entities");
                    }
                }
                else
                {
                    ImGui.TextColored(ColorDim, "No entities are currently using this component.");
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderComponentActions(ComponentInfo component)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorWarning * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Component Actions"))
            {
                ImGui.PopStyleColor();

                ImGui.Text("Development Tools:");

                if (RenderIconButton(Search, ColorPrimary, "Find All Entities"))
                {
                    Console.WriteLine($"[ComponentRegistry] Finding all entities with {component.Name}");
                }

                ImGui.SameLine();
                if (RenderIconButton(Code, ColorInfo, "View Component Code"))
                {
                    Console.WriteLine($"[ComponentRegistry] Opening code for {component.Name}");
                }

                ImGui.SameLine();
                if (RenderIconButton(Copy, ColorSuccess, "Copy Component Definition"))
                {
                    Console.WriteLine($"[ComponentRegistry] Copied {component.Name} definition");
                }

                ImGui.Spacing();

                ImGui.Text("Debug Actions:");
                if (RenderIconButton(Bug, ColorWarning, "Debug Component"))
                {
                    Console.WriteLine($"[ComponentRegistry] Debugging {component.Name}");
                }

                ImGui.SameLine();
                if (RenderIconButton(ExclamationTriangle, ColorDanger, "Validate All Instances"))
                {
                    Console.WriteLine($"[ComponentRegistry] Validating all {component.Name} instances");
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderNoComponentSelectedPanel()
        {
            var center = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPos(new Vector2(center.X / 2 - 120, center.Y / 2 - 50));

            ImGui.PushStyleColor(ImGuiCol.Text, ColorDim);
            ImGui.Text("No Component Selected");
            ImGui.PopStyleColor();

            ImGui.SetCursorPosX(center.X / 2 - 140);
            ImGui.TextColored(ColorDim * new Vector4(1, 1, 1, 0.5f), "Select a component from the list");
        }

        private List<ComponentInfo> GetFilteredComponents()
        {
            return _componentInfo.Values.Where(component =>
            {
                // Category filter
                if (_filterCategory != ComponentCategory.All && component.Category != _filterCategory)
                    return false;

                // Search filter
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !component.Name.ToLower().Contains(_searchFilter.ToLower()))
                    return false;

                return true;
            }).OrderBy(c => c.Category).ThenBy(c => c.Name).ToList();
        }

        private Vector4 GetCategoryColor(ComponentCategory category)
        {
            return category switch
            {
                ComponentCategory.Core => ColorPrimary,
                ComponentCategory.Transform => ColorSuccess,
                ComponentCategory.Rendering => ColorInfo,
                ComponentCategory.Gameplay => ColorWarning,
                ComponentCategory.Debug => ColorDanger,
                _ => ColorDim
            };
        }

        private string GetCategoryIcon(ComponentCategory category)
        {
            return category switch
            {
                ComponentCategory.Core => Cog,
                ComponentCategory.Transform => ArrowsAlt,
                ComponentCategory.Rendering => PaintBrush,
                ComponentCategory.Gameplay => Gamepad,
                ComponentCategory.Debug => Bug,
                _ => Circle
            };
        }

        private string GetComponentDescription(string componentName)
        {
            return componentName switch
            {
                nameof(GridPositionComponent) => "Represents an entity's position on the tile-based grid system.",
                nameof(TransformComponent) => "Stores world-space position, rotation, and scale information.",
                nameof(SpriteComponent) => "References sprite atlas and frame for 2D rendering.",
                nameof(StatsComponent) => "Contains gameplay statistics like health, mana, and combat attributes.",
                nameof(FactionComponent) => "Defines entity alignment and hostility relationships.",
                nameof(PlayerTagComponent) => "Marks an entity as the player character.",
                nameof(IDComponent) => "Provides a unique identifier for the entity.",
                _ => "No description available for this component."
            };
        }

        private void ExportComponentReport()
        {
            Console.WriteLine("[ComponentRegistry] Component Usage Report:");
            Console.WriteLine("==========================================");

            foreach (var component in _componentInfo.Values.OrderByDescending(c => c.Usage.Count))
            {
                Console.WriteLine($"{component.Name} ({component.Category}): {component.Usage.Count} instances");
            }

            Console.WriteLine($"\nTotal component types: {_componentInfo.Count}");
            Console.WriteLine($"Total component instances: {_componentInfo.Values.Sum(c => c.Usage.Count)}");
        }

        private bool RenderIconButton(string icon, Vector4 color, string tooltip = null)
        {
            ImGuiManager.Instance?.PushIconFont();

            ImGui.PushStyleColor(ImGuiCol.Button, color * new Vector4(1, 1, 1, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));

            Vector2 textSize = ImGui.CalcTextSize(icon);
            Vector2 btnSize = new Vector2(textSize.X + 8, textSize.Y + 4);

            bool clicked = ImGui.Button(icon, btnSize);

            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);

            ImGui.PopStyleColor(3);
            ImGuiManager.Instance?.PopIconFont();

            return clicked;
        }

        private enum ComponentCategory
        {
            All,
            Core,
            Transform,
            Rendering,
            Gameplay,
            Debug,
            Other
        }

        private class ComponentInfo
        {
            public string Name { get; set; } = "";
            public ComponentCategory Category { get; set; }
            public List<int> Usage { get; set; } = new();
        }
    }
}