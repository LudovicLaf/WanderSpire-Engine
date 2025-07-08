// ScriptHost/Interop/Events.cs
using System;
using System.Runtime.InteropServices;

namespace WanderSpire.Core.Events
{
    // Logic Tick
    [StructLayout(LayoutKind.Sequential)]
    public struct LogicTickEvent
    {
        public ulong index;
    }

    // (TileClickEvent has been removed)

    // Camera
    [StructLayout(LayoutKind.Sequential)]
    public struct CameraMovedEvent
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] minBound; // (x, y)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] maxBound; // (x, y)
    }

    // Pathfinding (PathAppliedEvent)
    [StructLayout(LayoutKind.Sequential)]
    public struct PathAppliedEvent
    {
        public uint entity;
        // Pass checkpoint count and pointer to native array, or use a callback to fetch the checkpoints.
        public IntPtr checkpoints; // pointer to native array of int[2]
        public int checkpointCount;

        // Helper to read as managed array if needed
        public (int X, int Y)[] GetCheckpoints()
        {
            var result = new (int X, int Y)[checkpointCount];
            int size = sizeof(int) * 2;
            for (int i = 0; i < checkpointCount; i++)
            {
                int x = Marshal.ReadInt32(checkpoints, i * size + 0);
                int y = Marshal.ReadInt32(checkpoints, i * size + sizeof(int));
                result[i] = (x, y);
            }
            return result;
        }
    }

    // MoveStarted
    [StructLayout(LayoutKind.Sequential)]
    public struct MoveStartedEvent
    {
        public uint entity;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] fromTile;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] toTile;
        public (int X, int Y) From => (fromTile[0], fromTile[1]);
        public (int X, int Y) To => (toTile[0], toTile[1]);
    }

    // MoveCompleted
    [StructLayout(LayoutKind.Sequential)]
    public struct MoveCompletedEvent
    {
        public uint entity;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] tile;
        public (int X, int Y) Tile => (tile[0], tile[1]);
    }

    // AnimationFinished
    [StructLayout(LayoutKind.Sequential)]
    public struct AnimationFinishedEvent
    {
        public uint entity;
    }

    // FrameRender
    [StructLayout(LayoutKind.Sequential)]
    public struct FrameRenderEvent
    {
        public IntPtr state; // pointer to AppState, only useful in native callbacks
    }

    // StateEntered (FSM notifications)
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct StateEnteredEvent
    {
        public uint entity;
        [MarshalAs(UnmanagedType.LPStr)]
        public string state;
    }
}
