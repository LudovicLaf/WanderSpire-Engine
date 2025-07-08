using Game.Prefabs;
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
    /// Real‑time prefab explorer with spawning & mass‑destruction utilities.
    /// </summary>
    public sealed class PrefabDebugWindow : ImGuiWindowBase, IDisposable
    {
        public override string Title => "Prefab Explorer";

        // ───────────────────────── theme ─────────────────────────
        private readonly Vector4 _primary = new(0.22f, 0.58f, 0.93f, 1f);
        private readonly Vector4 _success = new(0.22f, 0.80f, 0.46f, 1f);
        private readonly Vector4 _danger = new(0.93f, 0.23f, 0.29f, 1f);
        private readonly Vector4 _dim = new(0.55f, 0.55f, 0.58f, 1f);

        // ───────────────────────── state ─────────────────────────
        private readonly Dictionary<string, int> _counts = new(StringComparer.OrdinalIgnoreCase);
        private string _filter = string.Empty;

        private string _spawnKey = "orc";
        private int _spawnX;
        private int _spawnY;
        private bool _useCursor = true;

        public override void Render()
        {
            UpdateCounts();

            if (!BeginWindow(ImGuiWindowFlags.MenuBar))
            {
                EndWindow();
                return;
            }

            // Push common style *after* we know the window is open to prevent mismatches.
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(14, 12));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 6));

            try
            {
                RenderMenuBar();
                ImGui.Separator();

                RenderTable();
                ImGui.Separator();
                RenderSpawner();
            }
            finally
            {
                ImGui.PopStyleVar(2); // always balance the Pushes above
                EndWindow();
            }
        }

        // ───────────────────────── menu bar ─────────────────────────
        private void RenderMenuBar()
        {
            if (!ImGui.BeginMenuBar()) return;

            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(_primary, Boxes);
            ImGuiManager.Instance?.PopIconFont();
            ImGui.SameLine();
            ImGui.TextUnformatted(" Active Prefabs");

            ImGui.SameLine(ImGui.GetWindowWidth() - 240);
            ImGui.SetNextItemWidth(200);
            ImGui.InputTextWithHint("##pf_filter", " filter …", ref _filter, 64);

            ImGui.EndMenuBar();
        }

        // ───────────────────────── counts table ─────────────────────────
        private void RenderTable()
        {
            if (!_counts.Any())
            {
                ImGui.TextColored(_dim, "No prefabs in scene.");
                return;
            }

            if (!ImGui.BeginTable("##pf_table", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                return;

            try
            {
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 28);
                ImGui.TableSetupColumn("Prefab", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableHeadersRow();

                foreach (var kv in _counts.OrderByDescending(k => k.Value))
                {
                    if (!string.IsNullOrWhiteSpace(_filter) &&
                        !kv.Key.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    ImGui.TableNextColumn();
                    ImGuiManager.Instance?.PushIconFont();
                    ImGui.TextColored(_primary, Cube);
                    ImGuiManager.Instance?.PopIconFont();

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(kv.Key);

                    ImGui.TableNextColumn();
                    ImGui.TextColored(_success, kv.Value.ToString());
                }
            }
            finally { ImGui.EndTable(); }
        }

        // ───────────────────────── spawner ─────────────────────────
        private void RenderSpawner()
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.TextColored(_primary, PlusSquare + "  Spawn Prefab");
            ImGuiManager.Instance?.PopIconFont();
            ImGui.Spacing();

            ImGui.SetNextItemWidth(160);
            ImGui.InputText("Prefab", ref _spawnKey, 48);

            ImGui.Checkbox("Use mouse cursor", ref _useCursor);
            if (!_useCursor)
            {
                ImGui.InputInt("X", ref _spawnX);
                ImGui.SameLine();
                ImGui.InputInt("Y", ref _spawnY);
            }

            ImGui.SameLine();
            if (RenderIconButton(Cube, _success, "Spawn (Enter)") || Input.GetKeyDown(KeyCode.Return))
            {
                SpawnPrefab();
            }

            ImGui.SameLine();
            if (RenderIconButton(Trash, _danger, "Destroy NON-players"))
            {
                DestroyNonPlayers();
            }
        }

        private bool RenderIconButton(string icon, Vector4 col, string tooltip)
        {
            ImGuiManager.Instance?.PushIconFont();
            ImGui.PushStyleColor(ImGuiCol.Button, col * new Vector4(1, 1, 1, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, col);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));
            Vector2 sz = ImGui.CalcTextSize(icon) + new Vector2(12, 6);
            bool clicked = ImGui.Button(icon, sz);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            ImGui.PopStyleColor(3);
            ImGuiManager.Instance?.PopIconFont();
            return clicked;
        }

        private void SpawnPrefab()
        {
            try
            {
                int tx = _spawnX, ty = _spawnY;
                if (_useCursor)
                {
                    var ctx = Engine.Instance.Context;
                    WanderSpire.Scripting.EngineInterop.Engine_GetMouseTile(ctx, out tx, out ty);
                }
                PrefabRegistry.SpawnAtTile(_spawnKey, tx, ty);
                Console.WriteLine($"[PrefabDebug] +{_spawnKey} @ ({tx},{ty})");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[PrefabDebug] Spawn failed: {ex.Message}");
            }
        }

        private void DestroyNonPlayers()
        {
            var toKill = new List<Entity>();
            World.ForEachEntity(e => { if (!e.HasComponent(nameof(PlayerTagComponent))) toKill.Add(e); });
            foreach (var e in toKill) Engine.Instance.DestroyEntity(e);
            Console.WriteLine($"[PrefabDebug] Destroyed {toKill.Count} non-player entities");
        }

        // ───────────────────────── util ─────────────────────────
        private void UpdateCounts()
        {
            _counts.Clear();
            World.ForEachEntity(e =>
            {
                var p = e.GetComponent<PrefabIdComponent>(nameof(PrefabIdComponent));
                if (p == null || string.IsNullOrEmpty(p.PrefabName)) return;
                _counts[p.PrefabName] = _counts.GetValueOrDefault(p.PrefabName) + 1;
            });
        }

        public void Dispose() { /* placeholder for future cleanup */ }
    }
}
