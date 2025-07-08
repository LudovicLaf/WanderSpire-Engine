// O:\Projets\Game engine\WanderSpire\CSharp\ScriptHost\Input.cs

using System;
using System.Runtime.InteropServices;

namespace WanderSpire.Scripting
{
    /// <summary>
    /// Unity‐style static input system (keyboard + mouse).
    /// Call <see cref="Initialize"/> once at startup, and <see cref="BeginFrame"/>
    /// once per frame before you query any Input methods.
    /// </summary>
    public static class Input
    {
        // ── P/Invoke into SDL3 ─────────────────────────────────────────────
        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetKeyboardState(out int numkeys);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SDL_GetMouseState(out int x, out int y);

        // ── internal buffers ───────────────────────────────────────────────
        private static IntPtr _kbStatePtr;
        private static int _keyCount;
        private static byte[] _curKeys;
        private static byte[] _prevKeys;

        private static int _mouseX, _mouseY;
        private static uint _curMouseButtons, _prevMouseButtons;

        private static bool _initialized = false;

        /// <summary>Must be called once, before your first frame.</summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _kbStatePtr = SDL_GetKeyboardState(out _keyCount);
            _curKeys = new byte[_keyCount];
            _prevKeys = new byte[_keyCount];
            UpdateKeyboard();
            _curMouseButtons = SDL_GetMouseState(out _mouseX, out _mouseY);
            _prevMouseButtons = _curMouseButtons;
            _initialized = true;
        }

        /// <summary>Call once per frame (at the top of your loop).</summary>
        public static unsafe void BeginFrame()
        {
            if (!_initialized) Initialize();

            Buffer.BlockCopy(_curKeys, 0, _prevKeys, 0, _keyCount);
            _prevMouseButtons = _curMouseButtons;

            UpdateKeyboard();
            _curMouseButtons = SDL_GetMouseState(out _mouseX, out _mouseY);
        }

        private static unsafe void UpdateKeyboard()
        {
            if (_kbStatePtr == IntPtr.Zero) return;
            byte* ptr = (byte*)_kbStatePtr.ToPointer();
            for (int i = 0; i < _keyCount; i++)
                _curKeys[i] = ptr[i];
        }

        // ── Keyboard ────────────────────────────────────────────────────────

        public static bool GetKey(KeyCode key)
        {
            int idx = (int)key;
            return idx >= 0
                && idx < _keyCount
                && _curKeys[idx] != 0;
        }

        public static bool GetKeyDown(KeyCode key)
        {
            int idx = (int)key;
            return idx >= 0
                && idx < _keyCount
                && _curKeys[idx] != 0
                && _prevKeys[idx] == 0;
        }

        public static bool GetKeyUp(KeyCode key)
        {
            int idx = (int)key;
            return idx >= 0
                && idx < _keyCount
                && _curKeys[idx] == 0
                && _prevKeys[idx] != 0;
        }

        // ── Mouse ────────────────────────────────────────────────────────────

        public static bool GetMouseButton(MouseButton btn) =>
            (_curMouseButtons & (uint)btn) != 0;

        public static bool GetMouseButtonDown(MouseButton btn) =>
            ((_curMouseButtons & (uint)btn) != 0)
         && ((_prevMouseButtons & (uint)btn) == 0);

        public static bool GetMouseButtonUp(MouseButton btn) =>
            ((_curMouseButtons & (uint)btn) == 0)
         && ((_prevMouseButtons & (uint)btn) != 0);

        public static int MouseX => _mouseX;
        public static int MouseY => _mouseY;

        // ── Simple Axes ─────────────────────────────────────────────────────

        public static float GetAxis(string axisName)
        {
            switch (axisName)
            {
                case "Horizontal":
                    float r = GetKey(KeyCode.D) || GetKey(KeyCode.Right) ? 1f : 0f;
                    float l = GetKey(KeyCode.A) || GetKey(KeyCode.Left) ? -1f : 0f;
                    return r + l;
                case "Vertical":
                    float u = GetKey(KeyCode.W) || GetKey(KeyCode.Up) ? 1f : 0f;
                    float d = GetKey(KeyCode.S) || GetKey(KeyCode.Down) ? -1f : 0f;
                    return u + d;
                default:
                    return 0f;
            }
        }
    }

    /// <summary>SDL scancode-based key codes.</summary>
    public enum KeyCode : int
    {
        Unknown = 0,

        // Usage page 0x07 (USB keyboard page)
        A = 4,
        B = 5,
        C = 6,
        D = 7,
        E = 8,
        F = 9,
        G = 10,
        H = 11,
        I = 12,
        J = 13,
        K = 14,
        L = 15,
        M = 16,
        N = 17,
        O = 18,
        P = 19,
        Q = 20,
        R = 21,
        S = 22,
        T = 23,
        U = 24,
        V = 25,
        W = 26,
        X = 27,
        Y = 28,
        Z = 29,

        Num1 = 30,
        Num2 = 31,
        Num3 = 32,
        Num4 = 33,
        Num5 = 34,
        Num6 = 35,
        Num7 = 36,
        Num8 = 37,
        Num9 = 38,
        Num0 = 39,

        Return = 40,
        Escape = 41,
        Backspace = 42,
        Tab = 43,
        Space = 44,

        Minus = 45,
        Equals = 46,
        LeftBracket = 47,
        RightBracket = 48,
        Backslash = 49,
        NonUSHash = 50,
        Semicolon = 51,
        Apostrophe = 52,
        Grave = 53,
        Comma = 54,
        Period = 55,
        Slash = 56,

        CapsLock = 57,

        F1 = 58,
        F2 = 59,
        F3 = 60,
        F4 = 61,
        F5 = 62,
        F6 = 63,
        F7 = 64,
        F8 = 65,
        F9 = 66,
        F10 = 67,
        F11 = 68,
        F12 = 69,

        PrintScreen = 70,
        ScrollLock = 71,
        Pause = 72,
        Insert = 73,
        Home = 74,
        PageUp = 75,
        Delete = 76,
        End = 77,
        PageDown = 78,
        Right = 79,
        Left = 80,
        Down = 81,
        Up = 82,

        NumLockClear = 83,
        KpDivide = 84,
        KpMultiply = 85,
        KpMinus = 86,
        KpPlus = 87,
        KpEnter = 88,
        Kp1 = 89,
        Kp2 = 90,
        Kp3 = 91,
        Kp4 = 92,
        Kp5 = 93,
        Kp6 = 94,
        Kp7 = 95,
        Kp8 = 96,
        Kp9 = 97,
        Kp0 = 98,
        KpPeriod = 99,

        NonUSBackslash = 100,
        Application = 101,
        Power = 102,
        KpEquals = 103,
        F13 = 104,
        F14 = 105,
        F15 = 106,
        F16 = 107,
        F17 = 108,
        F18 = 109,
        F19 = 110,
        F20 = 111,
        F21 = 112,
        F22 = 113,
        F23 = 114,
        F24 = 115,
        Execute = 116,
        Help = 117,
        Menu = 118,
        Select = 119,
        Stop = 120,
        Again = 121,
        Undo = 122,
        Cut = 123,
        Copy = 124,
        Paste = 125,
        Find = 126,
        Mute = 127,
        VolumeUp = 128,
        VolumeDown = 129,

        // LockingCapsLock = 130,    // Not generally used
        // LockingNumLock = 131,
        // LockingScrollLock = 132,

        KpComma = 133,
        KpEqualsAS400 = 134,

        International1 = 135,
        International2 = 136,
        International3 = 137,
        International4 = 138,
        International5 = 139,
        International6 = 140,
        International7 = 141,
        International8 = 142,
        International9 = 143,

        Lang1 = 144,
        Lang2 = 145,
        Lang3 = 146,
        Lang4 = 147,
        Lang5 = 148,
        Lang6 = 149,
        Lang7 = 150,
        Lang8 = 151,
        Lang9 = 152,

        Alterase = 153,
        SysReq = 154,
        Cancel = 155,
        Clear = 156,
        Prior = 157,
        Return2 = 158,
        Separator = 159,
        Out = 160,
        Oper = 161,
        ClearAgain = 162,
        CrSel = 163,
        ExSel = 164,

        // 165-175 unused

        Kp00 = 176,
        Kp000 = 177,
        ThousandsSeparator = 178,
        DecimalSeparator = 179,
        CurrencyUnit = 180,
        CurrencySubunit = 181,
        KpLeftParen = 182,
        KpRightParen = 183,
        KpLeftBrace = 184,
        KpRightBrace = 185,
        KpTab = 186,
        KpBackspace = 187,
        KpA = 188,
        KpB = 189,
        KpC = 190,
        KpD = 191,
        KpE = 192,
        KpF = 193,
        KpXor = 194,
        KpPower = 195,
        KpPercent = 196,
        KpLess = 197,
        KpGreater = 198,
        KpAmpersand = 199,
        KpDblAmpersand = 200,
        KpVerticalBar = 201,
        KpDblVerticalBar = 202,
        KpColon = 203,
        KpHash = 204,
        KpSpace = 205,
        KpAt = 206,
        KpExclam = 207,
        KpMemStore = 208,
        KpMemRecall = 209,
        KpMemClear = 210,
        KpMemAdd = 211,
        KpMemSubtract = 212,
        KpMemMultiply = 213,
        KpMemDivide = 214,
        KpPlusMinus = 215,
        KpClear = 216,
        KpClearEntry = 217,
        KpBinary = 218,
        KpOctal = 219,
        KpDecimal = 220,
        KpHexadecimal = 221,

        LCtrl = 224,
        LShift = 225,
        LAlt = 226,
        LGui = 227,
        RCtrl = 228,
        RShift = 229,
        RAlt = 230,
        RGui = 231,

        Mode = 257,

        // Usage page 0x0C (USB consumer page)
        Sleep = 258,
        Wake = 259,
        ChannelIncrement = 260,
        ChannelDecrement = 261,
        MediaPlay = 262,
        MediaPause = 263,
        MediaRecord = 264,
        MediaFastForward = 265,
        MediaRewind = 266,
        MediaNextTrack = 267,
        MediaPreviousTrack = 268,
        MediaStop = 269,
        MediaEject = 270,
        MediaPlayPause = 271,
        MediaSelect = 272,

        AcNew = 273,
        AcOpen = 274,
        AcClose = 275,
        AcExit = 276,
        AcSave = 277,
        AcPrint = 278,
        AcProperties = 279,
        AcSearch = 280,
        AcHome = 281,
        AcBack = 282,
        AcForward = 283,
        AcStop = 284,
        AcRefresh = 285,
        AcBookmarks = 286,

        // Mobile keys
        SoftLeft = 287,
        SoftRight = 288,
        Call = 289,
        EndCall = 290,

        // Reserved 400-500 for dynamic keycodes
        Reserved = 400,

        // 512 is SDL_SCANCODE_COUNT (not a key)
    }


    [Flags]
    public enum MouseButton : uint
    {
        Left = 1 << 0,
        Right = 1 << 1,
        Middle = 1 << 2,
        X1 = 1 << 3,
        X2 = 1 << 4,
    }
}
