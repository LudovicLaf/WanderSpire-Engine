using Game.Dto;
using Game.Events;
using System;
using WanderSpire.Components;
using WanderSpire.ScriptHost;
using WanderSpire.Scripting;
using static Game.Behaviours.AIBehaviourUtils;

namespace Game.Behaviours
{
    public class AIBehaviour : Behaviour
    {
        public static bool Debug = false;

        internal enum State { Idle, Wander, Chase, Attack, Return, Dead }

        internal State CurrentState => _state;

        private float _wanderRadius, _wanderChance;
        private int _awarenessRange, _chaseRange;
        private FactionComponent _fc = new();
        private StatsComponent _st = new();
        private (int X, int Y) _pos, _origin;
        private State _state = State.Idle;
        private bool _dead = false;
        private readonly Random _rng = new();
        private float _moveTimer, _attackTimer, _moveInterval, _attackInterval;

        /* ─────────────────────────────────────────────────────────────── */
        protected override void Start()
        {
            if (!TryLoadComponent(nameof(GridPositionComponent), Entity, out GridPositionComponent gp))
            {
                Log(this, "Missing GridPositionComponent! AI disabled.");
                _dead = true; return;
            }
            _pos = _origin = gp.AsTuple();

            _fc = Entity.GetScriptData<FactionComponent>(nameof(FactionComponent)) ?? new();
            _st = Entity.GetScriptData<StatsComponent>(nameof(StatsComponent)) ?? new();
            if (_st == null)
            {
                Log(this, "Missing Stats! AI disabled.");
                _dead = true; return;
            }

            var aiData = Entity.GetScriptData<AIParams>(nameof(AIParams));
            if (aiData != null)
            {
                _wanderRadius = aiData.wanderRadius;
                _wanderChance = aiData.wanderChance;
                _awarenessRange = aiData.awarenessRange;
                _chaseRange = aiData.chaseRange;
                if (aiData.position?.Length == 2) _pos = (aiData.position[0], aiData.position[1]);
                if (aiData.origin?.Length == 2) _origin = (aiData.origin[0], aiData.origin[1]);
                _state = (State)aiData.state;
            }

            _moveTimer = 0f;
            _attackTimer = 0f;
            _attackInterval = Math.Max(1, _st.AttackSpeed) * Engine.Instance.TickInterval;
            ScheduleNextMoveInterval();

            EnsureFacing(Entity);

            if (_st.CurrentHitpoints <= 0)
            {
                _dead = true;
                _state = State.Dead;
            }

            Log(this, $"Initialized at {_pos}, origin={_origin}, state={_state}");

            // ─── Subscribe to global HurtEvent, DeathEvent ─────────────────
            GameEventBus.Event<HurtEvent>.Subscribe(OnGlobalHurt);
            GameEventBus.Event<DeathEvent>.Subscribe(OnGlobalDeath);
        }

        /* ─────────────────────────────────────────────────────────────── */
        public override void Update(float dt)
        {
            if (_dead) return;

            // refresh grid position
            var maybe = GetPosition(Entity.Id);
            if (maybe.HasValue)
                _pos = maybe.Value;

            // timers
            _moveTimer += dt;
            _attackTimer += dt;
            if (_moveTimer < _moveInterval) return;
            _moveTimer = 0f;
            ScheduleNextMoveInterval();

            // find nearest hostile
            var (found, targetPos, targetId, sees, inAttack) =
                FindNearestTarget(this, _fc, _st, _pos, _awarenessRange);

            // leash check ----------------------------------------------------
            int d2o = Dist2(_pos, _origin), leash2 = _chaseRange * _chaseRange;
            if (_state != State.Return && d2o > leash2)
            {
                QueueMove(_origin);
                _state = State.Return;
                Log(this, "Leash broken: Returning!");
                return;
            }

            // separation if overlapping -------------------------------------
            if (found && targetPos == _pos)
            {
                if (_rng.NextDouble() >= 2f / 3f)
                {
                    var sep = FindSeparationMove(_pos);
                    if (sep != _pos)
                    {
                        QueueMove(sep);
                        Log(this, $"Separation move to {sep}");
                        return;
                    }
                }
                return;
            }

            // helper for chase destination
            (int X, int Y) GetChaseDest()
            {
                (int X, int Y) best = _pos; int bestD2 = int.MaxValue;
                foreach (var n in new[] {
                        (X: targetPos.X + 1, Y: targetPos.Y),
                        (X: targetPos.X - 1, Y: targetPos.Y),
                        (X: targetPos.X,     Y: targetPos.Y + 1),
                        (X: targetPos.X,     Y: targetPos.Y - 1) })
                {
                    int d2 = Dist2(_pos, n);
                    if (d2 < bestD2) { bestD2 = d2; best = n; }
                }
                return best;
            }

            /* ── finite-state machine ───────────────────────────────── */
            switch (_state)
            {
                case State.Idle:
                    if (found && sees)
                    {
                        if (inAttack)
                        {
                            _state = State.Attack;
                            Log(this, $"Idle→Attack {targetId}");
                        }
                        else
                        {
                            var chase = GetChaseDest();
                            _state = State.Chase;
                            QueueMove(chase);
                            Log(this, $"Idle→Chase {targetId} via {chase}");
                        }
                    }
                    else if (_rng.NextDouble() < _wanderChance)
                    {
                        _state = State.Wander;
                    }
                    break;

                case State.Wander:
                    {
                        var dest = RandomNearby(_origin, (int)_wanderRadius);
                        if (dest != _pos)
                        {
                            QueueMove(dest);
                            Log(this, $"Wander→{dest}");
                        }
                        _state = State.Idle;
                    }
                    break;

                case State.Chase:
                    if (!found || !sees)
                    {
                        QueueMove(_origin);
                        _state = State.Return;
                        Log(this, "Chase lost→Return");
                    }
                    else if (inAttack)
                    {
                        _state = State.Attack;
                        Log(this, $"Chase→Attack {targetId}");
                    }
                    else
                    {
                        var chase = GetChaseDest();
                        QueueMove(chase);
                        Log(this, $"Chasing→{chase}");
                    }
                    break;

                case State.Attack:
                    if (!found || !sees)
                    {
                        _state = State.Chase;
                        Log(this, "Attack lost→Chase");
                    }
                    else if (!inAttack)
                    {
                        _state = State.Chase;
                        QueueMove(GetChaseDest());
                        Log(this, "Attack→Chase");
                    }
                    else if (_attackTimer >= _attackInterval)
                    {
                        _attackTimer = 0f;
                        SetFacing(_pos, targetPos);
                        PublishAttackEvent(Entity, targetId, targetPos.X > _pos.X);
                        Log(this, "Attack swing");
                    }
                    break;

                case State.Return:
                    if (_pos == _origin)
                    {
                        _state = State.Idle;
                        Log(this, "Returned→Idle");
                    }
                    else
                    {
                        var step = PreferAxis(_pos, _origin);
                        if (step != _pos)
                        {
                            QueueMove(step);
                            Log(this, $"Return→{step}");
                        }
                    }
                    break;

                case State.Dead:
                    break;
            }
        }

        /* ── global combat events ─────────────────────────────────── */
        private void OnGlobalHurt(HurtEvent ev)
        {
            if (ev.EntityId != (uint)Entity.Id || ev.Damage <= 0) return;
            _st.CurrentHitpoints = Math.Max(0, _st.CurrentHitpoints - ev.Damage);
            if (_st.CurrentHitpoints <= 0 && !_dead)
            {
                _dead = true;
                _state = State.Dead;
            }
        }

        private void OnGlobalDeath(DeathEvent ev)
        {
            if (ev.EntityId != (uint)Entity.Id) return;
            if (!_dead)
            {
                _dead = true;
                _state = State.Dead;
            }
        }

        /* ── helpers (unchanged except animation calls removed) ──── */
        private void ScheduleNextMoveInterval()
        {
            var baseDt = Engine.Instance.TickInterval;
            _moveInterval = baseDt * (1f + (float)_rng.NextDouble());
        }

        private void QueueMove((int X, int Y) dest)
        {
            if (dest == _pos) return;
            SetFacing(_pos, dest);

            // ── Use generic‐nested Event<T> instead of GameEventBus.Publish ──
            GameEventBus.Event<MovementIntentEvent>.Publish(new MovementIntentEvent(
                (uint)Entity.Id, dest.X, dest.Y, /*run*/ false));
        }

        private void SetFacing((int X, int Y) from, (int X, int Y) to)
        {
            int f = to.X > from.X ? 0
                  : to.X < from.X ? 1
                  : to.Y > from.Y ? 2 : 3;
            TrySetField(Entity, "FacingComponent", "facing", f);
        }
    }
}
