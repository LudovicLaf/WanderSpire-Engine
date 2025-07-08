// File: Game/Systems/UI/AssetDebugWindow.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WanderSpire.Scripting.UI;
using static WanderSpire.Scripting.UI.FontAwesome5;

namespace Game.Systems.UI
{
    /// <summary>
    /// Professional asset debug window for monitoring and managing game assets.
    /// </summary>
    public class AssetDebugWindow : ImGuiWindowBase
    {
        public override string Title => "Asset Manager";

        // Theme colors
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorDanger = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);

        // Asset tracking
        private readonly Dictionary<AssetType, List<AssetInfo>> _assetsByType = new();
        private AssetType _selectedAssetType = AssetType.All;
        private AssetInfo? _selectedAsset;
        private string _assetFilter = "";
        private bool _showLoadedOnly = false;

        // Asset statistics
        private long _totalMemoryUsage = 0;
        private int _loadedAssets = 0;
        private int _totalAssets = 0;

        public AssetDebugWindow()
        {
            InitializeAssets();
        }

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

            UpdateAssetStatistics();

            RenderHeader();
            ImGui.Separator();

            var contentRegion = ImGui.GetContentRegionAvail();
            float leftPanelWidth = 300;

            // Left panel - Asset browser
            RenderAssetBrowserPanel(leftPanelWidth, contentRegion.Y - 10);

            ImGui.SameLine();
            ImGuiExtensions.SeparatorEx(ImGuiSeparatorFlags.Vertical);
            ImGui.SameLine();

            // Right panel - Asset details
            RenderAssetDetailsPanel(contentRegion.X - leftPanelWidth - 20, contentRegion.Y - 10);

            ImGui.PopStyleVar(3);
            EndWindow();
        }

        private void InitializeAssets()
        {
            // Initialize with sample asset data
            _assetsByType[AssetType.Texture] = new List<AssetInfo>
            {
                new() { Name = "player_spritesheet.png", Size = 1024 * 1024 * 4, IsLoaded = true, Type = AssetType.Texture, RefCount = 1 },
                new() { Name = "orc_spritesheet.png", Size = 512 * 512 * 4, IsLoaded = true, Type = AssetType.Texture, RefCount = 2 },
                new() { Name = "tileset_dungeon.png", Size = 2048 * 2048 * 4, IsLoaded = true, Type = AssetType.Texture, RefCount = 1 },
                new() { Name = "ui_icons.png", Size = 256 * 256 * 4, IsLoaded = false, Type = AssetType.Texture, RefCount = 0 },
            };

            _assetsByType[AssetType.Audio] = new List<AssetInfo>
            {
                new() { Name = "background_music.ogg", Size = 8 * 1024 * 1024, IsLoaded = true, Type = AssetType.Audio, RefCount = 1 },
                new() { Name = "sword_clash.wav", Size = 256 * 1024, IsLoaded = true, Type = AssetType.Audio, RefCount = 3 },
                new() { Name = "footsteps.wav", Size = 128 * 1024, IsLoaded = false, Type = AssetType.Audio, RefCount = 0 },
            };

            _assetsByType[AssetType.Model] = new List<AssetInfo>
            {
                new() { Name = "character_base.fbx", Size = 2 * 1024 * 1024, IsLoaded = true, Type = AssetType.Model, RefCount = 5 },
                new() { Name = "weapon_sword.obj", Size = 512 * 1024, IsLoaded = false, Type = AssetType.Model, RefCount = 0 },
            };

            _assetsByType[AssetType.Script] = new List<AssetInfo>
            {
                new() { Name = "PlayerController.cs", Size = 16 * 1024, IsLoaded = true, Type = AssetType.Script, RefCount = 1 },
                new() { Name = "EnemyAI.cs", Size = 24 * 1024, IsLoaded = true, Type = AssetType.Script, RefCount = 3 },
                new() { Name = "GameManager.cs", Size = 32 * 1024, IsLoaded = true, Type = AssetType.Script, RefCount = 1 },
            };

            _assetsByType[AssetType.Prefab] = new List<AssetInfo>
            {
                new() { Name = "player.prefab", Size = 4 * 1024, IsLoaded = true, Type = AssetType.Prefab, RefCount = 1 },
                new() { Name = "orc.prefab", Size = 3 * 1024, IsLoaded = true, Type = AssetType.Prefab, RefCount = 2 },
                new() { Name = "treasure_chest.prefab", Size = 2 * 1024, IsLoaded = false, Type = AssetType.Prefab, RefCount = 0 },
            };
        }

        private void UpdateAssetStatistics()
        {
            _totalMemoryUsage = 0;
            _loadedAssets = 0;
            _totalAssets = 0;

            foreach (var assets in _assetsByType.Values)
            {
                foreach (var asset in assets)
                {
                    _totalAssets++;
                    if (asset.IsLoaded)
                    {
                        _loadedAssets++;
                        _totalMemoryUsage += asset.Size;
                    }
                }
            }
        }

        private void RenderHeader()
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(Archive);
            ImGui.PopStyleColor();
            ImGuiManager.Instance?.PopIconFont();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text("Asset Manager");
            ImGui.PopStyleColor();

            ImGui.SameLine();
            ImGui.TextColored(ColorDim, $"({_loadedAssets}/{_totalAssets} loaded)");

            // Memory usage indicator
            ImGui.SameLine(ImGui.GetWindowWidth() - 200);
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(ColorInfo, Memory);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.TextColored(ColorInfo, FormatBytes(_totalMemoryUsage));
        }

        private void RenderAssetBrowserPanel(float width, float height)
        {
            ImGui.BeginChild("##asset_browser", new Vector2(width, height), true);

            RenderAssetControls();
            ImGui.Separator();

            RenderAssetStatistics();
            ImGui.Separator();

            RenderAssetList();

            ImGui.EndChild();
        }

        private void RenderAssetControls()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorPrimary * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Asset Controls", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                // Asset type filter
                ImGui.Text("Asset Type:");
                ImGui.SetNextItemWidth(-1);
                string[] typeNames = Enum.GetNames<AssetType>();
                int typeIndex = (int)_selectedAssetType;
                if (ImGui.Combo("##asset_type", ref typeIndex, typeNames, typeNames.Length))
                {
                    _selectedAssetType = (AssetType)typeIndex;
                }

                // Search filter
                ImGui.Text("Search:");
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##asset_filter", ref _assetFilter, 128);

                // Options
                ImGui.Checkbox("Show Loaded Only", ref _showLoadedOnly);

                ImGui.Spacing();

                // Quick actions
                if (RenderIconButton(Sync, ColorPrimary, "Refresh Asset List"))
                {
                    Console.WriteLine("[AssetDebug] Refreshed asset list");
                }

                ImGui.SameLine();
                if (RenderIconButton(Trash, ColorDanger, "Unload Unused Assets"))
                {
                    Console.WriteLine("[AssetDebug] Unloaded unused assets");
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderAssetStatistics()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorInfo * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Statistics", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                ImGui.Columns(2, "##asset_stats", false);

                // Total memory
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(Memory);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Memory:");
                ImGui.NextColumn();
                ImGui.TextColored(ColorInfo, FormatBytes(_totalMemoryUsage));
                ImGui.NextColumn();

                // Loaded assets
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(CheckCircle);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Loaded:");
                ImGui.NextColumn();
                ImGui.TextColored(ColorSuccess, $"{_loadedAssets}/{_totalAssets}");
                ImGui.NextColumn();

                // Asset breakdown by type
                foreach (var assetType in Enum.GetValues<AssetType>())
                {
                    if (assetType == AssetType.All) continue;
                    if (!_assetsByType.TryGetValue(assetType, out var assets)) continue;

                    var loadedCount = assets.Count(a => a.IsLoaded);
                    var totalCount = assets.Count;

                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.Text(GetAssetTypeIcon(assetType));
                    ImGuiManager.Instance?.PopIconFont();
                    ImGui.SameLine();
                    ImGui.Text($"{assetType}:");
                    ImGui.NextColumn();
                    ImGui.TextColored(ColorDim, $"{loadedCount}/{totalCount}");
                    ImGui.NextColumn();
                }

                ImGui.Columns(1);
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderAssetList()
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorSuccess * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Asset List", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                var allAssets = GetFilteredAssets();

                foreach (var asset in allAssets)
                {
                    RenderAssetListItem(asset);
                }

                if (allAssets.Count == 0)
                {
                    ImGui.TextColored(ColorDim, "No assets match the current filter.");
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderAssetListItem(AssetInfo asset)
        {
            bool isSelected = _selectedAsset?.Name == asset.Name;
            var statusColor = asset.IsLoaded ? ColorSuccess : ColorDim;
            var typeIcon = GetAssetTypeIcon(asset.Type);

            ImGui.PushID(asset.Name);

            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Header, ColorPrimary * new Vector4(1, 1, 1, 0.3f));
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ColorPrimary * new Vector4(1, 1, 1, 0.4f));
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, ColorPrimary * new Vector4(1, 1, 1, 0.5f));
            }

            // Asset icon and status
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(statusColor, typeIcon);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();

            // Asset name
            if (ImGui.Selectable(asset.Name, isSelected))
            {
                _selectedAsset = asset;
            }

            if (isSelected)
                ImGui.PopStyleColor(3);

            // Context menu
            if (ImGui.BeginPopupContextItem($"##asset_ctx_{asset.Name}"))
            {
                if (ImGui.MenuItem(asset.IsLoaded ? "Unload" : "Load"))
                {
                    asset.IsLoaded = !asset.IsLoaded;
                    Console.WriteLine($"[AssetDebug] {(asset.IsLoaded ? "Loaded" : "Unloaded")} {asset.Name}");
                }

                if (ImGui.MenuItem("Copy Path"))
                {
                    Console.WriteLine($"Asset path: {asset.Name}");
                }

                if (asset.IsLoaded && ImGui.MenuItem("Reload"))
                {
                    Console.WriteLine($"[AssetDebug] Reloaded {asset.Name}");
                }

                ImGui.EndPopup();
            }

            ImGui.PopID();
        }

        private void RenderAssetDetailsPanel(float width, float height)
        {
            ImGui.BeginChild("##asset_details", new Vector2(width, height), true);

            if (_selectedAsset != null)
            {
                RenderSelectedAssetDetails();
            }
            else
            {
                RenderNoAssetSelectedPanel();
            }

            ImGui.EndChild();
        }

        private void RenderSelectedAssetDetails()
        {
            var asset = _selectedAsset!;
            var typeIcon = GetAssetTypeIcon(asset.Type);
            var statusColor = asset.IsLoaded ? ColorSuccess : ColorDim;

            // Header
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(statusColor, typeIcon);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text($"Asset Details: {asset.Name}");
            ImGui.PopStyleColor();

            ImGui.Separator();

            // Asset information
            RenderAssetInformation(asset);
            ImGui.Separator();

            // Asset controls
            RenderAssetControls(asset);
        }

        private void RenderAssetInformation(AssetInfo asset)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorInfo * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Asset Information", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                ImGui.Columns(2, "##asset_info", false);

                // Name
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(File);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Name:");
                ImGui.NextColumn();
                ImGui.TextColored(ColorInfo, asset.Name);
                ImGui.NextColumn();

                // Type
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(GetAssetTypeIcon(asset.Type));
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Type:");
                ImGui.NextColumn();
                ImGui.TextColored(ColorInfo, asset.Type.ToString());
                ImGui.NextColumn();

                // Size
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(Archive);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Size:");
                ImGui.NextColumn();
                ImGui.TextColored(ColorInfo, FormatBytes(asset.Size));
                ImGui.NextColumn();

                // Status
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(asset.IsLoaded ? CheckCircle : TimesCircle);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Status:");
                ImGui.NextColumn();
                ImGui.TextColored(asset.IsLoaded ? ColorSuccess : ColorDim,
                    asset.IsLoaded ? "Loaded" : "Unloaded");
                ImGui.NextColumn();

                // Reference count
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(Link);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("References:");
                ImGui.NextColumn();
                var refColor = asset.RefCount > 0 ? ColorSuccess : ColorWarning;
                ImGui.TextColored(refColor, asset.RefCount.ToString());
                ImGui.NextColumn();

                // Last modified (simulated)
                ImGuiManager.Instance?.PushIconFont();
                ImGui.Text(Clock);
                ImGuiManager.Instance?.PopIconFont();
                ImGui.SameLine();
                ImGui.Text("Modified:");
                ImGui.NextColumn();
                ImGui.TextColored(ColorDim, "2024-01-15 10:30:00");
                ImGui.NextColumn();

                ImGui.Columns(1);
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderAssetControls(AssetInfo asset)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, ColorSuccess * new Vector4(1, 1, 1, 0.2f));
            if (ImGui.CollapsingHeader("Asset Controls", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PopStyleColor();

                // Load/Unload
                if (asset.IsLoaded)
                {
                    if (RenderIconButton(Download, ColorWarning, "Unload Asset"))
                    {
                        asset.IsLoaded = false;
                        Console.WriteLine($"[AssetDebug] Unloaded {asset.Name}");
                    }
                }
                else
                {
                    if (RenderIconButton(Upload, ColorSuccess, "Load Asset"))
                    {
                        asset.IsLoaded = true;
                        Console.WriteLine($"[AssetDebug] Loaded {asset.Name}");
                    }
                }

                ImGui.SameLine();
                if (RenderIconButton(Sync, ColorPrimary, "Reload Asset"))
                {
                    Console.WriteLine($"[AssetDebug] Reloaded {asset.Name}");
                }

                ImGui.SameLine();
                if (RenderIconButton(Copy, ColorInfo, "Duplicate Asset"))
                {
                    Console.WriteLine($"[AssetDebug] Duplicated {asset.Name}");
                }

                ImGui.Spacing();

                // Advanced controls
                ImGui.Text("Advanced:");
                if (RenderIconButton(Cog, ColorWarning, "Asset Properties"))
                {
                    Console.WriteLine($"[AssetDebug] Opened properties for {asset.Name}");
                }

                ImGui.SameLine();
                if (RenderIconButton(Search, ColorInfo, "Find References"))
                {
                    Console.WriteLine($"[AssetDebug] Finding references for {asset.Name}");
                }

                ImGui.SameLine();
                if (RenderIconButton(Trash, ColorDanger, "Delete Asset"))
                {
                    Console.WriteLine($"[AssetDebug] Deleted {asset.Name}");
                }

                // Memory usage breakdown
                if (asset.IsLoaded)
                {
                    ImGui.Spacing();
                    ImGui.Text("Memory Usage:");
                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, ColorPrimary);
                    float memoryPercent = (float)asset.Size / Math.Max(_totalMemoryUsage, 1);
                    ImGui.ProgressBar(memoryPercent, new Vector2(-1, 0),
                        $"{FormatBytes(asset.Size)} ({memoryPercent * 100:F1}%)");
                    ImGui.PopStyleColor();
                }
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }

        private void RenderNoAssetSelectedPanel()
        {
            var center = ImGui.GetContentRegionAvail();
            ImGui.SetCursorPos(new Vector2(center.X / 2 - 100, center.Y / 2 - 50));

            ImGui.PushStyleColor(ImGuiCol.Text, ColorDim);
            ImGui.Text("No Asset Selected");
            ImGui.PopStyleColor();

            ImGui.SetCursorPosX(center.X / 2 - 120);
            ImGui.TextColored(ColorDim * new Vector4(1, 1, 1, 0.5f), "Select an asset from the browser");
        }

        private List<AssetInfo> GetFilteredAssets()
        {
            var filteredAssets = new List<AssetInfo>();

            foreach (var (type, assets) in _assetsByType)
            {
                if (_selectedAssetType != AssetType.All && type != _selectedAssetType)
                    continue;

                foreach (var asset in assets)
                {
                    if (_showLoadedOnly && !asset.IsLoaded)
                        continue;

                    if (!string.IsNullOrEmpty(_assetFilter) &&
                        !asset.Name.ToLower().Contains(_assetFilter.ToLower()))
                        continue;

                    filteredAssets.Add(asset);
                }
            }

            return filteredAssets.OrderBy(a => a.Type).ThenBy(a => a.Name).ToList();
        }

        private string GetAssetTypeIcon(AssetType type)
        {
            return type switch
            {
                AssetType.Texture => Image,
                AssetType.Audio => VolumeUp,
                AssetType.Model => Cube,
                AssetType.Script => Code,
                AssetType.Prefab => Box,
                _ => File
            };
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

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:F1} {suffixes[counter]}";
        }

        private enum AssetType
        {
            All,
            Texture,
            Audio,
            Model,
            Script,
            Prefab
        }

        private class AssetInfo
        {
            public string Name { get; set; } = "";
            public AssetType Type { get; set; }
            public long Size { get; set; }
            public bool IsLoaded { get; set; }
            public int RefCount { get; set; }
        }
    }
}