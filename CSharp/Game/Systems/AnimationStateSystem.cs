using Game.Events;
using ScriptHost;
using WanderSpire.Components;
using WanderSpire.Core.Events;
using WanderSpire.Scripting;
using WanderSpire.Scripting.Utils;

namespace Game.Systems
{
    /// <summary>
    /// Central, event‐driven animation‐state resolver.
    /// Now purely generic: any non‐looping clip will reset to "Idle" when finished.
    /// </summary>
    public sealed class AnimationStateSystem : ITickReceiver
    {
        public AnimationStateSystem()
        {
            // ── Movement animations driven by interpolation ─────────────────
            GameEventBus.Event<MoveStartedEvent>.Subscribe(ev =>
            {
                SetState(ev.entity, "Walk");
            });

            // Only go back to Idle when the **visual** interpolation finishes
            GameEventBus.Event<InterpolationCompleteEvent>.Subscribe(ev =>
            {
                SetState(ev.EntityId, "Idle");
            });

            // ── Hurt and Death events (unchanged) ──────────────────────────
            GameEventBus.Event<HurtEvent>.Subscribe(ev =>
            {
                if (ev.Damage > 0)
                    SetState(ev.EntityId, "Hurt");
            });
            GameEventBus.Event<DeathEvent>.Subscribe(ev =>
                SetState(ev.EntityId, "Death"));

            // ── Keep resetting non‐looping clips → Idle after they finish ──
            EventBus.AnimationFinished += OnAnimationFinished;
        }


        public void OnTick(float dt)
        {
            // purely event‐driven; no per‐frame logic here
        }

        private void OnAnimationFinished(WanderSpire.Core.Events.AnimationFinishedEvent ev)
        {
            var ent = Entity.FromRaw(Engine.Instance!.Context, (int)ev.entity);
            if (!ent.IsValid) return;

            string current;
            try
            {
                current = ent.GetField<string>(nameof(AnimationStateComponent), "state");
            }
            catch
            {
                return;
            }

            if (current == "Death")
                return;

            // Retrieve the clips to check loop flag
            var clipsComp = ent.GetComponent<AnimationClipsComponent>(nameof(AnimationClipsComponent));
            if (clipsComp != null && clipsComp.Clips.TryGetValue(current, out var clip))
            {
                if (!clip.Loop)
                {
                    SetState(ev.entity, "Idle");
                }
            }
            else
            {
                if (current == "Hurt" || current.StartsWith("Attack"))
                {
                    SetState(ev.entity, "Idle");
                }
            }
        }

        private static void SetState(uint eid, string newState)
        {
            var ent = Entity.FromRaw(Engine.Instance!.Context, (int)eid);
            if (!ent.IsValid) return;

            string cur;
            try
            {
                cur = ent.GetField<string>(nameof(AnimationStateComponent), "state");
            }
            catch
            {
                cur = string.Empty;
            }

            if (cur == newState) return;

            ComponentWriter.Patch(
                eid,
                nameof(AnimationStateComponent),
                new AnimationStateComponent { state = newState }
            );
        }
    }
}
