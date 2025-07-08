using Game.Dto;
using ScriptHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WanderSpire.Components;
using WanderSpire.Scripting;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Professional entity search window with advanced filtering and query capabilities.
    /// </summary>
    public class EntitySearchWindow : ImGuiWindowBase, IDisposable
    {
        public override string Title => "Entity Search";

        // Theme colors
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorDanger = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);

        // Search state
        private string _searchQuery = "";
        private SearchMode _searchMode = SearchMode.Simple;
        private readonly List<SearchFilter> _filters = new();
        private readonly List<Entity> _searchResults = new();
        private readonly List<SearchQuery> _searchHistory = new();
        private Entity? _selectedEntity;
        private bool _caseSensitive = false;
        private bool _useRegex = false;

        // Search criteria
        private bool _searchByName = true;
        private bool _searchByComponent = true;
        private bool _searchByProperty = false;
        private bool _searchByValue = false;

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

            RenderHeader();
            ImGui.Separator();

            var contentRegion = ImGui.GetContentRegionAvail();
            float leftPanelWidth = 350;

            // Left panel - Search interface
            RenderSearchPanel(leftPanelWidth, contentRegion.Y - 10);

            ImGui.SameLine();
            ImGuiExtensions.SeparatorEx(ImGuiSeparatorFlags.Vertical);
            ImGui.SameLine();

            // Right panel - Search results
            RenderResultsPanel(contentRegion.X - leftPanelWidth - 20, contentRegion.Y - 10);

            ImGui.PopStyleVar(3);
            EndWindow();
        }

        private void RenderHeader()
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(Search);
            ImGui.PopStyleColor();
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text("Entity Search");
            ImGui.PopStyleColor();

            ImGui.SameLine();
            ImGui.TextColored(ColorDim, $"({_searchResults.Count} results)");

            // Search mode indicator
            ImGui.SameLine(ImGui.GetWindowWidth() - 150);
            var modeColor = _searchMode == SearchMode.Advanced ? ColorWarning : ColorInfo;
            ImGui.TextColored(modeColor, _searchMode.ToString());
        }

        private void RenderSearchPanel(float width, float height)
        {
            ImGui.BeginChild("##search_panel", new Vector2(width, height), true);

            RenderSearchInterface();
            ImGui.Separator();

            if (_searchMode == SearchMode.Advanced)
            {
                RenderAdvancedFilters();
                ImGui.Separator();
            }

            RenderSearchHistory();

            ImGui.EndChild();
        }

        private void RenderSearchInterface()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorPrimary * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Search Query", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                // Main search input
                ImGui.Text("Search:");
                ImGui.SetNextItemWidth(-80);
                bool searchChanged = ImGui.InputText("##search_query", ref _searchQuery, 256);

                ImGui.SameLine();
                if (RenderIconButton(Search, ColorPrimary, "Execute Search") || searchChanged)
                {
                    ExecuteSearch();
                }

                // Search mode toggle
                ImGui.Spacing();
                string[] modeNames = Enum.GetNames<SearchMode>();
                int modeIndex = (int)_searchMode;
                if (ImGui.Combo("Mode", ref modeIndex, modeNames, modeNames.Length))
                {
                    _searchMode = (SearchMode)modeIndex;
                }

                // Search options
                ImGui.Spacing();
                ImGui.Text("Search In:");
                ImGui.Checkbox("Entity Names", ref _searchByName);
                ImGui.SameLine();
                ImGui.Checkbox("Components", ref _searchByComponent);

                ImGui.Checkbox("Properties", ref _searchByProperty);
                ImGui.SameLine();
                ImGui.Checkbox("Values", ref _searchByValue);

                // Advanced options
                ImGui.Spacing();
                ImGui.Text("Options:");
                ImGui.Checkbox("Case Sensitive", ref _caseSensitive);
                ImGui.SameLine();
                ImGui.Checkbox("Use Regex", ref _useRegex);

                // Quick search buttons
                ImGui.Spacing();
                ImGui.Text("Quick Search:");
                if (RenderIconButton(User, ColorSuccess, "Find Players"))
                    QuickSearch("PlayerTagComponent");

                ImGui.SameLine();
                if (RenderIconButton(Robot, ColorWarning, "Find NPCs"))
                    QuickSearch("!PlayerTagComponent StatsComponent");

                ImGui.SameLine();
                if (RenderIconButton(Cube, ColorInfo, "Find Items"))
                    QuickSearch("!StatsComponent !PlayerTagComponent");

                ImGui.SameLine();
                if (RenderIconButton(Road, ColorDim, "Find Obstacles"))
                    QuickSearch("ObstacleComponent");
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderAdvancedFilters()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorWarning * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Advanced Filters", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                // Filter management
                if (RenderIconButton(Plus, ColorSuccess, "Add Filter"))
                {
                    _filters.Add(new SearchFilter());
                }

                ImGui.SameLine();
                if (RenderIconButton(Trash, ColorDanger, "Clear All Filters"))
                {
                    _filters.Clear();
                }

                ImGui.Separator();

                // Render existing filters
                for (int i = _filters.Count - 1; i >= 0; i--)
                {
                    RenderFilterRow(i);
                }

                if (_filters.Count == 0)
                {
                    ImGui.TextColored(ColorDim, "No filters added. Click + to add a filter.");
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderFilterRow(int index)
        {
            var filter = _filters[index];

            ImGui.PushID($"filter_{index}");

            // Filter type
            ImGui.SetNextItemWidth(100);
            string[] filterTypes = Enum.GetNames<FilterType>();
            int typeIndex = (int)filter.Type;
            if (ImGui.Combo("##type", ref typeIndex, filterTypes, filterTypes.Length))
            {
                filter.Type = (FilterType)typeIndex;
            }

            ImGui.SameLine();

            // Filter value
            ImGui.SetNextItemWidth(150);

            // WORKAROUND for CS0206
            string value = filter.Value ?? ""; // local copy
            if (ImGui.InputText("##value", ref value, 128))
            {
                filter.Value = value;
            }

            ImGui.SameLine();

            // Remove button
            if (RenderIconButton(Trash, ColorDanger, "Remove Filter"))
            {
                _filters.RemoveAt(index);
            }
            else
            {
                // Only update the list if not removing!
                _filters[index] = filter;
            }

            ImGui.PopID();
        }


        private void RenderSearchHistory()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorInfo * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Search History"))
            {
                ImGui.PopStyleColor();

                if (RenderIconButton(Trash, ColorDanger, "Clear History"))
                {
                    _searchHistory.Clear();
                }

                ImGui.Separator();

                foreach (var query in _searchHistory.TakeLast(10).Reverse())
                {
                    ImGui.PushID(query.Id.ToString());

                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.TextColored(ColorDim, Clock);
                    ImGuiManager.Instance?.PopIconFont();
                    ImGui.SameLine();

                    if (ImGui.Selectable(query.Query))
                    {
                        _searchQuery = query.Query;
                        ExecuteSearch();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Executed: {query.Timestamp:HH:mm:ss}\nResults: {query.ResultCount}");
                    }

                    ImGui.PopID();
                }

                if (_searchHistory.Count == 0)
                {
                    ImGui.TextColored(ColorDim, "No search history.");
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderResultsPanel(float width, float height)
        {
            ImGui.BeginChild("##results_panel", new Vector2(width, height), true);

            RenderResultsHeader();
            ImGui.Separator();

            if (_searchResults.Count > 0)
            {
                RenderSearchResults();
            }
            else
            {
                RenderNoResultsPanel();
            }

            ImGui.EndChild();
        }

        private void RenderResultsHeader()
        {
            ImGui.Text($"Search Results ({_searchResults.Count})");

            ImGui.SameLine(ImGui.GetWindowWidth() - 200);
            if (RenderIconButton(Download, ColorPrimary, "Export Results"))
            {
                ExportSearchResults();
            }

            ImGui.SameLine();
            if (RenderIconButton(Sync, ColorSuccess, "Refresh Results"))
            {
                ExecuteSearch();
            }
        }

        private void RenderSearchResults()
        {
            ImGui.BeginChild("##results_list", new Vector2(-1, -1));

            foreach (var entity in _searchResults)
            {
                RenderSearchResultItem(entity);
            }

            ImGui.EndChild();
        }

        private void RenderSearchResultItem(Entity entity)
        {
            bool isSelected = _selectedEntity?.Id == entity.Id;
            var entityName = GetEntityName(entity);
            var entityIcon = GetEntityIcon(entity);

            ImGui.PushID(entity.Id.ToString());

            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Header, ColorPrimary * new Vector4(1, 1, 1, 0.3f));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ColorPrimary * new Vector4(1, 1, 1, 0.4f));
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, ColorPrimary * new Vector4(1, 1, 1, 0.5f));
            }

            // Entity icon
            ImGuiManager.Instance?.PushIconFont();
            ImGui.Text(entityIcon);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();

            // Entity name and selection
            if (ImGui.Selectable($"{entityName}##{entity.Id}", isSelected))
            {
                _selectedEntity = entity;
            }

            if (isSelected)
                ImGui.PopStyleColor(3);

            // Show match reason
            var matchReason = GetMatchReason(entity);
            if (!string.IsNullOrEmpty(matchReason))
            {
                ImGui.Indent();
                ImGui.TextColored(ColorDim, $"Match: {matchReason}");
                ImGui.Unindent();
            }

            // Context menu
            if (ImGui.BeginPopupContextItem($"##result_ctx_{entity.Id}"))
            {
                if (ImGui.MenuItem("Inspect Entity"))
                {
                    Console.WriteLine($"[EntitySearch] Inspecting entity {entity.Id}");
                }

                if (ImGui.MenuItem("Focus in World"))
                {
                    Console.WriteLine($"[EntitySearch] Focusing on entity {entity.Id}");
                }

                if (ImGui.MenuItem("Copy Entity ID"))
                {
                    Console.WriteLine($"Entity ID: {entity.Id}");
                }

                ImGui.EndPopup();
            }

            ImGui.PopID();
        }

        private void RenderNoResultsPanel()
        {
            var center = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPos(new Vector2(center.X / 2 - 80, center.Y / 2 - 50));

            ImGui.PushStyleColor(ImGuiCol.Text, ColorDim);
            ImGui.Text("No Results Found");
            ImGui.PopStyleColor();

            ImGui.SetCursorPosX(center.X / 2 - 100);
            ImGui.TextColored(ColorDim * new Vector4(1, 1, 1, 0.5f), "Try adjusting your search criteria");
        }

        private void ExecuteSearch()
        {
            _searchResults.Clear();
            _selectedEntity = null;

            if (string.IsNullOrWhiteSpace(_searchQuery))
                return;

            // Add to search history
            _searchHistory.Add(new SearchQuery
            {
                Id = Guid.NewGuid(),
                Query = _searchQuery,
                Timestamp = DateTime.UtcNow,
                ResultCount = 0 // Will be updated later
            });

            // Perform search
            World.ForEachEntity(entity =>
            {
                if (MatchesSearchCriteria(entity))
                {
                    _searchResults.Add(entity);
                }
            });

            // Update result count in history
            if (_searchHistory.Count > 0)
            {
                _searchHistory[^1].ResultCount = _searchResults.Count;
            }

            // Limit history size
            while (_searchHistory.Count > 50)
                _searchHistory.RemoveAt(0);

            Console.WriteLine($"[EntitySearch] Found {_searchResults.Count} entities matching '{_searchQuery}'");
        }

        private bool MatchesSearchCriteria(Entity entity)
        {
            var query = _caseSensitive ? _searchQuery : _searchQuery.ToLower();

            // Simple search mode
            if (_searchMode == SearchMode.Simple)
            {
                return MatchesSimpleSearch(entity, query);
            }

            // Advanced search mode
            return MatchesAdvancedSearch(entity, query);
        }

        private bool MatchesSimpleSearch(Entity entity, string query)
        {
            // Search by entity name
            if (_searchByName)
            {
                var entityName = GetEntityName(entity);
                var name = _caseSensitive ? entityName : entityName.ToLower();
                if (name.Contains(query))
                    return true;
            }

            // Search by component presence
            if (_searchByComponent)
            {
                var knownComponents = new[]
                {
                    nameof(PlayerTagComponent),
                    nameof(StatsComponent),
                    nameof(GridPositionComponent),
                    nameof(SpriteComponent),
                    nameof(ObstacleComponent),
                    nameof(PrefabIdComponent)
                };

                foreach (var componentName in knownComponents)
                {
                    var compName = _caseSensitive ? componentName : componentName.ToLower();
                    if (compName.Contains(query) && entity.HasComponent(componentName))
                        return true;
                }
            }

            return false;
        }

        private bool MatchesAdvancedSearch(Entity entity, string query)
        {
            // Parse query for special operators
            if (query.StartsWith("!"))
            {
                // Negation - entity must NOT have this component
                var componentName = query[1..];
                return !entity.HasComponent(componentName);
            }

            // Apply additional filters
            foreach (var filter in _filters)
            {
                if (!MatchesFilter(entity, filter))
                    return false;
            }

            return MatchesSimpleSearch(entity, query);
        }

        private bool MatchesFilter(Entity entity, SearchFilter filter)
        {
            return filter.Type switch
            {
                FilterType.HasComponent => entity.HasComponent(filter.Value),
                FilterType.MissingComponent => !entity.HasComponent(filter.Value),
                FilterType.EntityId => entity.Id.ToString().Contains(filter.Value),
                FilterType.PrefabName => GetPrefabName(entity).Contains(filter.Value),
                _ => true
            };
        }

        private void QuickSearch(string query)
        {
            _searchQuery = query;
            ExecuteSearch();
        }

        private string GetEntityName(Entity entity)
        {
            if (entity.HasComponent(nameof(PlayerTagComponent)))
                return "Player";

            if (entity.HasComponent(nameof(PrefabIdComponent)))
            {
                var prefab = entity.GetComponent<PrefabIdComponent>(nameof(PrefabIdComponent));
                return prefab?.PrefabName ?? $"Entity {entity.Id}";
            }

            return $"Entity {entity.Id}";
        }

        private string GetEntityIcon(Entity entity)
        {
            if (entity.HasComponent(nameof(PlayerTagComponent))) return User;
            if (entity.HasComponent(nameof(StatsComponent))) return Robot;
            if (entity.HasComponent(nameof(ObstacleComponent))) return Road;
            return Cube;
        }

        private string GetPrefabName(Entity entity)
        {
            if (entity.HasComponent(nameof(PrefabIdComponent)))
            {
                var prefab = entity.GetComponent<PrefabIdComponent>(nameof(PrefabIdComponent));
                return prefab?.PrefabName ?? "";
            }
            return "";
        }

        private string GetMatchReason(Entity entity)
        {
            // Simplified match reason - in a real implementation, track which criteria matched
            if (entity.HasComponent(nameof(PlayerTagComponent)))
                return "Has PlayerTagComponent";
            if (entity.HasComponent(nameof(StatsComponent)))
                return "Has StatsComponent";
            return "Name match";
        }

        private void ExportSearchResults()
        {
            Console.WriteLine($"[EntitySearch] Exporting {_searchResults.Count} search results:");
            Console.WriteLine($"Query: '{_searchQuery}'");
            Console.WriteLine("Results:");

            foreach (var entity in _searchResults.Take(20))
            {
                Console.WriteLine($"  - {GetEntityName(entity)} (ID: {entity.Id})");
            }

            if (_searchResults.Count > 20)
            {
                Console.WriteLine($"  ... and {_searchResults.Count - 20} more entities");
            }
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

        public void Dispose()
        {
            _searchResults.Clear();
            _searchHistory.Clear();
            _filters.Clear();
        }

        private enum SearchMode
        {
            Simple,
            Advanced
        }

        private enum FilterType
        {
            HasComponent,
            MissingComponent,
            EntityId,
            PrefabName
        }

        private class SearchFilter
        {
            public FilterType Type { get; set; }
            public string Value { get; set; } = "";
        }

        private class SearchQuery
        {
            public Guid Id { get; set; }
            public string Query { get; set; } = "";
            public DateTime Timestamp { get; set; }
            public int ResultCount { get; set; }
        }
    }
}