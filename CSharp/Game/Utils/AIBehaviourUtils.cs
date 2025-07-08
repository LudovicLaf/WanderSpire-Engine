// Game/Behaviours/AIBehaviourUtils.cs
using Game.Dto;
using Game.Events;
using ScriptHost;
using System;
using System.Collections.Generic;
using System.Linq;
using WanderSpire.Components;
using WanderSpire.Scripting;
using WanderSpire.Scripting.Utils;

namespace Game.Behaviours
{
    public static class AIBehaviourUtils
    {
        // Simple, per‐tick cache of everything we need for AI decisions
        public struct EntityInfo
        {
            public int Id;
            public (int X, int Y) Pos;
            public FactionComponent Fc;
            public string[] HostileFactions;
            public StatsComponent St;
            public bool IsPlayer;
        }

        private static List<EntityInfo> _cache = new();
        private static readonly HashSet<int> _initialized = new();
        private static readonly Dictionary<int, FactionComponent> _fcCache = new();
        private static readonly Dictionary<int, string[]> _hfCache = new();
        private static readonly Dictionary<int, StatsComponent> _stCache = new();

        static AIBehaviourUtils()
        {
            EventBus.LogicTick += OnLogicTick;
            GameEventBus.Event<HurtEvent>.Subscribe(OnHurt);
            GameEventBus.Event<DeathEvent>.Subscribe(OnDeath);
        }

        private static void OnLogicTick(WanderSpire.Core.Events.LogicTickEvent ev)
        {
            var list = new List<EntityInfo>();
            World.ForEachEntity(ent =>
            {
                if (!ent.HasComponent(nameof(GridPositionComponent))) return;

                var gp = ent.GetComponent<GridPositionComponent>(nameof(GridPositionComponent));
                if (gp == null) return;

                var id = ent.Id;
                if (!_initialized.Contains(id))
                {
                    var fc = ent.GetScriptData<FactionComponent>(nameof(FactionComponent)) ?? new();
                    _fcCache[id] = fc;
                    _hfCache[id] = (fc.HostileToFactions ?? "")
                                      .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim())
                                      .ToArray();
                    var st = ent.GetScriptData<StatsComponent>(nameof(StatsComponent)) ?? new();
                    _stCache[id] = st;
                    _initialized.Add(id);
                }

                list.Add(new EntityInfo
                {
                    Id = id,
                    Pos = gp.AsTuple(),
                    Fc = _fcCache[id],
                    HostileFactions = _hfCache[id],
                    St = _stCache[id],
                    IsPlayer = ent.HasComponent(nameof(PlayerTagComponent))
                });
            });
            _cache = list;
        }

        private static void OnHurt(HurtEvent ev)
        {
            int id = (int)ev.EntityId;
            if (_stCache.TryGetValue(id, out var st))
                st.CurrentHitpoints = Math.Max(0, st.CurrentHitpoints - ev.Damage);
        }

        private static void OnDeath(DeathEvent ev)
        {
            int id = (int)ev.EntityId;
            _stCache.Remove(id);
            _fcCache.Remove(id);
            _hfCache.Remove(id);
            _initialized.Remove(id);
        }

        public static (int X, int Y)? GetPosition(int entityId)
        {
            foreach (var e in _cache)
                if (e.Id == entityId)
                    return e.Pos;
            return null;
        }

        public static void Log(AIBehaviour beh, string msg)
        {
            if (AIBehaviour.Debug)
                Console.WriteLine($"[AIBehaviour:{beh.Entity.Uuid:X16}] {beh.CurrentState}: {msg}");
        }

        public static bool TryLoadComponent<T>(string comp, Entity ent, out T result) where T : class, new()
        {
            result = default!;
            var buf = new byte[4096];
            int len = EngineInterop.GetComponentJson(
                Engine.Instance.Context,
                new EntityId { id = (uint)ent.Id },
                comp, buf, buf.Length);

            if (len <= 0) return false;

            try
            {
                string json = System.Text.Encoding.UTF8.GetString(buf, 0, len);
                result = JsonHelper.Deserialize<T>(json, includeFields: true)!;
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        public static void EnsureFacing(Entity ent)
        {
            ComponentWriter.Patch(
                (uint)ent.Id,
                nameof(FacingComponent),
                new WanderSpire.Components.FacingComponent { Facing = 0 }
            );
        }

        public static void TrySetField(Entity ent, string comp, string field, int value)
        {
            try { ent.SetField(comp, field, value); } catch { }
        }

        public static int Dist2((int X, int Y) a, (int X, int Y) b)
            => (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);

        private static readonly Random _rng = new();
        public static (int X, int Y) RandomNearby((int X, int Y) origin, int radius)
        {
            (int X, int Y) dest;
            do
            {
                dest = (
                    origin.X + _rng.Next(-radius, radius + 1),
                    origin.Y + _rng.Next(-radius, radius + 1));
            } while (Dist2(dest, origin) > radius * radius || dest == origin);

            return dest;
        }

        public static (int X, int Y) PreferAxis((int X, int Y) from, (int X, int Y) to)
        {
            int dx = to.X - from.X, dy = to.Y - from.Y;
            if (Math.Abs(dx) == 1 && Math.Abs(dy) == 1)
                return (_rng.Next(2) == 0) ? (to.X, from.Y) : (from.X, to.Y);
            return to;
        }

        public static (bool found, (int X, int Y) targetPos, int targetId, bool sees, bool inAttack)
            FindNearestTarget(
                AIBehaviour beh,
                FactionComponent fc,
                StatsComponent st,
                (int X, int Y) pos,
                int awarenessRange)
        {
            int minD2 = int.MaxValue, victim = -1;
            (int X, int Y) seen = pos;

            foreach (var info in _cache)
            {
                if (info.Id == beh.Entity.Id) continue;

                bool hostile = false;
                // 1) player hostility
                if (info.IsPlayer && fc.HostileToPlayer)
                {
                    hostile = true;
                }
                else
                {
                    // 2) alignment-based hostility
                    if ((info.Fc.Alignment == "good" && fc.HostileToGood) ||
                        (info.Fc.Alignment == "neutral" && fc.HostileToNeutral) ||
                        (info.Fc.Alignment == "bad" && fc.HostileToBad))
                    {
                        hostile = true;
                    }
                    // 3) custom factions (fix: use the behaviour's own list, not the target's)
                    else if (!string.IsNullOrEmpty(fc.HostileToFactions)
                             && fc.HostileToFactions
                                   .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(s => s.Trim())
                                   .Contains(info.Fc.Faction))
                    {
                        hostile = true;
                    }
                }

                if (!hostile)
                    continue;

                int dx = info.Pos.X - pos.X, dy = info.Pos.Y - pos.Y;
                int d2 = dx * dx + dy * dy;
                if (d2 < minD2)
                {
                    minD2 = d2;
                    seen = info.Pos;
                    victim = info.Id;
                }
            }

            bool found = victim >= 0;
            bool sees = found && minD2 <= awarenessRange * awarenessRange;
            bool inAtk = found && minD2 <= st.AttackRange * st.AttackRange && minD2 >= 1;
            return (found, seen, victim, sees, inAtk);
        }

        // ───────────────────────────────────────────────────────────────────────
        //  SEPARATION – now a quick lookup in our _cache list
        // ───────────────────────────────────────────────────────────────────────
        public static (int X, int Y) FindSeparationMove((int X, int Y) from)
        {
            var options = new[]
            {
                (X: from.X + 1, Y: from.Y),
                (X: from.X - 1, Y: from.Y),
                (X: from.X,     Y: from.Y + 1),
                (X: from.X,     Y: from.Y - 1)
            };

            // filter out any tiles currently occupied by other entities
            var free = options.Where(o => !_cache.Any(e => e.Pos == o)).ToList();

            if (free.Count > 0)
            {
                // pick a random free neighbor
                return free[_rng.Next(free.Count)];
            }

            // if no free neighbors, pick any direction at random (matches native)
            return options[_rng.Next(options.Length)];
        }

        public static void PublishAttackEvent(Entity attacker, int victimId, bool rightHand)
        {
            if (!attacker.IsValid || victimId < 0) return;
            GameEventBus.Event<AttackEvent>.Publish(new AttackEvent((uint)attacker.Id, (uint)victimId, rightHand));
        }

        public static void PublishHurtEvent(uint entityId, int damage)
        {
            GameEventBus.Event<HurtEvent>.Publish(new HurtEvent(entityId, damage));
        }

        public static void PublishDeathEvent(uint entityId)
        {
            GameEventBus.Event<DeathEvent>.Publish(new DeathEvent(entityId));
        }
    }
}
