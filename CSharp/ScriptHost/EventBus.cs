using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WanderSpire.Core.Events;

namespace WanderSpire.Scripting
{
    /// <summary>
    /// Bridging layer between the native EventBus and managed code.
    /// Now strictly limited to generic engine events (logic tick, input, render, etc).
    /// All gameplay–specific messages (e.g. HurtEvent, DeathEvent) are purely managed.
    /// </summary>
    public static class EventBus
    {
        // — Generic engine‐driven events (still hard‐wired) —
        public static event Action<LogicTickEvent>? LogicTick;
        public static event Action<MoveStartedEvent>? MoveStarted;
        public static event Action<MoveCompletedEvent>? MoveCompleted;
        public static event Action<PathAppliedEvent>? PathApplied;
        public static event Action<AnimationFinishedEvent>? AnimationFinished;
        public static event Action<StateEnteredEvent>? StateEntered;
        // [TileClickEvent has been removed]
        public static event Action<FrameRenderEvent>? FrameRender;

        // — Script‐side subscriptions for anything else (keyed by event name) —
        private static readonly Dictionary<string, List<Action<IntPtr, int>>> _scriptListeners
            = new Dictionary<string, List<Action<IntPtr, int>>>(StringComparer.Ordinal);

        // Native callback machinery - FIXED: Use the delegate directly, not function pointer
        private static readonly EngineInterop.ScriptEventCallback _nativeCallback = OnNativeEvent;
        private static bool _initialized;

        /// <summary>
        /// Call once, immediately after EngineInit.
        /// Hooks every native event into our managed dispatcher.
        /// </summary>
        public static void Initialize(IntPtr ctx)
        {
            if (_initialized) return;
            // FIXED: Pass the delegate directly, not a function pointer
            EngineInterop.Script_SubscribeEvent(ctx, "*", _nativeCallback, IntPtr.Zero);
            _initialized = true;
        }

        private static void OnNativeEvent(
            IntPtr eventNamePtr, IntPtr payloadPtr, int payloadSize, IntPtr _)
        {
            string name = Marshal.PtrToStringAnsi(eventNamePtr) ?? string.Empty;

            // First, handle all built‐in engine events explicitly:
            switch (name)
            {
                case "LogicTickEvent" when payloadSize == Marshal.SizeOf<LogicTickEvent>():
                    LogicTick?.Invoke(Marshal.PtrToStructure<LogicTickEvent>(payloadPtr));
                    return;
                case "MoveStartedEvent" when payloadSize == Marshal.SizeOf<MoveStartedEvent>():
                    MoveStarted?.Invoke(Marshal.PtrToStructure<MoveStartedEvent>(payloadPtr));
                    return;
                case "MoveCompletedEvent" when payloadSize == Marshal.SizeOf<MoveCompletedEvent>():
                    MoveCompleted?.Invoke(Marshal.PtrToStructure<MoveCompletedEvent>(payloadPtr));
                    return;
                case "PathAppliedEvent" when payloadSize == Marshal.SizeOf<PathAppliedEvent>():
                    PathApplied?.Invoke(Marshal.PtrToStructure<PathAppliedEvent>(payloadPtr));
                    return;
                case "AnimationFinishedEvent" when payloadSize == Marshal.SizeOf<AnimationFinishedEvent>():
                    AnimationFinished?.Invoke(Marshal.PtrToStructure<AnimationFinishedEvent>(payloadPtr));
                    return;
                case "StateEnteredEvent" when payloadSize == Marshal.SizeOf<StateEnteredEvent>():
                    StateEntered?.Invoke(Marshal.PtrToStructure<StateEnteredEvent>(payloadPtr));
                    return;
                // [TileClickEvent case removed]
                case "FrameRenderEvent" when payloadSize == Marshal.SizeOf<FrameRenderEvent>():
                    FrameRender?.Invoke(Marshal.PtrToStructure<FrameRenderEvent>(payloadPtr));
                    return;
                default:
                    break;
            }

            // Otherwise, dispatch to any script‐side listeners by name:
            if (_scriptListeners.TryGetValue(name, out var list))
            {
                foreach (var cb in list)
                    cb(payloadPtr, payloadSize);
            }
        }

        /// <summary>
        /// Subscribe to *any* event name. The callback receives raw payload ptr+size.
        /// </summary>
        public static void Subscribe(string eventName, Action<IntPtr, int> callback)
        {
            if (!_scriptListeners.TryGetValue(eventName, out var list))
            {
                list = new List<Action<IntPtr, int>>();
                _scriptListeners[eventName] = list;
            }
            list.Add(callback);
        }

        /// <summary>
        /// Unsubscribe a previously registered callback.
        /// </summary>
        public static void Unsubscribe(string eventName, Action<IntPtr, int> callback)
        {
            if (_scriptListeners.TryGetValue(eventName, out var list))
            {
                list.Remove(callback);
                if (list.Count == 0)
                    _scriptListeners.Remove(eventName);
            }
        }
    }
}