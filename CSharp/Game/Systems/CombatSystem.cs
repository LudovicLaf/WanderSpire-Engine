using Game.Dto;
using Game.Events;
using ScriptHost;
using System;
using WanderSpire.Components;
using WanderSpire.Core.Events;
using WanderSpire.Scripting;
using WanderSpire.Scripting.Utils;
using static WanderSpire.Scripting.EngineInterop;

namespace Game.Systems
{
    /// <summary>
    /// Pure‐managed combat resolver (native StatsComponent has been retired).
    /// Also drives Death animations & respawn.
    /// </summary>
    public sealed class CombatSystem : ITickReceiver
    {
        private static bool _wired;
        private static readonly Random _rng = new();

        public CombatSystem()
        {
            if (_wired) return;
            _wired = true;

            // ── gameplay events ───────────────────────────────────────────
            GameEventBus.Event<AttackEvent>.Subscribe(OnAttack);
            GameEventBus.Event<HurtEvent>.Subscribe(OnHurt);
            GameEventBus.Event<DeathEvent>.Subscribe(OnDeath);

            // still need AnimationFinished for death/respawn
            EventBus.AnimationFinished += OnAnimFinished;
        }

        public void OnTick(float dt) { /* stateless */ }

        private void OnAttack(AttackEvent ev)
        {
            var eng = Engine.Instance;
            if (eng == null) return;

            var attacker = Entity.FromRaw(eng.Context, (int)ev.AttackerId);
            var victim = Entity.FromRaw(eng.Context, (int)ev.VictimId);
            if (!attacker.IsValid || !victim.IsValid) return;

            // Set directional attack animation before applying damage
            string attackState = "Attack";

            try
            {
                var atkPosComp = attacker.GetComponent<GridPositionComponent>(nameof(GridPositionComponent));
                var vicPosComp = victim.GetComponent<GridPositionComponent>(nameof(GridPositionComponent));
                if (atkPosComp != null && vicPosComp != null)
                {
                    var atkPos = atkPosComp.AsTuple();
                    var vicPos = vicPosComp.AsTuple();
                    attackState = (vicPos.X == atkPos.X) ? "AttackVertical" : "AttackHorizontal";
                }
            }
            catch
            {
                // fallback to generic "Attack"
            }

            ComponentWriter.Patch(
                ev.AttackerId,
                nameof(AnimationStateComponent),
                new AnimationStateComponent { state = attackState }
            );

            var atkStats = attacker.GetScriptData<StatsComponent>(nameof(StatsComponent));
            var vicStats = victim.GetScriptData<StatsComponent>(nameof(StatsComponent));
            if (atkStats == null || vicStats == null) return;

            int attAcc = atkStats.Accuracy;
            int attStr = atkStats.Strength;
            int defRoll = atkStats.AttackType switch
            {
                0 => vicStats.DefenseStab,
                1 => vicStats.DefenseSlash,
                2 => vicStats.DefenseCrush,
                3 => vicStats.DefenseRanged,
                4 => vicStats.DefenseMagic,
                _ => vicStats.DefenseStab
            };
            float chance = attAcc > defRoll
                ? 1f - (defRoll + 2f) / (2f * attAcc + 1f)
                : (float)attAcc / (2f * defRoll + 1f);

            bool hit = _rng.NextDouble() < chance;
            int maxHit = (int)MathF.Floor(0.5f + attStr);
            int dmg = hit ? _rng.Next(0, maxHit + 1) : 0;

            int newHp = Math.Max(0, vicStats.CurrentHitpoints - dmg);
            vicStats.CurrentHitpoints = newHp;
            victim.SetScriptData(nameof(StatsComponent), vicStats);

            // notify hurt (AnimationStateSystem will set "Hurt")
            GameEventBus.Event<HurtEvent>.Publish(new HurtEvent(ev.VictimId, dmg));

            // lethal?
            if (newHp == 0)
            {
                GameEventBus.Event<DeathEvent>.Publish(new DeathEvent(ev.VictimId));
            }
        }

        private void OnHurt(HurtEvent ev)
        {
            if (ev.Damage <= 0) return;
            var eng = Engine.Instance;
            if (eng == null) return;

            var ent = Entity.FromRaw(eng.Context, (int)ev.EntityId);
            if (!ent.IsValid) return;

            var st = ent.GetScriptData<StatsComponent>(nameof(StatsComponent))
                  ?? ent.GetComponent<StatsComponent>(nameof(StatsComponent));
            if (st == null || st.CurrentHitpoints <= 0) return;
            // no need to do anything else—AnimationStateSystem handles the state
        }

        private void OnDeath(DeathEvent ev)
        {
            var eng = Engine.Instance;
            if (eng == null) return;

            var ent = Entity.FromRaw(eng.Context, (int)ev.EntityId);
            if (!ent.IsValid) return;

            if (ent.GetComponent<object>(nameof(PlayerTagComponent)) != null)
                return;

            // NPC: AnimationStateSystem will handle the Death clip
        }


        private void OnAnimFinished(WanderSpire.Core.Events.AnimationFinishedEvent ev)
        {
            var eng = Engine.Instance;
            if (eng == null) return;

            var ent = Entity.FromRaw(eng.Context, (int)ev.entity);
            if (!ent.IsValid) return;

            bool isPlayer = ent.GetComponent<object>(nameof(PlayerTagComponent)) != null;

            string state = string.Empty;
            try
            {
                state = ent.GetField<string>(nameof(AnimationStateComponent), "state");
            }
            catch
            {
                // missing component? bail
            }

            if (state == "Death")
            {
                if (isPlayer)
                {
                    var st = ent.GetScriptData<StatsComponent>(nameof(StatsComponent));
                    if (st != null)
                    {
                        st.CurrentHitpoints = st.MaxHitpoints;
                        ent.SetScriptData(nameof(StatsComponent), st);
                    }

                    // reset position to origin
                    int ox = 0, oy = 0;
                    var originArr = ent.GetScriptData<int[]>("origin") ?? Array.Empty<int>();
                    if (originArr.Length == 2)
                        (ox, oy) = (originArr[0], originArr[1]);

                    ComponentWriter.Patch(
                        (uint)ent.Id,
                        nameof(GridPositionComponent),
                        new GridPositionComponent
                        {
                            Tile = new[] { ox, oy },
                            TileObj = new GridPositionComponent.Vec2 { X = ox, Y = oy }
                        }
                    );

                    // notify MoveCompleted so camera/idle snap back
                    GameEventBus.Event<MoveCompletedEvent>.Publish(new MoveCompletedEvent
                    {
                        entity = (uint)ent.Id,
                        tile = new int[2] { ox, oy }
                    });

                    Engine_SetPlayerEntity(eng.Context, new EntityId { id = (uint)ent.Id });
                    Engine_SetCameraTarget(eng.Context, new EntityId { id = (uint)ent.Id });

                    Console.WriteLine($"[CombatSystem] Player respawned @{ox},{oy}");
                }
                else
                {
                    eng.DestroyEntity(ent);
                }
            }
        }
    }
}
