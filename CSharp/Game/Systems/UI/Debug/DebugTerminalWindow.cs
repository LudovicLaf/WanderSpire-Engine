// Game/Systems/UI/DebugTerminalWindow.cs

using Game.Dto;
using Game.Prefabs;
using ScriptHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using WanderSpire.Components;
using WanderSpire.Scripting;
using WanderSpire.Scripting.UI;

namespace Game.Systems.UI
{
    public unsafe class DebugTerminalWindow : ImGuiWindowBase, IDisposable
    {
        public override string Title => "Debug Terminal";
        public bool IsVisible { get; set; } = false;

        // Terminal state
        private readonly List<LogEntry> _log = new();
        private string _input = string.Empty;
        private bool _focusInput = false;
        private int _scrollToBottom = 0;

        // Command system
        private readonly CommandRegistry _commandRegistry;
        private readonly List<string> _history = new();
        private int _historyPos = -1;

        // Auto-completion
        private bool _showAutocomplete = false;
        private List<AutocompleteItem> _autocompleteItems = new();
        private int _autocompleteIndex = 0;
        private string _autocompleteBase = "";

        // Search
        private bool _showSearch = false;
        private string _searchText = "";
        private List<int> _searchMatches = new();
        private int _currentSearchMatch = -1;

        // UI State
        private float _terminalAlpha = 0.95f;
        private bool _showTimestamps = true;
        private bool _enableRichText = true;
        private readonly Dictionary<string, object> _variables = new();

        // Input callback delegate
        private unsafe ImGuiInputTextCallback _inputCallback;

        // Theme colors
        private readonly Vector4 ColorBackground = new(0.08f, 0.08f, 0.10f, 1.0f);
        private readonly Vector4 ColorPrimary = new(0.26f, 0.59f, 0.98f, 1.0f);
        private readonly Vector4 ColorSuccess = new(0.40f, 0.86f, 0.40f, 1.0f);
        private readonly Vector4 ColorWarning = new(0.98f, 0.75f, 0.35f, 1.0f);
        private readonly Vector4 ColorError = new(0.98f, 0.35f, 0.35f, 1.0f);
        private readonly Vector4 ColorInfo = new(0.65f, 0.85f, 1.0f, 1.0f);
        private readonly Vector4 ColorDim = new(0.55f, 0.55f, 0.58f, 1.0f);
        private readonly Vector4 ColorAccent = new(0.75f, 0.45f, 0.98f, 1.0f);

        public DebugTerminalWindow()
        {
            _commandRegistry = new CommandRegistry();
            _inputCallback = InputCallback;
            RegisterBuiltInCommands();
            LogSystem("Terminal initialized. Type 'help' for commands.");
        }

        private void RegisterBuiltInCommands()
        {
            // System commands
            _commandRegistry.Register(new Command
            {
                Name = "help",
                Description = "Display available commands",
                Category = "System",
                Action = CmdHelp,
                Syntax = "help [command]"
            });

            _commandRegistry.Register(new Command
            {
                Name = "clear",
                Description = "Clear terminal output",
                Category = "System",
                Aliases = new[] { "cls" },
                Action = args => { _log.Clear(); _scrollToBottom = 2; }
            });

            _commandRegistry.Register(new Command
            {
                Name = "history",
                Description = "Show command history",
                Category = "System",
                Action = args =>
                {
                    LogInfo($"Command History ({_history.Count} entries):");
                    for (int i = 0; i < _history.Count; i++)
                        Log($"  {i + 1}: {_history[i]}", LogType.Normal);
                }
            });

            _commandRegistry.Register(new Command
            {
                Name = "set",
                Description = "Set a variable",
                Category = "System",
                Syntax = "set <name> <value>",
                Action = CmdSet
            });

            _commandRegistry.Register(new Command
            {
                Name = "echo",
                Description = "Echo text or variable",
                Category = "System",
                Action = args => Log(string.Join(" ", args), LogType.Normal)
            });

            _commandRegistry.Register(new Command
            {
                Name = "alias",
                Description = "Create command alias",
                Category = "System",
                Syntax = "alias <name> <command>",
                Action = CmdAlias
            });

            // Player commands
            _commandRegistry.Register(new Command
            {
                Name = "player.hp",
                Description = "Get/set player HP",
                Category = "Player",
                Syntax = "player.hp [value]",
                Action = CmdPlayerHP
            });

            _commandRegistry.Register(new Command
            {
                Name = "player.stats",
                Description = "Display player stats",
                Category = "Player",
                Action = CmdPlayerStats
            });

            //_commandRegistry.Register(new Command
            //{
            //    Name = "player.teleport",
            //    Description = "Teleport player to coordinates",
            //    Category = "Player",
            //    Syntax = "player.teleport <x> <y>",
            //    Action = CmdPlayerTeleport
            //});

            // World commands
            _commandRegistry.Register(new Command
            {
                Name = "spawn",
                Description = "Spawn entity at location",
                Category = "World",
                Syntax = "spawn <prefab> <x> <y> [count]",
                Action = CmdSpawn
            });

            _commandRegistry.Register(new Command
            {
                Name = "time",
                Description = "Get/set game time",
                Category = "World",
                Syntax = "time [set <hour>]",
                Action = CmdTime
            });

            _commandRegistry.Register(new Command
            {
                Name = "entities",
                Description = "List all entities",
                Category = "World",
                Action = CmdEntities
            });
        }

        public override void Render()
        {
            if (!IsVisible) return;

            ImGui.PushStyleColor(ImGuiCol.WindowBg, ColorBackground * new Vector4(1, 1, 1, _terminalAlpha));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12, 12));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6.0f);

            var flags = ImGuiWindowFlags.NoScrollbar;
            if (!BeginWindow(flags))
            {
                ImGui.PopStyleVar(3);
                ImGui.PopStyleColor();
                EndWindow();
                return;
            }

            RenderHeader();
            RenderLog();
            RenderInputBar();

            if (_showAutocomplete)
                RenderAutocomplete();

            if (_showSearch)
                RenderSearchOverlay();

            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor();
            EndWindow();
        }

        private void RenderHeader()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text($"◆ Terminal");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ColorDim);
            ImGui.Text($"({_log.Count} lines)");
            ImGui.PopStyleColor(2);

            ImGui.SameLine(ImGui.GetWindowWidth() - 180);

            // Quick action buttons
            if (ImGuiButton("●", ColorSuccess, "Clear"))
                ExecuteCommand("clear");

            ImGui.SameLine();
            if (ImGuiButton(_showTimestamps ? "◐" : "○", ColorInfo, "Toggle timestamps"))
                _showTimestamps = !_showTimestamps;

            ImGui.SameLine();
            if (ImGuiButton("⌕", ColorAccent, "Search (Ctrl+F)"))
                ToggleSearch();

            ImGui.Separator();
        }

        private void RenderLog()
        {
            float windowWidth = ImGui.GetContentRegionAvail().X;
            float windowHeight = ImGui.GetContentRegionAvail().Y;
            float logHeight = windowHeight - 70.0f;

            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0, 0, 0, 0.2f));
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 6.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));

            ImGui.BeginChild("##log", new Vector2(-1, logHeight), true);

            // Render visible log entries
            int visibleStart = Math.Max(0, _log.Count - 500);
            for (int i = visibleStart; i < _log.Count; i++)
            {
                var entry = _log[i];

                // Highlight search matches
                bool isSearchMatch = _searchMatches.Contains(i);
                if (isSearchMatch)
                {
                    ImGui.PushStyleColor(ImGuiCol.ChildBg, ColorWarning * new Vector4(1, 1, 1, 0.2f));
                    ImGui.BeginChild($"##match{i}", new Vector2(-1, ImGui.GetTextLineHeight()), false);
                }

                // Timestamp
                if (_showTimestamps)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ColorDim * new Vector4(1, 1, 1, 0.7f));
                    ImGui.Text($"[{entry.Timestamp:HH:mm:ss}]");
                    ImGui.SameLine();
                    ImGui.PopStyleColor();
                }

                // Log content with color
                var color = GetLogColor(entry.Type);
                ImGui.PushStyleColor(ImGuiCol.Text, color);

                if (entry.Type == LogType.Command)
                {
                    ImGui.Text(">");
                    ImGui.SameLine();
                    RenderSyntaxHighlightedCommand(entry.Text);
                }
                else
                {
                    ImGui.TextWrapped(entry.Text);
                }

                ImGui.PopStyleColor();

                if (isSearchMatch)
                {
                    ImGui.EndChild();
                    ImGui.PopStyleColor();
                }
            }

            if (_scrollToBottom > 0)
            {
                ImGui.SetScrollHereY(1.0f);
                _scrollToBottom--;
            }

            ImGui.EndChild();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
        }

        private void RenderInputBar()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(10, 8));
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0, 0, 0.4f));

            // Input field
            ImGui.PushItemWidth(-90);

            bool submitted = ImGui.InputText("##input", ref _input, 512,
                ImGuiInputTextFlags.CallbackHistory |
                ImGuiInputTextFlags.CallbackCompletion |
                ImGuiInputTextFlags.CallbackAlways |
                ImGuiInputTextFlags.EnterReturnsTrue,
                _inputCallback);

            ImGui.PopItemWidth();

            // Execute button
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, ColorPrimary * new Vector4(1, 1, 1, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorPrimary);
            if (ImGui.Button("Execute", new Vector2(80, 0)) || submitted)
            {
                ExecuteCommand(_input.Trim());
                _input = string.Empty;
                _focusInput = true;
                _scrollToBottom = 2;
            }
            ImGui.PopStyleColor(2);

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();

            if (_focusInput)
            {
                ImGui.SetKeyboardFocusHere(-1);
                _focusInput = false;
            }
        }

        private void RenderAutocomplete()
        {
            if (_autocompleteItems.Count == 0) return;

            var windowPos = ImGui.GetWindowPos();
            var cursorPos = ImGui.GetCursorPos();
            var startPos = new Vector2(windowPos.X + 20, windowPos.Y + ImGui.GetWindowHeight() - 80);

            ImGui.SetNextWindowPos(startPos);
            ImGui.SetNextWindowSize(new Vector2(300, Math.Min(200, _autocompleteItems.Count * 25 + 10)));

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6.0f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, ColorBackground * new Vector4(1, 1, 1, 0.98f));

            if (ImGui.Begin("##autocomplete", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                for (int i = 0; i < _autocompleteItems.Count; i++)
                {
                    var item = _autocompleteItems[i];
                    bool isSelected = i == _autocompleteIndex;

                    if (isSelected)
                        ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);

                    // Icon based on type
                    string icon = item.Type switch
                    {
                        AutocompleteType.Command => "►",
                        AutocompleteType.Variable => "$",
                        AutocompleteType.Alias => "@",
                        _ => "•"
                    };

                    ImGui.Text($"{icon} {item.Text}");

                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Text, ColorDim);
                        ImGui.Text($"- {item.Description}");
                        ImGui.PopStyleColor();
                    }

                    if (isSelected)
                        ImGui.PopStyleColor();
                }
            }
            ImGui.End();

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }

        private void RenderSearchOverlay()
        {
            ImGui.SetNextWindowPos(new Vector2(ImGui.GetWindowPos().X + ImGui.GetWindowWidth() - 320, ImGui.GetWindowPos().Y + 60));
            ImGui.SetNextWindowSize(new Vector2(300, 60));

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6.0f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, ColorBackground);

            if (ImGui.Begin("##search", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                ImGui.Text("Search:");
                ImGui.SameLine();

                if (ImGui.InputText("##searchinput", ref _searchText, 128))
                    PerformSearch();

                if (_searchMatches.Count > 0)
                {
                    ImGui.SameLine();
                    ImGui.Text($"{_currentSearchMatch + 1}/{_searchMatches.Count}");
                }
            }
            ImGui.End();

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }

        private unsafe int InputCallback(ImGuiInputTextCallbackData* data)
        {
            if (data->EventFlag == ImGuiInputTextFlags.CallbackHistory)
            {
                if (data->EventKey == ImGuiKey.UpArrow)
                {
                    if (_history.Count > 0 && _historyPos < _history.Count - 1)
                    {
                        _historyPos++;
                        UpdateInputFromHistory(data);
                    }
                }
                else if (data->EventKey == ImGuiKey.DownArrow)
                {
                    if (_historyPos > -1)
                    {
                        _historyPos--;
                        UpdateInputFromHistory(data);
                    }
                }
            }
            else if (data->EventFlag == ImGuiInputTextFlags.CallbackCompletion)
            {
                HandleTabCompletion(data);
            }
            else if (data->EventFlag == ImGuiInputTextFlags.CallbackAlways)
            {
                UpdateAutocomplete(data);
            }

            return 0;
        }

        private unsafe void UpdateInputFromHistory(ImGuiInputTextCallbackData* data)
        {
            string newText = _historyPos >= 0 ? _history[_history.Count - 1 - _historyPos] : "";
            data->DeleteChars(0, data->BufTextLen);
            data->InsertChars(0, newText);
        }

        private unsafe void HandleTabCompletion(ImGuiInputTextCallbackData* data)
        {
            if (_autocompleteItems.Count > 0)
            {
                var item = _autocompleteItems[_autocompleteIndex];
                data->DeleteChars(0, data->BufTextLen);
                data->InsertChars(0, item.Text);
                _showAutocomplete = false;
            }
        }

        private unsafe void UpdateAutocomplete(ImGuiInputTextCallbackData* data)
        {
            string currentInput = new string((char*)data->Buf, 0, data->BufTextLen);

            if (string.IsNullOrWhiteSpace(currentInput))
            {
                _showAutocomplete = false;
                return;
            }

            // Get autocomplete suggestions
            _autocompleteItems.Clear();
            _autocompleteBase = currentInput.ToLower();

            // Add matching commands
            foreach (var cmd in _commandRegistry.GetCommands())
            {
                if (cmd.Name.StartsWith(_autocompleteBase, StringComparison.OrdinalIgnoreCase))
                {
                    _autocompleteItems.Add(new AutocompleteItem
                    {
                        Text = cmd.Name,
                        Description = cmd.Description,
                        Type = AutocompleteType.Command
                    });
                }
            }

            // Add matching variables
            foreach (var var in _variables)
            {
                if (var.Key.StartsWith(_autocompleteBase, StringComparison.OrdinalIgnoreCase))
                {
                    _autocompleteItems.Add(new AutocompleteItem
                    {
                        Text = $"${var.Key}",
                        Description = var.Value?.ToString(),
                        Type = AutocompleteType.Variable
                    });
                }
            }

            _showAutocomplete = _autocompleteItems.Count > 0;
            _autocompleteIndex = 0;
        }

        private void ExecuteCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            // Add to history
            if (_history.Count == 0 || _history[^1] != input)
                _history.Add(input);
            _historyPos = -1;

            // Log command
            Log(input, LogType.Command);

            // Parse and execute
            try
            {
                var expanded = ExpandVariables(input);
                var parts = ParseCommandLine(expanded);

                if (parts.Length == 0) return;

                string cmdName = parts[0];
                string[] args = parts.Skip(1).ToArray();

                if (!_commandRegistry.Execute(cmdName, args))
                {
                    LogError($"Unknown command '{cmdName}'. Type 'help' for available commands.");
                }
            }
            catch (Exception ex)
            {
                LogError($"Command execution failed: {ex.Message}");
            }
        }

        private string[] ParseCommandLine(string input)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            bool escape = false;

            foreach (char c in input)
            {
                if (escape)
                {
                    current.Append(c);
                    escape = false;
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                parts.Add(current.ToString());

            return parts.ToArray();
        }

        private string ExpandVariables(string input)
        {
            return Regex.Replace(input, @"\$(\w+)", match =>
            {
                string varName = match.Groups[1].Value;
                return _variables.TryGetValue(varName, out var value) ? value.ToString() : match.Value;
            });
        }

        private void RenderSyntaxHighlightedCommand(string command)
        {
            var parts = command.Split(' ', 2);

            // Command name
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPrimary);
            ImGui.Text(parts[0]);
            ImGui.PopStyleColor();

            // Arguments
            if (parts.Length > 1)
            {
                ImGui.SameLine();
                ImGui.Text(parts[1]);
            }
        }

        // Command implementations
        private void CmdHelp(string[] args)
        {
            if (args.Length > 0)
            {
                var cmd = _commandRegistry.GetCommand(args[0]);
                if (cmd != null)
                {
                    LogInfo($"◆ {cmd.Name}");
                    Log($"  {cmd.Description}", LogType.Normal);
                    if (!string.IsNullOrEmpty(cmd.Syntax))
                        Log($"  Syntax: {cmd.Syntax}", LogType.Normal);
                    if (cmd.Aliases?.Length > 0)
                        Log($"  Aliases: {string.Join(", ", cmd.Aliases)}", LogType.Normal);
                }
                else
                {
                    LogError($"Unknown command: {args[0]}");
                }
            }
            else
            {
                LogInfo("Available Commands:");

                var categories = _commandRegistry.GetCommands()
                    .GroupBy(c => c.Category ?? "Uncategorized")
                    .OrderBy(g => g.Key);

                foreach (var category in categories)
                {
                    Log($"\n◆ {category.Key}", LogType.Info);
                    foreach (var cmd in category.OrderBy(c => c.Name))
                    {
                        Log($"  {cmd.Name,-20} {cmd.Description}", LogType.Normal);
                    }
                }

                Log("\nType 'help <command>' for detailed information.", LogType.Dim);
            }
        }

        private void CmdSet(string[] args)
        {
            if (args.Length < 2)
            {
                LogError("Usage: set <name> <value>");
                return;
            }

            string name = args[0];
            string value = string.Join(" ", args.Skip(1));

            _variables[name] = value;
            LogSuccess($"Set ${name} = {value}");
        }

        private void CmdAlias(string[] args)
        {
            if (args.Length < 2)
            {
                LogError("Usage: alias <name> <command>");
                return;
            }

            string aliasName = args[0];
            string command = string.Join(" ", args.Skip(1));

            _commandRegistry.Register(new Command
            {
                Name = aliasName,
                Description = $"Alias for: {command}",
                Category = "Aliases",
                Action = aliasArgs => ExecuteCommand(command + " " + string.Join(" ", aliasArgs))
            });

            LogSuccess($"Created alias '{aliasName}' → '{command}'");
        }

        private void CmdPlayerHP(string[] args)
        {
            var player = FindPlayer();
            if (player == null)
            {
                LogError("Player entity not found.");
                return;
            }

            var stats = player.GetScriptData<StatsComponent>(nameof(StatsComponent));
            if (stats == null)
            {
                LogError("Player has no StatsComponent.");
                return;
            }

            if (args.Length == 0)
            {
                LogInfo($"Player HP: {stats.CurrentHitpoints}/{stats.MaxHitpoints}");

                // Visual HP bar
                float hpPercent = (float)stats.CurrentHitpoints / stats.MaxHitpoints;
                int barWidth = 20;
                int filled = (int)(barWidth * hpPercent);
                string bar = "[" + new string('█', filled) + new string('░', barWidth - filled) + "]";

                Log($"  {bar} {hpPercent:P0}", LogType.Normal);
            }
            else if (int.TryParse(args[0], out int newHp))
            {
                stats.CurrentHitpoints = Math.Clamp(newHp, 0, stats.MaxHitpoints);
                player.SetScriptData(nameof(StatsComponent), stats);
                LogSuccess($"Set player HP to {stats.CurrentHitpoints}/{stats.MaxHitpoints}");
            }
            else
            {
                LogError($"Invalid HP value: {args[0]}");
            }
        }

        private void CmdPlayerStats(string[] args)
        {
            var player = FindPlayer();
            if (player == null)
            {
                LogError("Player entity not found.");
                return;
            }

            LogInfo("Player Statistics:");

            // Get all components
            var stats = player.GetScriptData<StatsComponent>(nameof(StatsComponent));
            if (stats != null)
            {
                Log($"  ◆ Health: {stats.CurrentHitpoints}/{stats.MaxHitpoints}", LogType.Success);
                Log($"  ◆ Mana: {stats.CurrentMana}/{stats.MaxMana}", LogType.Info);
            }

            // Position
            var transform = player.GetScriptData<TransformComponent>(nameof(TransformComponent));
            if (transform != null)
            {
                Log($"  ◆ Position: ({transform.LocalPosition:F1})", LogType.Normal);
            }
        }

        //private void CmdPlayerTeleport(string[] args)
        //{
        //    if (args.Length < 2 || !float.TryParse(args[0], out float x) || !float.TryParse(args[1], out float y))
        //    {
        //        LogError("Usage: player.teleport <x> <y>");
        //        return;
        //    }

        //    var player = FindPlayer();
        //    if (player == null)
        //    {
        //        LogError("Player entity not found.");
        //        return;
        //    }

        //    var transform = player.GetScriptData<TransformComponent>(nameof(TransformComponent));
        //    if (transform != null)
        //    {
        //        transform.X = x;
        //        transform.Y = y;
        //        player.SetScriptData(nameof(TransformComponent), transform);
        //        LogSuccess($"Teleported player to ({x:F1}, {y:F1})");
        //    }
        //}

        private void CmdSpawn(string[] args)
        {
            if (args.Length < 3 || !int.TryParse(args[1], out int x) || !int.TryParse(args[2], out int y))
            {
                LogError("Usage: spawn <prefab> <x> <y> [count]");
                return;
            }

            string prefabName = args[0];
            int count = args.Length > 3 && int.TryParse(args[3], out int c) ? c : 1;

            try
            {
                for (int i = 0; i < count; i++)
                {
                    var entity = PrefabRegistry.SpawnAtTile(prefabName, x + i, y);
                    LogSuccess($"Spawned '{prefabName}' at ({x + i},{y}) [ID: {entity.Id}]");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to spawn '{prefabName}': {ex.Message}");
            }
        }

        private void CmdTime(string[] args)
        {
            if (args.Length == 0)
            {
                // Get current time - implementation depends on your game
                LogInfo("Current game time: 12:00"); // Placeholder
            }
            else if (args.Length >= 2 && args[0] == "set" && int.TryParse(args[1], out int hour))
            {
                // Set time - implementation depends on your game
                LogSuccess($"Set game time to {hour:00}:00");
            }
            else
            {
                LogError("Usage: time [set <hour>]");
            }
        }

        private void CmdEntities(string[] args)
        {
            int count = 0;
            var entityTypes = new Dictionary<string, int>();

            World.ForEachEntity(entity =>
            {
                count++;
                string type = entity.GetType().Name;
                entityTypes[type] = entityTypes.GetValueOrDefault(type) + 1;
            });

            LogInfo($"Total Entities: {count}");
            foreach (var kvp in entityTypes.OrderByDescending(k => k.Value))
            {
                Log($"  ◆ {kvp.Key}: {kvp.Value}", LogType.Normal);
            }
        }

        // Search functionality
        private void ToggleSearch()
        {
            _showSearch = !_showSearch;
            if (!_showSearch)
            {
                _searchText = "";
                _searchMatches.Clear();
            }
        }

        private void PerformSearch()
        {
            _searchMatches.Clear();
            if (string.IsNullOrEmpty(_searchText)) return;

            for (int i = 0; i < _log.Count; i++)
            {
                if (_log[i].Text.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    _searchMatches.Add(i);
            }

            _currentSearchMatch = _searchMatches.Count > 0 ? 0 : -1;
        }

        // Logging methods
        private void Log(string message, LogType type)
        {
            _log.Add(new LogEntry
            {
                Text = message,
                Type = type,
                Timestamp = DateTime.Now
            });
            _scrollToBottom = 2;
        }

        private void LogInfo(string message) => Log(message, LogType.Info);
        private void LogSuccess(string message) => Log(message, LogType.Success);
        private void LogWarning(string message) => Log(message, LogType.Warning);
        private void LogError(string message) => Log(message, LogType.Error);
        private void LogSystem(string message) => Log(message, LogType.System);

        private Vector4 GetLogColor(LogType type) => type switch
        {
            LogType.Command => ColorPrimary,
            LogType.Success => ColorSuccess,
            LogType.Warning => ColorWarning,
            LogType.Error => ColorError,
            LogType.Info => ColorInfo,
            LogType.System => ColorAccent,
            LogType.Dim => ColorDim,
            _ => new Vector4(1, 1, 1, 1)
        };

        private Entity? FindPlayer()
        {
            Entity? result = null;
            World.ForEachEntity(ent =>
            {
                if (result == null && ent.HasComponent(nameof(PlayerTagComponent)))
                    result = ent;
            });
            return result;
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

        public void Dispose()
        {
            _log.Clear();
            _history.Clear();
            _autocompleteItems.Clear();
            _variables.Clear();
        }
    }

    // Supporting classes
    public class LogEntry
    {
        public string Text { get; set; }
        public LogType Type { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum LogType
    {
        Normal,
        Command,
        Success,
        Warning,
        Error,
        Info,
        System,
        Dim
    }

    public class Command
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Syntax { get; set; }
        public string[] Aliases { get; set; }
        public Action<string[]> Action { get; set; }
    }

    public class CommandRegistry
    {
        private readonly Dictionary<string, Command> _commands = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);

        public void Register(Command command)
        {
            _commands[command.Name] = command;

            if (command.Aliases != null)
            {
                foreach (var alias in command.Aliases)
                    _aliases[alias] = command.Name;
            }
        }

        public bool Execute(string name, string[] args)
        {
            // Check aliases
            if (_aliases.TryGetValue(name, out string actualName))
                name = actualName;

            if (_commands.TryGetValue(name, out var command))
            {
                command.Action(args);
                return true;
            }

            return false;
        }

        public Command GetCommand(string name)
        {
            if (_aliases.TryGetValue(name, out string actualName))
                name = actualName;

            return _commands.GetValueOrDefault(name);
        }

        public IEnumerable<Command> GetCommands() => _commands.Values;
    }

    public class AutocompleteItem
    {
        public string Text { get; set; }
        public string Description { get; set; }
        public AutocompleteType Type { get; set; }
    }

    public enum AutocompleteType
    {
        Command,
        Variable,
        Alias,
        File
    }
}