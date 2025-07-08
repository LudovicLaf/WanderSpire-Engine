using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace WanderSpire.Scripting.UI
{
    [Flags]
    public enum ImGuiWindowFlags : int
    {
        None = 0,
        NoTitleBar = 1 << 0,
        NoResize = 1 << 1,
        NoMove = 1 << 2,
        NoScrollbar = 1 << 3,
        NoScrollWithMouse = 1 << 4,
        NoCollapse = 1 << 5,
        AlwaysAutoResize = 1 << 6,
        NoBackground = 1 << 7,
        NoSavedSettings = 1 << 8,
        NoMouseInputs = 1 << 9,
        MenuBar = 1 << 10,
        HorizontalScrollbar = 1 << 11,
        NoFocusOnAppearing = 1 << 12,
        NoBringToFrontOnFocus = 1 << 13,
        AlwaysVerticalScrollbar = 1 << 14,
        AlwaysHorizontalScrollbar = 1 << 15,
        AlwaysUseWindowPadding = 1 << 16,
        NoNavInputs = 1 << 18,
        NoNavFocus = 1 << 19,
        UnsavedDocument = 1 << 20,
        NoNav = NoNavInputs | NoNavFocus,
        NoDecoration = NoTitleBar | NoResize | NoScrollbar | NoCollapse,
        NoInputs = NoMouseInputs | NoNavInputs | NoNavFocus,
    }

    [Flags]
    public enum ImGuiTableFlags : int
    {
        None = 0,
        Resizable = 1 << 0,
        Reorderable = 1 << 1,
        Hideable = 1 << 2,
        Sortable = 1 << 3,
        NoSavedSettings = 1 << 4,
        ContextMenuInBody = 1 << 5,
        RowBg = 1 << 6,
        BordersInnerH = 1 << 7,
        BordersOuterH = 1 << 8,
        BordersInnerV = 1 << 9,
        BordersOuterV = 1 << 10,
        BordersH = BordersInnerH | BordersOuterH,
        BordersV = BordersInnerV | BordersOuterV,
        BordersInner = BordersInnerV | BordersInnerH,
        BordersOuter = BordersOuterV | BordersOuterH,
        Borders = BordersInner | BordersOuter,
        NoBordersInBody = 1 << 11,
        NoBordersInBodyUntilResize = 1 << 12,
        SizingFixedFit = 1 << 13,
        SizingFixedSame = 2 << 13,
        SizingStretchProp = 3 << 13,
        SizingStretchSame = 4 << 13,
        NoHostExtendX = 1 << 16,
        NoHostExtendY = 1 << 17,
        NoKeepColumnsVisible = 1 << 18,
        PreciseWidths = 1 << 19,
        NoClip = 1 << 20,
        PadOuterX = 1 << 21,
        NoPadOuterX = 1 << 22,
        NoPadInnerX = 1 << 23,
        ScrollX = 1 << 24,
        ScrollY = 1 << 25,
        SortMulti = 1 << 26,
        SortTristate = 1 << 27,
        HighlightHoveredColumn = 1 << 28,

        // Masks
        SizingMask_ = SizingFixedFit | SizingFixedSame | SizingStretchProp | SizingStretchSame,
    }

    [Flags]
    public enum ImGuiTableColumnFlags : int
    {
        None = 0,
        Disabled = 1 << 0,
        DefaultHide = 1 << 1,
        DefaultSort = 1 << 2,
        WidthStretch = 1 << 3,
        WidthFixed = 1 << 4,
        NoResize = 1 << 5,
        NoReorder = 1 << 6,
        NoHide = 1 << 7,
        NoClip = 1 << 8,
        NoSort = 1 << 9,
        NoSortAscending = 1 << 10,
        NoSortDescending = 1 << 11,
        NoHeaderLabel = 1 << 12,
        NoHeaderWidth = 1 << 13,
        PreferSortAscending = 1 << 14,
        PreferSortDescending = 1 << 15,
        IndentEnable = 1 << 16,
        IndentDisable = 1 << 17,
        AngledHeader = 1 << 18,

        // Status/Runtime masks (set automatically by ImGui)
        IsEnabled = 1 << 24,
        IsVisible = 1 << 25,
        IsSorted = 1 << 26,
        IsHovered = 1 << 27,

        // Masks
        WidthMask_ = WidthStretch | WidthFixed,
        IndentMask_ = IndentEnable | IndentDisable,
        StatusMask_ = IsEnabled | IsVisible | IsSorted | IsHovered,
        NoDirectResize_ = 1 << 30,
    }

    [Flags]
    public enum ImGuiColorEditFlags : int
    {
        None = 0,
        NoAlpha = 1 << 1,
        NoPicker = 1 << 2,
        NoOptions = 1 << 3,
        NoSmallPreview = 1 << 4,
        NoInputs = 1 << 5,
        NoTooltip = 1 << 6,
        NoLabel = 1 << 7,
        NoSidePreview = 1 << 8,
        NoDragDrop = 1 << 9,
        NoBorder = 1 << 10,
        AlphaOpaque = 1 << 11,
        AlphaNoBg = 1 << 12,
        AlphaPreviewHalf = 1 << 13,
        AlphaBar = 1 << 16,
        HDR = 1 << 19,
        DisplayRGB = 1 << 20,
        DisplayHSV = 1 << 21,
        DisplayHex = 1 << 22,
        Uint8 = 1 << 23,
        Float = 1 << 24,
        PickerHueBar = 1 << 25,
        PickerHueWheel = 1 << 26,
        InputRGB = 1 << 27,
        InputHSV = 1 << 28,

        DefaultOptions = Uint8 | DisplayRGB | InputRGB | PickerHueBar,
        AlphaMask = NoAlpha | AlphaOpaque | AlphaNoBg | AlphaPreviewHalf,
        DisplayMask = DisplayRGB | DisplayHSV | DisplayHex,
        DataTypeMask = Uint8 | Float,
        PickerMask = PickerHueWheel | PickerHueBar,
        InputMask = InputRGB | InputHSV,
    }


    [Flags]
    public enum ImGuiTableRowFlags : int
    {
        None = 0,
        Headers = 1 << 0,
    }

    public enum ImGuiTableBgTarget : int
    {
        None = 0,
        RowBg0 = 1,
        RowBg1 = 2,
        CellBg = 3,
    }

    [Flags]
    public enum ImGuiDockNodeFlags : int
    {
        None = 0,
        KeepAliveOnly = 1 << 0,
        NoDockingInCentralNode = 1 << 2,
        PassthruCentralNode = 1 << 3,
        NoSplit = 1 << 4,
    }

    public enum ImGuiCol : int
    {
        Text = 0,
        TextDisabled,
        WindowBg,
        ChildBg,
        PopupBg,
        Border,
        BorderShadow,
        FrameBg,
        FrameBgHovered,
        FrameBgActive,
        TitleBg,
        TitleBgActive,
        TitleBgCollapsed,
        MenuBarBg,
        ScrollbarBg,
        ScrollbarGrab,
        ScrollbarGrabHovered,
        ScrollbarGrabActive,
        CheckMark,
        SliderGrab,
        SliderGrabActive,
        Button,
        ButtonHovered,
        ButtonActive,
        Header,
        HeaderHovered,
        HeaderActive,
        Separator,
        SeparatorHovered,
        SeparatorActive,
        ResizeGrip,
        ResizeGripHovered,
        ResizeGripActive,
        Tab,
        TabHovered,
        TabActive,
        TabUnfocused,
        TabUnfocusedActive,
        DockingPreview,
        DockingEmptyBg,
        PlotLines,
        PlotLinesHovered,
        PlotHistogram,
        PlotHistogramHovered,
        TableHeaderBg,
        TableBorderStrong,
        TableBorderLight,
        TableRowBg,
        TableRowBgAlt,
        TextSelectedBg,
        DragDropTarget,
        NavHighlight,
        NavWindowingHighlight,
        NavWindowingDimBg,
        ModalWindowDimBg,
        COUNT
    }

    public enum ImGuiStyleVar : int
    {
        Alpha = 0,
        DisabledAlpha = 1,
        WindowPadding = 2,
        WindowRounding = 3,
        WindowBorderSize = 4,
        WindowMinSize = 5,
        WindowTitleAlign = 6,
        ChildRounding = 7,
        ChildBorderSize = 8,
        PopupRounding = 9,
        PopupBorderSize = 10,
        FramePadding = 11,
        FrameRounding = 12,
        FrameBorderSize = 13,
        ItemSpacing = 14,
        ItemInnerSpacing = 15,
        IndentSpacing = 16,
        CellPadding = 17,
        ScrollbarSize = 18,
        ScrollbarRounding = 19,
        GrabMinSize = 20,
        GrabRounding = 21,
        ImageBorderSize = 22,
        TabRounding = 23,
        TabBorderSize = 24,
        TabBarBorderSize = 25,
        TabBarOverlineSize = 26,
        TableAngledHeadersAngle = 27,
        TableAngledHeadersTextAlign = 28,
        ButtonTextAlign = 29,
        SelectableTextAlign = 30,
        SeparatorTextBorderSize = 31,
        SeparatorTextAlign = 32,
        SeparatorTextPadding = 33,
        DockingSeparatorSize = 34,
        COUNT = 35
    }

    [Flags]
    public enum ImGuiInputTextFlags : int
    {
        None = 0,
        CharsDecimal = 1 << 0,
        CharsHexadecimal = 1 << 1,
        CharsUppercase = 1 << 2,
        CharsNoBlank = 1 << 3,
        AutoSelectAll = 1 << 4,
        EnterReturnsTrue = 1 << 5,
        CallbackCompletion = 1 << 6,
        CallbackHistory = 1 << 7,
        CallbackAlways = 1 << 8,
        CallbackCharFilter = 1 << 9,
        AllowTabInput = 1 << 10,
        CtrlEnterForNewLine = 1 << 11,
        NoHorizontalScroll = 1 << 12,
        AlwaysOverwrite = 1 << 13,
        ReadOnly = 1 << 14,
        Password = 1 << 15,
        NoUndoRedo = 1 << 16,
        CharsScientific = 1 << 17,
        CallbackResize = 1 << 18,
        CallbackEdit = 1 << 19
    }

    [Flags]
    public enum ImGuiTreeNodeFlags : int
    {
        None = 0,
        Selected = 1 << 0,
        Framed = 1 << 1,
        AllowItemOverlap = 1 << 2,
        NoTreePushOnOpen = 1 << 3,
        NoAutoOpenOnLog = 1 << 4,
        DefaultOpen = 1 << 5,
        OpenOnDoubleClick = 1 << 6,
        OpenOnArrow = 1 << 7,
        Leaf = 1 << 8,
        Bullet = 1 << 9,
        FramePadding = 1 << 10,
        SpanAvailWidth = 1 << 11,
        SpanFullWidth = 1 << 12,
        NavLeftJumpsBackHere = 1 << 13,
        CollapsingHeader = Framed | NoTreePushOnOpen | NoAutoOpenOnLog
    }

    [Flags]
    public enum ImGuiSelectableFlags : int
    {
        None = 0,
        DontClosePopups = 1 << 0,
        SpanAllColumns = 1 << 1,
        AllowDoubleClick = 1 << 2,
        Disabled = 1 << 3,
        AllowItemOverlap = 1 << 4
    }

    public enum ImGuiTabBarFlags : int
    {
        None = 0,
        Reorderable = 1 << 0,
        AutoSelectNewTabs = 1 << 1,
        TabListPopupButton = 1 << 2,
        NoCloseWithMiddleMouseButton = 1 << 3,
        NoTabListScrollingButtons = 1 << 4,
        NoTooltip = 1 << 5,
        FittingPolicyResizeDown = 1 << 6,
        FittingPolicyScroll = 1 << 7,
        FittingPolicyMask_ = FittingPolicyResizeDown | FittingPolicyScroll,
        FittingPolicyDefault_ = FittingPolicyResizeDown
    }

    public enum ImGuiTabItemFlags : int
    {
        None = 0,
        UnsavedDocument = 1 << 0,
        SetSelected = 1 << 1,
        NoCloseWithMiddleMouseButton = 1 << 2,
        NoPushId = 1 << 3,
        NoTooltip = 1 << 4,
        NoReorder = 1 << 5,
        Leading = 1 << 6,
        Trailing = 1 << 7
    }

    // ImGuiKey enum for keyboard input
    public enum ImGuiKey : int
    {
        None = 0,
        Tab = 512,
        LeftArrow = 513,
        RightArrow = 514,
        UpArrow = 515,
        DownArrow = 516,
        PageUp = 517,
        PageDown = 518,
        Home = 519,
        End = 520,
        Insert = 521,
        Delete = 522,
        Backspace = 523,
        Space = 524,
        Enter = 525,
        Escape = 526,
        Apostrophe = 527,
        Comma = 528,
        Minus = 529,
        Period = 530,
        Slash = 531,
        Semicolon = 532,
        Equal = 533,
        LeftBracket = 534,
        Backslash = 535,
        RightBracket = 536,
        GraveAccent = 537,
        CapsLock = 538,
        ScrollLock = 539,
        NumLock = 540,
        PrintScreen = 541,
        Pause = 542,
        Keypad0 = 543,
        Keypad1 = 544,
        Keypad2 = 545,
        Keypad3 = 546,
        Keypad4 = 547,
        Keypad5 = 548,
        Keypad6 = 549,
        Keypad7 = 550,
        Keypad8 = 551,
        Keypad9 = 552,
        KeypadDecimal = 553,
        KeypadDivide = 554,
        KeypadMultiply = 555,
        KeypadSubtract = 556,
        KeypadAdd = 557,
        KeypadEnter = 558,
        KeypadEqual = 559,
        LeftShift = 560,
        LeftCtrl = 561,
        LeftAlt = 562,
        LeftSuper = 563,
        RightShift = 564,
        RightCtrl = 565,
        RightAlt = 566,
        RightSuper = 567,
        Menu = 568,
        _0 = 569,
        _1 = 570,
        _2 = 571,
        _3 = 572,
        _4 = 573,
        _5 = 574,
        _6 = 575,
        _7 = 576,
        _8 = 577,
        _9 = 578,
        A = 579,
        B = 580,
        C = 581,
        D = 582,
        E = 583,
        F = 584,
        G = 585,
        H = 586,
        I = 587,
        J = 588,
        K = 589,
        L = 590,
        M = 591,
        N = 592,
        O = 593,
        P = 594,
        Q = 595,
        R = 596,
        S = 597,
        T = 598,
        U = 599,
        V = 600,
        W = 601,
        X = 602,
        Y = 603,
        Z = 604,
        F1 = 605,
        F2 = 606,
        F3 = 607,
        F4 = 608,
        F5 = 609,
        F6 = 610,
        F7 = 611,
        F8 = 612,
        F9 = 613,
        F10 = 614,
        F11 = 615,
        F12 = 616
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImVec2
    {
        public float x, y;
        public ImVec2(float x, float y) { this.x = x; this.y = y; }
        public static implicit operator ImVec2(Vector2 v) => new ImVec2(v.X, v.Y);
        public static implicit operator Vector2(ImVec2 v) => new Vector2(v.x, v.y);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImRect
    {
        public ImVec2 Min;
        public ImVec2 Max;
        public ImRect(ImVec2 min, ImVec2 max) { Min = min; Max = max; }
        public ImRect(float x1, float y1, float x2, float y2)
            : this(new ImVec2(x1, y1), new ImVec2(x2, y2)) { }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImVec4
    {
        public float x, y, z, w;
        public ImVec4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static implicit operator ImVec4(Vector4 v) => new ImVec4(v.X, v.Y, v.Z, v.W);
        public static implicit operator Vector4(ImVec4 v) => new Vector4(v.x, v.y, v.z, v.w);
    }

    // Font configuration structure
    [StructLayout(LayoutKind.Sequential)]
    public struct ImFontConfig
    {
        public IntPtr FontData;
        public int FontDataSize;
        public byte FontDataOwnedByAtlas;
        public int FontNo;
        public float SizePixels;
        public int OversampleH;
        public int OversampleV;
        public byte PixelSnapH;
        public float GlyphExtraSpacing_X;
        public float GlyphExtraSpacing_Y;
        public float GlyphOffset_X;
        public float GlyphOffset_Y;
        public IntPtr GlyphRanges;
        public float GlyphMinAdvanceX;
        public float GlyphMaxAdvanceX;
        public byte MergeMode;
        public uint FontBuilderFlags;
        public float RasterizerMultiply;
        public char EllipsisChar;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] Name;
        public IntPtr DstFont;
    }

    // Callback delegate for InputText
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int ImGuiInputTextCallback(ImGuiInputTextCallbackData* data);

    // Callback data structure
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ImGuiInputTextCallbackData
    {
        public ImGuiInputTextFlags EventFlag;
        public ImGuiInputTextFlags Flags;
        public IntPtr UserData;
        public ImGuiKey EventKey;
        public char EventChar;
        public uint EventKey2;
        public byte BufDirty;
        public int CursorPos;
        public int SelectionStart;
        public int SelectionEnd;
        public int BufTextLen;
        public int BufSize;
        public byte* Buf;

        // Helper methods
        public void DeleteChars(int pos, int bytes_count)
        {
            ImGui.igImGuiInputTextCallbackData_DeleteChars(ref this, pos, bytes_count);
        }

        public void InsertChars(int pos, string text)
        {
            ImGui.igImGuiInputTextCallbackData_InsertChars(ref this, pos, text);
        }
    }

    // Delegate types for callbacks:
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr ImGuiListBoxItemGetter(IntPtr data, int idx);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate float ImGuiPlotGetter(IntPtr data, int idx);

    public unsafe static class ImGui
    {
        private const string DLL = "EngineCore";

        // imgui context
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr igGetIO_ContextPtr(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr igGetIO_Nil();

        // ImGui window functions (cimgui)
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igBegin(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
            ref bool p_open,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igBegin(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
            IntPtr p_open, // can pass IntPtr.Zero if you don't care about close button
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEnd();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igText(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string fmt);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTextColored(ImVec4 col,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string fmt);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTextWrapped(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string fmt);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igButton(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ImVec2 size);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igSmallButton(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igInputText(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            byte[] buf,
            uint buf_size,
            int flags,
            IntPtr callback,
            IntPtr user_data);

        // Version 2: Delegate callback (for when callback is not null)  
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igInputText(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            byte[] buf,
            uint buf_size,
            int flags,
            ImGuiInputTextCallback callback,
            IntPtr user_data);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igInputFloat(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ref float v,
            float step,
            float step_fast,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string format,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igInputInt(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ref int v,
            int step,
            int step_fast,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igCheckbox(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ref bool v);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igSliderFloat(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ref float v,
            float v_min,
            float v_max,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string format,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igSliderInt(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ref int v,
            int v_min,
            int v_max,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string format,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSeparator();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSameLine(float offset_from_start_x, float spacing);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igNewLine();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSpacing();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igTreeNode(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTreePop();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "igCollapsingHeader_BoolPtr")]
        private static extern byte igCollapsingHeader_BoolPtr(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ref bool p_open,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "igCollapsingHeader_TreeNodeFlags")]
        private static extern byte igCollapsingHeader_TreeNodeFlags(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igTreeNodeEx_Str(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            int flags);

        // Columns
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igColumns(int count,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string id,
            byte border);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igNextColumn();

        // Tab bars
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igBeginTabBar(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string str_id,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndTabBar();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igBeginTabItem(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            IntPtr p_open,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndTabItem();

        // Drag controls
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igDragFloat(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ref float v,
            float v_speed,
            float v_min,
            float v_max,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string format,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igDragFloat2(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ref float v,
            float v_speed,
            float v_min,
            float v_max,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string format,
            int flags);

        // Progress bar
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igProgressBar(
            float fraction,
            ImVec2 size_arg,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string overlay);

        // Multi-line text input
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igInputTextMultiline(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            byte[] buf,
            uint buf_size,
            ImVec2 size,
            int flags,
            IntPtr callback,
            IntPtr user_data);

        // Selectables
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igSelectable_Bool(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            byte selected,
            int flags,
            ImVec2 size);

        // Popups and menus
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igBeginPopupContextItem(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string str_id,
            int popup_flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndPopup();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igMenuItem_Bool(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string shortcut,
            byte selected,
            byte enabled);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igBeginMenu([MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            byte enabled);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndMenu();

        /// <summary>
        /// Call between BeginMenuBar() / EndMenuBar() inside a window to draw your menus.
        /// </summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igBeginMenuBar();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndMenuBar();


        // Layout
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igIndent(float indent_w);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igUnindent(float indent_w);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetCursorPos(ImVec2 local_pos);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetCursorPosX(float local_x);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushID_Int(int int_id);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushID_Str(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string str_id);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPopID();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushFont(IntPtr font);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPopFont();

        // Child windows
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igBeginChild_Str(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string str_id,
            ImVec2 size,
            byte border,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igBeginChild_ID(
            uint id,
            ImVec2 size,
            byte border,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndChild();

        // Window utilities
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetNextWindowPos(ImVec2 pos, int cond, ImVec2 pivot);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetNextWindowSize(ImVec2 size, int cond);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igGetWindowPos(out ImVec2 pOut);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern float igGetWindowWidth();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern float igGetWindowHeight();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igGetContentRegionAvail(out ImVec2 pOut);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igGetCursorPos(out ImVec2 pOut);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern float igGetCursorPosX();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern float igGetCursorPosY();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern float igGetTextLineHeight();

        // Item/Widgets utilities
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igIsItemHovered(int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushItemWidth(float item_width);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPopItemWidth();

        // Tooltips
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetTooltip(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string fmt);

        // Docking helpers
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetNextWindowDockID(uint dock_id, int cond);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint igDockSpaceOverViewport(
            uint dockspace_id, // first argument is ImGuiID (uint)
            IntPtr viewport,   // second: viewport (or IntPtr.Zero)
            int flags,
            IntPtr window_class // or IntPtr.Zero
        );

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ImGui_SetDockingEnabled(int enabled);

        // IsKeyPressed
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igIsKeyPressed(int key, byte repeat);

        public static bool IsKeyPressed(ImGuiKey key, bool repeat = true)
            => igIsKeyPressed((int)key, (byte)(repeat ? 1 : 0)) != 0;

        // IsItemActive
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igIsItemActive();

        public static bool IsItemActive()
            => igIsItemActive() != 0;

        // PushStyleColor
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushStyleColor_Vec4(int idx, ImVec4 col);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushStyleColor_U32(int idx, uint col);

        public static void PushStyleColor(ImGuiCol col, Vector4 color)
            => igPushStyleColor_Vec4((int)col, new ImVec4(color.X, color.Y, color.Z, color.W));

        // If you want to use packed color (e.g. 0xAARRGGBB):
        public static void PushStyleColor(ImGuiCol col, uint packedColor)
            => igPushStyleColor_U32((int)col, packedColor);

        // PopStyleColor
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPopStyleColor(int count);

        public static void PopStyleColor(int count = 1)
            => igPopStyleColor(count);

        // PushStyleVar (float)
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushStyleVar_Float(int idx, float val);

        // PushStyleVar (ImVec2)
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPushStyleVar_Vec2(int idx, ImVec2 val);

        public static void PushStyleVar(ImGuiStyleVar idx, float val)
            => igPushStyleVar_Float((int)idx, val);

        public static void PushStyleVar(ImGuiStyleVar idx, Vector2 val)
            => igPushStyleVar_Vec2((int)idx, new ImVec2(val.X, val.Y));

        // PopStyleVar
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igPopStyleVar(int count);

        public static void PopStyleVar(int count = 1)
            => igPopStyleVar(count);

        /// <summary>Globally enable/disable Dear ImGui docking.</summary>
        public static void SetDockingEnabled(bool enabled)
            => ImGui_SetDockingEnabled(enabled ? 1 : 0);

        // SetCursorPosY
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetCursorPosY(float y);

        public static void SetCursorPosY(float y)
            => igSetCursorPosY(y);

        // SetKeyboardFocusHere
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetKeyboardFocusHere(int offset);

        public static void SetKeyboardFocusHere(int offset = 0)
            => igSetKeyboardFocusHere(offset);

        // SetNextItemWidth
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetNextItemWidth(float width);

        public static void SetNextItemWidth(float width)
            => igSetNextItemWidth(width);

        // SetScrollHereY
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetScrollHereY(float center_y_ratio);

        public static void SetScrollHereY(float center_y_ratio = 0.5f)
            => igSetScrollHereY(center_y_ratio);

        // TextUnformatted
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTextUnformatted(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
                IntPtr text_end);

        // Callback data helpers
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void igImGuiInputTextCallbackData_DeleteChars(
            ref ImGuiInputTextCallbackData self,
            int pos,
            int bytes_count);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void igImGuiInputTextCallbackData_InsertChars(
            ref ImGuiInputTextCallbackData self,
            int pos,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text);


        // void igImFontAtlasUpdateSourcesPointers(ImFontAtlas* atlas);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igImFontAtlasUpdateSourcesPointers(IntPtr atlas);

        // void igImFontAtlasBuildInit(ImFontAtlas* atlas);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igImFontAtlasBuildInit(IntPtr atlas);

        // void igImFontAtlasBuildSetupFont(ImFontAtlas* atlas, ImFont* font, ImFontConfig* src, float ascent, float descent);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igImFontAtlasBuildSetupFont(IntPtr atlas, IntPtr font, IntPtr src, float ascent, float descent);

        // void igImFontAtlasBuildPackCustomRects(ImFontAtlas* atlas, void* stbrp_context_opaque);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igImFontAtlasBuildPackCustomRects(IntPtr atlas, IntPtr stbrp_context_opaque);

        // void igImFontAtlasBuildFinish(ImFontAtlas* atlas);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igImFontAtlasBuildFinish(IntPtr atlas);

        // void igImFontAtlasBuildRender8bppRectFromString(ImFontAtlas* atlas, int x, int y, int w, int h, const char* in_str, char in_marker_char, unsigned char in_marker_pixel_value);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igImFontAtlasBuildRender8bppRectFromString(
            IntPtr atlas, int x, int y, int w, int h,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string in_str,
            byte in_marker_char, byte in_marker_pixel_value);

        // void igImFontAtlasBuildRender32bppRectFromString(ImFontAtlas* atlas, int x, int y, int w, int h, const char* in_str, char in_marker_char, unsigned int in_marker_pixel_value);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igImFontAtlasBuildRender32bppRectFromString(
            IntPtr atlas, int x, int y, int w, int h,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string in_str,
            byte in_marker_char, uint in_marker_pixel_value);

        // void igImFontAtlasBuildMultiplyCalcLookupTable(unsigned char out_table[256], float in_multiply_factor);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igImFontAtlasBuildMultiplyCalcLookupTable(
            [Out] byte[] out_table, float in_multiply_factor);

        // void igImFontAtlasBuildMultiplyRectAlpha8(const unsigned char table[256], unsigned char* pixels, int x, int y, int w, int h, int stride);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igImFontAtlasBuildMultiplyRectAlpha8(
            [In] byte[] table, IntPtr pixels, int x, int y, int w, int h, int stride);

        // void igImFontAtlasBuildGetOversampleFactors(const ImFontConfig* src, int* out_oversample_h, int* out_oversample_v);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igImFontAtlasBuildGetOversampleFactors(
            IntPtr src, out int out_oversample_h, out int out_oversample_v);

        // bool igImFontAtlasGetMouseCursorTexData(ImFontAtlas* atlas, ImGuiMouseCursor cursor_type, ImVec2* out_offset, ImVec2* out_size, ImVec2 out_uv_border[2], ImVec2 out_uv_fill[2]);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool igImFontAtlasGetMouseCursorTexData(
            IntPtr atlas,
            int cursor_type, // Or use your ImGuiMouseCursor enum here
            out ImVec2 out_offset,
            out ImVec2 out_size,
            [Out] ImVec2[] out_uv_border, // length 2
            [Out] ImVec2[] out_uv_fill);  // length 2

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr igGetFont();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern float igGetFontSize();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igGetFontTexUvWhitePixel(out ImVec2 pOut);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igSetWindowFontScale(float scale);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void ImDrawList_AddText_FontPtr(
            IntPtr draw_list,
            IntPtr font,
            float font_size,
            ImVec2 pos,
            uint col,
            byte* text_begin,
            byte* text_end,
            float wrap_width,
            ImVec4* cpu_fine_clip_rect
        );


        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontConfig_ImFontConfig();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontConfig_destroy(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontGlyphRangesBuilder_destroy(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontGlyphRangesBuilder_Clear(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool ImFontGlyphRangesBuilder_GetBit(IntPtr self, UIntPtr n);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontGlyphRangesBuilder_SetBit(IntPtr self, UIntPtr n);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontGlyphRangesBuilder_AddChar(IntPtr self, ushort c);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontGlyphRangesBuilder_AddText(
            IntPtr self,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text_end);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontGlyphRangesBuilder_AddRanges(IntPtr self, IntPtr ranges);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontGlyphRangesBuilder_BuildRanges(IntPtr self, IntPtr out_ranges);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlasCustomRect_ImFontAtlasCustomRect();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontAtlasCustomRect_destroy(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool ImFontAtlasCustomRect_IsPacked(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_ImFontAtlas();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontAtlas_destroy(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_AddFont(IntPtr self, IntPtr font_cfg);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_AddFontDefault(IntPtr self, IntPtr font_cfg);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_AddFontFromFileTTF(
            IntPtr self,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string filename,
            float size_pixels,
            IntPtr font_cfg,
            IntPtr glyph_ranges);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_AddFontFromMemoryTTF(
            IntPtr self,
            IntPtr font_data,
            int font_data_size,
            float size_pixels,
            IntPtr font_cfg,
            IntPtr glyph_ranges);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_AddFontFromMemoryCompressedTTF(
            IntPtr self,
            IntPtr compressed_font_data,
            int compressed_font_data_size,
            float size_pixels,
            IntPtr font_cfg,
            IntPtr glyph_ranges);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_AddFontFromMemoryCompressedBase85TTF(
            IntPtr self,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string compressed_font_data_base85,
            float size_pixels,
            IntPtr font_cfg,
            IntPtr glyph_ranges);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontAtlas_ClearInputData(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontAtlas_ClearFonts(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontAtlas_ClearTexData(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontAtlas_Clear(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool ImFontAtlas_Build(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontAtlas_GetTexDataAsAlpha8(
            IntPtr self,
            out IntPtr out_pixels,
            out int out_width,
            out int out_height,
            out int out_bytes_per_pixel);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontAtlas_GetTexDataAsRGBA32(
            IntPtr self,
            out IntPtr out_pixels,
            out int out_width,
            out int out_height,
            out int out_bytes_per_pixel);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool ImFontAtlas_IsBuilt(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontAtlas_SetTexID(IntPtr self, IntPtr id);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_GetGlyphRangesDefault(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_GetGlyphRangesGreek(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_GetGlyphRangesKorean(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_GetGlyphRangesJapanese(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_GetGlyphRangesChineseFull(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_GetGlyphRangesChineseSimplifiedCommon(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_GetGlyphRangesCyrillic(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_GetGlyphRangesThai(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_GetGlyphRangesVietnamese(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ImFontAtlas_AddCustomRectRegular(IntPtr self, int width, int height);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ImFontAtlas_AddCustomRectFontGlyph(
            IntPtr self, IntPtr font, ushort id, int width, int height, float advance_x, ImVec2 offset);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFontAtlas_GetCustomRectByIndex(IntPtr self, int index);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFontAtlas_CalcCustomRectUV(
            IntPtr self, IntPtr rect, out ImVec2 out_uv_min, out ImVec2 out_uv_max);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFont_ImFont();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFont_destroy(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFont_FindGlyph(IntPtr self, ushort c);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFont_FindGlyphNoFallback(IntPtr self, ushort c);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ImFont_GetCharAdvance(IntPtr self, ushort c);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool ImFont_IsLoaded(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFont_GetDebugName(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFont_CalcTextSizeA(
            out ImVec2 pOut,
            IntPtr self,
            float size,
            float max_width,
            float wrap_width,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text_begin,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text_end,
            out IntPtr remaining);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImFont_CalcWordWrapPositionA(
            IntPtr self,
            float scale,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text_end,
            float wrap_width);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFont_RenderChar(
            IntPtr self,
            IntPtr draw_list,
            float size,
            ImVec2 pos,
            uint col,
            ushort c);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFont_RenderText(
            IntPtr self,
            IntPtr draw_list,
            float size,
            ImVec2 pos,
            uint col,
            ImVec4 clip_rect,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text_begin,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text_end,
            float wrap_width,
            [MarshalAs(UnmanagedType.I1)] bool cpu_fine_clip);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFont_BuildLookupTable(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFont_ClearOutputData(IntPtr self);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFont_GrowIndex(IntPtr self, int new_size);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFont_AddGlyph(
            IntPtr self, IntPtr src_cfg, ushort c,
            float x0, float y0, float x1, float y1,
            float u0, float v0, float u1, float v1, float advance_x);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImFont_AddRemapChar(
            IntPtr self, ushort dst, ushort src, [MarshalAs(UnmanagedType.I1)] bool overwrite_dst);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool ImFont_IsGlyphRangeUnused(IntPtr self, uint c_begin, uint c_last);

        // ImGuiContext* ImGuiContext_ImGuiContext(ImFontAtlas* shared_font_atlas);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImGuiContext_ImGuiContext(IntPtr shared_font_atlas);

        // float ImGuiWindow_CalcFontSize(ImGuiWindow* self);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ImGuiWindow_CalcFontSize(IntPtr self);

        // void igSetCurrentFont(ImFont* font);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igSetCurrentFont(IntPtr font);

        // ImFont* igGetDefaultFont(void);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr igGetDefaultFont();

        // void igPushPasswordFont(void);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igPushPasswordFont();

        // void igShowFontAtlas(ImFontAtlas* atlas);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igShowFontAtlas(IntPtr atlas);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void igCalcTextSize(
            out ImVec2 pOut,
            byte* text,          // const char*
            byte* text_end,      // const char*
            [MarshalAs(UnmanagedType.I1)] bool hide_text_after_double_hash,
            float wrap_width
        );

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igBulletText(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string fmt
        /*, ... args -- varargs not supported directly in .NET, see wrapper below */
        );

        // const char* igGetVersion();
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr igGetVersion();

        // void igSetColumnWidth(int column_index, float width);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igSetColumnWidth(int column_index, float width);

        // void igShowAboutWindow(bool* p_open);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igShowAboutWindow(ref bool p_open);

        // void igShowDebugLogWindow(bool* p_open);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igShowDebugLogWindow(ref bool p_open);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igShowStyleEditor(IntPtr style);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igShowMetricsWindow(ref bool p_open);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igShowIDStackToolWindow(ref bool p_open);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igShowDemoWindow(ref bool p_open);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr igGetStyle();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern float igGetScrollX();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern float igGetScrollY();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern float igGetScrollMaxX();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern float igGetScrollMaxY();
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool igBeginComboPopup(uint popup_id, ImRect bb, int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool igBeginComboPreview();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndComboPreview();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool igBeginCombo(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string preview_value,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndCombo();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool igCombo_Str_arr(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ref int current_item,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] items,
            int items_count,
            int popup_max_height_in_items);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool igCombo_Str(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ref int current_item,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string items_separated_by_zeros,
            int popup_max_height_in_items);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr ComboItemGetter(IntPtr user_data, int idx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool igCombo_FnStrPtr(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ref int current_item,
            ComboItemGetter getter,
            IntPtr user_data,
            int items_count,
            int popup_max_height_in_items);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr igGetWindowDrawList();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool igIsWindowHovered(int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igGetMousePos(out ImVec2 pOut);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igGetMousePosOnOpeningCurrentPopup(out ImVec2 pOut);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igGetCursorScreenPos(out ImVec2 pOut);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetCursorScreenPos(ImVec2 pos);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igDrawList_AddRectFilled(
            IntPtr drawList,
            ImVec2 p_min,
            ImVec2 p_max,
            uint col,
            float rounding,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool igBeginTable(
    [MarshalAs(UnmanagedType.LPUTF8Str)] string str_id,
    int columns,
    ImGuiTableFlags flags,
    ImVec2 outer_size,
    float inner_width);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igEndTable();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTableNextRow(ImGuiTableRowFlags row_flags, float min_row_height);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool igTableNextColumn();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool igTableSetColumnIndex(int column_n);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTableSetupColumn(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            ImGuiTableColumnFlags flags,
            float init_width_or_weight,
            uint user_id);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTableSetupScrollFreeze(int cols, int rows);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTableHeader([MarshalAs(UnmanagedType.LPUTF8Str)] string label);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTableHeadersRow();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTableAngledHeadersRow();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr igTableGetSortSpecs();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int igTableGetColumnCount();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int igTableGetColumnIndex();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int igTableGetRowIndex();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr igTableGetColumnName_Int(int column_n);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern ImGuiTableColumnFlags igTableGetColumnFlags(int column_n);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTableSetColumnEnabled(int column_n, [MarshalAs(UnmanagedType.I1)] bool v);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int igTableGetHoveredColumn();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igTableSetBgColor(ImGuiTableBgTarget target, uint color, int column_n);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igInputTextWithHint(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string hint,
            byte[] buf,
            UIntPtr buf_size,
            int flags,
            ImGuiInputTextCallback callback,
            IntPtr user_data);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igInputFloat2(
         [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
         float[] v,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string format,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igInputFloat3(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            float[] v,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string format,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern byte igInputFloat4(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
            float[] v,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string format,
            int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igSetNextWindowBgAlpha(float alpha);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool igColorButton(
    [MarshalAs(UnmanagedType.LPStr)] string desc_id,
    ImVec4 col,
    int flags,
    ImVec2 size);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igSetColorEditOptions(int flags);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool igTreeNode_Str(
            [MarshalAs(UnmanagedType.LPStr)] string label);

        // varargs version – you can still P/Invoke it, but C# callers will
        // usually use the wrapper below instead of calling this directly:
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool igTreeNode_StrStr(
            [MarshalAs(UnmanagedType.LPStr)] string str_id,
            [MarshalAs(UnmanagedType.LPStr)] string fmt,
            __arglist);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igColorConvertU32ToFloat4(
            out ImVec4 pOut,
            uint in_col);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint igColorConvertFloat4ToU32(ImVec4 col);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool igListBox_Str_arr(
            [MarshalAs(UnmanagedType.LPStr)] string label,
            ref int current_item,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)]
            string[] items,
            int items_count,
            int height_in_items);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool igListBox_FnStrPtr(
            [MarshalAs(UnmanagedType.LPStr)] string label,
            ref int current_item,
            ImGuiListBoxItemGetter getter,
            IntPtr user_data,
            int items_count,
            int height_in_items);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igPlotLines_FloatPtr(
            [MarshalAs(UnmanagedType.LPStr)] string label,
            float* values,
            int values_count,
            int values_offset,
            [MarshalAs(UnmanagedType.LPStr)] string overlay_text,
            float scale_min,
            float scale_max,
            ImVec2 graph_size,
            int stride);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igPlotLines_FnFloatPtr(
            [MarshalAs(UnmanagedType.LPStr)] string label,
            ImGuiPlotGetter values_getter,
            IntPtr data,
            int values_count,
            int values_offset,
            [MarshalAs(UnmanagedType.LPStr)] string overlay_text,
            float scale_min,
            float scale_max,
            ImVec2 graph_size);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igPlotHistogram_FloatPtr(
            [MarshalAs(UnmanagedType.LPStr)] string label,
            float* values,
            int values_count,
            int values_offset,
            [MarshalAs(UnmanagedType.LPStr)] string overlay_text,
            float scale_min,
            float scale_max,
            ImVec2 graph_size,
            int stride);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igPlotHistogram_FnFloatPtr(
            [MarshalAs(UnmanagedType.LPStr)] string label,
            ImGuiPlotGetter values_getter,
            IntPtr data,
            int values_count,
            int values_offset,
            [MarshalAs(UnmanagedType.LPStr)] string overlay_text,
            float scale_min,
            float scale_max,
            ImVec2 graph_size);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igBeginGroup();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void igEndGroup();




        // --- Managed API ---

        /// <summary>
        /// Get the ImGuiIO pointer for the specified ImGuiContext.
        /// </summary>
        public static IntPtr GetIOForContext(IntPtr context)
        {
            try { return igGetIO_ContextPtr(context); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] GetIOForContext failed: {ex.Message}");
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Get the ImGuiIO pointer for the "nil" (null/default) context.
        /// </summary>
        public static IntPtr GetIO()
        {
            try { return igGetIO_Nil(); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] GetIO_Nil failed: {ex.Message}");
                return IntPtr.Zero;
            }
        }


        public static bool Begin(string name, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        {
            try { return igBegin(name, ref open, (int)flags) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Begin failed: {ex.Message}"); return false; }
        }

        public static bool Begin(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        {
            try { return igBegin(name, IntPtr.Zero, (int)flags) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Begin (no close) failed: {ex.Message}"); return false; }
        }

        public static void End()
        {
            try { igEnd(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] End failed: {ex.Message}"); }
        }

        public static void Text(string text)
        {
            try { igText(text); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Text failed: {ex.Message}"); }
        }

        public static void TextColored(Vector4 color, string text)
        {
            try { igTextColored(new ImVec4(color.X, color.Y, color.Z, color.W), text); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TextColored failed: {ex.Message}"); }
        }

        public static void TextColored(float r, float g, float b, float a, string text)
        {
            try { igTextColored(new ImVec4(r, g, b, a), text); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TextColored failed: {ex.Message}"); }
        }

        public static void TextWrapped(string text)
        {
            try { igTextWrapped(text); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TextWrapped failed: {ex.Message}"); }
        }

        public static bool Button(string label, Vector2 size = default)
        {
            try { return igButton(label, new ImVec2(size.X, size.Y)) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Button failed: {ex.Message}"); return false; }
        }

        public static bool SmallButton(string label)
        {
            try { return igSmallButton(label) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SmallButton failed: {ex.Message}"); return false; }
        }

        public static bool InputText(string label, ref string text, uint maxLength = 256, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallback callback = null)
        {
            try
            {
                var buffer = new byte[maxLength];
                var textBytes = Encoding.UTF8.GetBytes(text ?? "");
                var copyLength = Math.Min(textBytes.Length, (int)maxLength - 1);
                Array.Copy(textBytes, buffer, copyLength);

                bool result;
                if (callback != null)
                {
                    result = igInputText(label, buffer, maxLength, (int)flags, callback, IntPtr.Zero) != 0;
                }
                else
                {
                    result = igInputText(label, buffer, maxLength, (int)flags, IntPtr.Zero, IntPtr.Zero) != 0;
                }

                if (result)
                {
                    var nullIndex = Array.IndexOf(buffer, (byte)0);
                    if (nullIndex >= 0)
                        text = Encoding.UTF8.GetString(buffer, 0, nullIndex);
                    else
                        text = Encoding.UTF8.GetString(buffer);
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] InputText failed: {ex.Message}");
                return false;
            }
        }

        public static bool InputFloat(string label, ref float value, float step = 0.0f, float stepFast = 0.0f, string format = "%.3f")
        {
            try { return igInputFloat(label, ref value, step, stepFast, format, 0) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] InputFloat failed: {ex.Message}"); return false; }
        }

        public static bool InputInt(string label, ref int value, int step = 1, int stepFast = 100)
        {
            try { return igInputInt(label, ref value, step, stepFast, 0) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] InputInt failed: {ex.Message}"); return false; }
        }

        public static bool Checkbox(string label, ref bool value)
        {
            try { return igCheckbox(label, ref value) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Checkbox failed: {ex.Message}"); return false; }
        }

        public static bool SliderFloat(string label, ref float value, float min, float max, string format = "%.3f")
        {
            try { return igSliderFloat(label, ref value, min, max, format, 0) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SliderFloat failed: {ex.Message}"); return false; }
        }

        public static bool SliderInt(string label, ref int value, int min, int max, string format = "%d")
        {
            try { return igSliderInt(label, ref value, min, max, format, 0) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SliderInt failed: {ex.Message}"); return false; }
        }

        public static void Separator()
        {
            try { igSeparator(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Separator failed: {ex.Message}"); }
        }

        public static void SameLine(float offsetFromStartX = 0.0f, float spacing = -1.0f)
        {
            try { igSameLine(offsetFromStartX, spacing); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SameLine failed: {ex.Message}"); }
        }

        public static void NewLine()
        {
            try { igNewLine(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] NewLine failed: {ex.Message}"); }
        }

        public static void Spacing()
        {
            try { igSpacing(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Spacing failed: {ex.Message}"); }
        }

        public static bool TreeNode(string label)
        {
            try { return igTreeNode(label) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TreeNode failed: {ex.Message}"); return false; }
        }

        public static void TreePop()
        {
            try { igTreePop(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TreePop failed: {ex.Message}"); }
        }

        // Child windows
        public static bool BeginChild(string str_id, Vector2 size, bool border = false, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        {
            try { return igBeginChild_Str(str_id, new ImVec2(size.X, size.Y), (byte)(border ? 1 : 0), (int)flags) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] BeginChild failed: {ex.Message}"); return false; }
        }

        public static bool BeginChild(uint id, Vector2 size, bool border = false, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        {
            try { return igBeginChild_ID(id, new ImVec2(size.X, size.Y), (byte)(border ? 1 : 0), (int)flags) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] BeginChild (ID) failed: {ex.Message}"); return false; }
        }

        public static void EndChild()
        {
            try { igEndChild(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] EndChild failed: {ex.Message}"); }
        }

        // Window utilities
        public static void SetNextWindowPos(Vector2 pos, int cond = 0, Vector2 pivot = default)
        {
            try { igSetNextWindowPos(new ImVec2(pos.X, pos.Y), cond, new ImVec2(pivot.X, pivot.Y)); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SetNextWindowPos failed: {ex.Message}"); }
        }

        public static void SetNextWindowSize(Vector2 size, int cond = 0)
        {
            try { igSetNextWindowSize(new ImVec2(size.X, size.Y), cond); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SetNextWindowSize failed: {ex.Message}"); }
        }

        public static Vector2 GetWindowPos()
        {
            try
            {
                igGetWindowPos(out ImVec2 pos);
                return new Vector2(pos.x, pos.y);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] GetWindowPos failed: {ex.Message}");
                return Vector2.Zero;
            }
        }

        public static float GetWindowWidth()
        {
            try { return igGetWindowWidth(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetWindowWidth failed: {ex.Message}"); return 0; }
        }

        public static float GetWindowHeight()
        {
            try { return igGetWindowHeight(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetWindowHeight failed: {ex.Message}"); return 0; }
        }

        public static Vector2 GetContentRegionAvail()
        {
            try
            {
                igGetContentRegionAvail(out ImVec2 avail);
                return new Vector2(avail.x, avail.y);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] GetContentRegionAvail failed: {ex.Message}");
                return Vector2.Zero;
            }
        }

        public static Vector2 GetCursorPos()
        {
            try
            {
                igGetCursorPos(out ImVec2 pos);
                return new Vector2(pos.x, pos.y);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] GetCursorPos failed: {ex.Message}");
                return Vector2.Zero;
            }
        }

        public static float GetCursorPosX()
        {
            try { return igGetCursorPosX(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetCursorPosX failed: {ex.Message}"); return 0; }
        }

        public static float GetCursorPosY()
        {
            try { return igGetCursorPosY(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetCursorPosY failed: {ex.Message}"); return 0; }
        }

        public static float GetTextLineHeight()
        {
            try { return igGetTextLineHeight(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetTextLineHeight failed: {ex.Message}"); return 0; }
        }

        // Item/Widgets utilities
        public static bool IsItemHovered()
        {
            try { return igIsItemHovered(0) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] IsItemHovered failed: {ex.Message}"); return false; }
        }

        public static void PushItemWidth(float item_width)
        {
            try { igPushItemWidth(item_width); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] PushItemWidth failed: {ex.Message}"); }
        }

        public static void PopItemWidth()
        {
            try { igPopItemWidth(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] PopItemWidth failed: {ex.Message}"); }
        }

        // Tooltips
        public static void SetTooltip(string fmt)
        {
            try { igSetTooltip(fmt); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SetTooltip failed: {ex.Message}"); }
        }

        public static void TextUnformatted(string text)
            => igTextUnformatted(text, IntPtr.Zero);

        public static bool CollapsingHeader(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        {
            try
            {
                return igCollapsingHeader_TreeNodeFlags(label, (int)flags) != 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] CollapsingHeader(label,flags) failed: {ex.Message}");
                return false;
            }
        }

        public static bool CollapsingHeader(string label, ref bool open, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        {
            try
            {
                return igCollapsingHeader_BoolPtr(label, ref open, (int)flags) != 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] CollapsingHeader(label,ref open,flags) failed: {ex.Message}");
                return false;
            }
        }

        public static bool TreeNodeEx(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        {
            try { return igTreeNodeEx_Str(label, (int)flags) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TreeNodeEx failed: {ex.Message}"); return false; }
        }

        // Columns
        public static void Columns(int count = 1, string id = null, bool border = true)
        {
            try { igColumns(count, id, (byte)(border ? 1 : 0)); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Columns failed: {ex.Message}"); }
        }

        public static void NextColumn()
        {
            try { igNextColumn(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] NextColumn failed: {ex.Message}"); }
        }

        // Tab bars
        public static bool BeginTabBar(string str_id, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
        {
            try { return igBeginTabBar(str_id, (int)flags) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] BeginTabBar failed: {ex.Message}"); return false; }
        }

        public static void EndTabBar()
        {
            try { igEndTabBar(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] EndTabBar failed: {ex.Message}"); }
        }

        public static bool BeginTabItem(string label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
        {
            try { return igBeginTabItem(label, IntPtr.Zero, (int)flags) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] BeginTabItem failed: {ex.Message}"); return false; }
        }

        public static void EndTabItem()
        {
            try { igEndTabItem(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] EndTabItem failed: {ex.Message}"); }
        }

        // Drag controls
        public static bool DragFloat(string label, ref float v, float v_speed = 1.0f, float v_min = 0.0f, float v_max = 0.0f, string format = "%.3f")
        {
            try { return igDragFloat(label, ref v, v_speed, v_min, v_max, format, 0) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] DragFloat failed: {ex.Message}"); return false; }
        }

        public static bool DragFloat2(string label, ref float v, float v_speed = 1.0f, float v_min = 0.0f, float v_max = 0.0f, string format = "%.3f")
        {
            try { return igDragFloat2(label, ref v, v_speed, v_min, v_max, format, 0) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] DragFloat2 failed: {ex.Message}"); return false; }
        }

        // Progress bar
        public static void ProgressBar(float fraction, Vector2 size = default, string overlay = null)
        {
            try { igProgressBar(fraction, new ImVec2(size.X, size.Y), overlay); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] ProgressBar failed: {ex.Message}"); }
        }

        // Multi-line text input
        public static bool InputTextMultiline(string label, ref string text, uint maxLength, Vector2 size, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        {
            try
            {
                var buffer = new byte[maxLength];
                var textBytes = Encoding.UTF8.GetBytes(text ?? "");
                var copyLength = Math.Min(textBytes.Length, (int)maxLength - 1);
                Array.Copy(textBytes, buffer, copyLength);

                if (igInputTextMultiline(label, buffer, maxLength, new ImVec2(size.X, size.Y), (int)flags, IntPtr.Zero, IntPtr.Zero) != 0)
                {
                    var nullIndex = Array.IndexOf(buffer, (byte)0);
                    if (nullIndex >= 0)
                        text = Encoding.UTF8.GetString(buffer, 0, nullIndex);
                    else
                        text = Encoding.UTF8.GetString(buffer);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] InputTextMultiline failed: {ex.Message}");
                return false;
            }
        }

        // Selectables
        public static bool Selectable(string label, bool selected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None, Vector2 size = default)
        {
            try { return igSelectable_Bool(label, (byte)(selected ? 1 : 0), (int)flags, new ImVec2(size.X, size.Y)) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Selectable failed: {ex.Message}"); return false; }
        }

        // Popups and menus
        public static bool BeginPopupContextItem(string str_id = null, int popup_flags = 1)
        {
            try { return igBeginPopupContextItem(str_id, popup_flags) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] BeginPopupContextItem failed: {ex.Message}"); return false; }
        }

        public static void EndPopup()
        {
            try { igEndPopup(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] EndPopup failed: {ex.Message}"); }
        }

        public static bool MenuItem(string label, string shortcut = null, bool selected = false, bool enabled = true)
        {
            try { return igMenuItem_Bool(label, shortcut, (byte)(selected ? 1 : 0), (byte)(enabled ? 1 : 0)) != 0; }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] MenuItem failed: {ex.Message}"); return false; }
        }
        /// <summary>
        /// Create a sub-menu entry. Returns true if open (you must call EndMenu() if true).
        /// </summary>
        public static bool BeginMenu(string label, bool enabled = true)
        {
            try { return igBeginMenu(label, enabled ? (byte)1 : (byte)0) != 0; }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] BeginMenu failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Close a submenu started with BeginMenu.</summary>
        public static void EndMenu()
        {
            try { igEndMenu(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] EndMenu failed: {ex.Message}"); }
        }

        /// <summary>
        /// Call at the top of a window to layout menus (must be paired with EndMenuBar).
        /// </summary>
        public static bool BeginMenuBar()
        {
            try { return igBeginMenuBar() != 0; }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] BeginMenuBar failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>End a menu bar started with BeginMenuBar.</summary>
        public static void EndMenuBar()
        {
            try { igEndMenuBar(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] EndMenuBar failed: {ex.Message}"); }
        }


        // Layout
        public static void Indent(float indent_w = 0.0f)
        {
            try { igIndent(indent_w); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Indent failed: {ex.Message}"); }
        }

        public static void Unindent(float indent_w = 0.0f)
        {
            try { igUnindent(indent_w); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Unindent failed: {ex.Message}"); }
        }

        public static void SetCursorPos(Vector2 local_pos)
        {
            try { igSetCursorPos(new ImVec2(local_pos.X, local_pos.Y)); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SetCursorPos failed: {ex.Message}"); }
        }

        public static void SetCursorPosX(float local_x)
        {
            try { igSetCursorPosX(local_x); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SetCursorPosX failed: {ex.Message}"); }
        }

        public static void PushID(int int_id)
        {
            try { igPushID_Int(int_id); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] PushID(int) failed: {ex.Message}"); }
        }

        public static void PushID(string str_id)
        {
            try { igPushID_Str(str_id); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] PushID(string) failed: {ex.Message}"); }
        }

        public static void PopID()
        {
            try { igPopID(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] PopID failed: {ex.Message}"); }
        }

        /// <summary>
        /// Switch to the specified font (returned by AddFontXXX).  
        /// Pass the raw ImFont* pointer as an IntPtr.
        /// </summary>
        public static void PushFont(IntPtr font)
        {
            try { igPushFont(font); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] PushFont failed: {ex.Message}"); }
        }

        /// <summary>Revert to the previous font on the stack.</summary>
        public static void PopFont()
        {
            try { igPopFont(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] PopFont failed: {ex.Message}"); }
        }

        public static void FontAtlasUpdateSourcesPointers(IntPtr atlas)
        {
            try { igImFontAtlasUpdateSourcesPointers(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasUpdateSourcesPointers failed: {ex.Message}"); }
        }

        public static void FontAtlasBuildInit(IntPtr atlas)
        {
            try { igImFontAtlasBuildInit(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasBuildInit failed: {ex.Message}"); }
        }

        public static void FontAtlasBuildSetupFont(IntPtr atlas, IntPtr font, IntPtr src, float ascent, float descent)
        {
            try { igImFontAtlasBuildSetupFont(atlas, font, src, ascent, descent); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasBuildSetupFont failed: {ex.Message}"); }
        }

        public static void FontAtlasBuildPackCustomRects(IntPtr atlas, IntPtr stbrpContextOpaque)
        {
            try { igImFontAtlasBuildPackCustomRects(atlas, stbrpContextOpaque); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasBuildPackCustomRects failed: {ex.Message}"); }
        }

        public static void FontAtlasBuildFinish(IntPtr atlas)
        {
            try { igImFontAtlasBuildFinish(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasBuildFinish failed: {ex.Message}"); }
        }

        public static void FontAtlasBuildRender8bppRectFromString(
            IntPtr atlas, int x, int y, int w, int h, string str, byte markerChar, byte markerPixelValue)
        {
            try { igImFontAtlasBuildRender8bppRectFromString(atlas, x, y, w, h, str, markerChar, markerPixelValue); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasBuildRender8bppRectFromString failed: {ex.Message}"); }
        }

        public static void FontAtlasBuildRender32bppRectFromString(
            IntPtr atlas, int x, int y, int w, int h, string str, byte markerChar, uint markerPixelValue)
        {
            try { igImFontAtlasBuildRender32bppRectFromString(atlas, x, y, w, h, str, markerChar, markerPixelValue); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasBuildRender32bppRectFromString failed: {ex.Message}"); }
        }

        public static void FontAtlasBuildMultiplyCalcLookupTable(byte[] outTable, float multiplyFactor)
        {
            if (outTable == null || outTable.Length != 256)
                throw new ArgumentException("outTable must be a byte[256] array");
            try { igImFontAtlasBuildMultiplyCalcLookupTable(outTable, multiplyFactor); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasBuildMultiplyCalcLookupTable failed: {ex.Message}"); }
        }

        public static void FontAtlasBuildMultiplyRectAlpha8(
            byte[] table, IntPtr pixels, int x, int y, int w, int h, int stride)
        {
            if (table == null || table.Length != 256)
                throw new ArgumentException("table must be a byte[256] array");
            try { igImFontAtlasBuildMultiplyRectAlpha8(table, pixels, x, y, w, h, stride); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasBuildMultiplyRectAlpha8 failed: {ex.Message}"); }
        }

        public static void FontAtlasBuildGetOversampleFactors(
            IntPtr src, out int oversampleH, out int oversampleV)
        {
            try { igImFontAtlasBuildGetOversampleFactors(src, out oversampleH, out oversampleV); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] FontAtlasBuildGetOversampleFactors failed: {ex.Message}");
                oversampleH = oversampleV = 0;
            }
        }

        public static bool FontAtlasGetMouseCursorTexData(
            IntPtr atlas,
            int cursorType,
            out ImVec2 offset,
            out ImVec2 size,
            ImVec2[] uvBorder,
            ImVec2[] uvFill)
        {
            if (uvBorder == null || uvBorder.Length != 2)
                throw new ArgumentException("uvBorder must be a ImVec2[2] array");
            if (uvFill == null || uvFill.Length != 2)
                throw new ArgumentException("uvFill must be a ImVec2[2] array");
            try
            {
                return igImFontAtlasGetMouseCursorTexData(
                    atlas, cursorType, out offset, out size, uvBorder, uvFill
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] FontAtlasGetMouseCursorTexData failed: {ex.Message}");
                offset = default;
                size = default;
                return false;
            }
        }

        public static IntPtr GetFont()
        {
            try { return igGetFont(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetFont failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static float GetFontSize()
        {
            try { return igGetFontSize(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetFontSize failed: {ex.Message}"); return 0f; }
        }

        public static ImVec2 GetFontTexUvWhitePixel()
        {
            try
            {
                igGetFontTexUvWhitePixel(out ImVec2 pOut);
                return pOut;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] GetFontTexUvWhitePixel failed: {ex.Message}");
                return default;
            }
        }

        public static void SetWindowFontScale(float scale)
        {
            try { igSetWindowFontScale(scale); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SetWindowFontScale failed: {ex.Message}"); }
        }

        public static unsafe void DrawListAddText(
            IntPtr drawList, IntPtr font, float fontSize, ImVec2 pos, uint color,
            string text, float wrapWidth, ImVec4? cpuFineClipRect = null)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            // Convert .NET string to UTF-8 byte array
            byte[] utf8 = Encoding.UTF8.GetBytes(text);
            fixed (byte* textBegin = utf8)
            {
                byte* textEnd = textBegin + utf8.Length; // one-past-the-end

                if (cpuFineClipRect.HasValue)
                {
                    ImVec4 rect = cpuFineClipRect.Value;
                    ImDrawList_AddText_FontPtr(drawList, font, fontSize, pos, color, textBegin, textEnd, wrapWidth, &rect);
                }
                else
                {
                    ImDrawList_AddText_FontPtr(drawList, font, fontSize, pos, color, textBegin, textEnd, wrapWidth, null);
                }
            }
        }


        // --- FONT CONFIG/BUILDER/ATLAS HELPERS ---
        public static IntPtr CreateFontConfig()
        {
            try { return ImFontConfig_ImFontConfig(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] CreateFontConfig failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static void DestroyFontConfig(IntPtr config)
        {
            try { ImFontConfig_destroy(config); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] DestroyFontConfig failed: {ex.Message}"); }
        }

        public static IntPtr CreateGlyphRangesBuilder()
        {
            try { return ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] CreateGlyphRangesBuilder failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static void DestroyGlyphRangesBuilder(IntPtr builder)
        {
            try { ImFontGlyphRangesBuilder_destroy(builder); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] DestroyGlyphRangesBuilder failed: {ex.Message}"); }
        }

        public static void GlyphRangesBuilderClear(IntPtr builder)
        {
            try { ImFontGlyphRangesBuilder_Clear(builder); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GlyphRangesBuilderClear failed: {ex.Message}"); }
        }

        public static bool GlyphRangesBuilderGetBit(IntPtr builder, nuint n)
        {
            try { return ImFontGlyphRangesBuilder_GetBit(builder, n); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GlyphRangesBuilderGetBit failed: {ex.Message}"); return false; }
        }

        public static void GlyphRangesBuilderSetBit(IntPtr builder, nuint n)
        {
            try { ImFontGlyphRangesBuilder_SetBit(builder, n); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GlyphRangesBuilderSetBit failed: {ex.Message}"); }
        }

        public static void GlyphRangesBuilderAddChar(IntPtr builder, ushort c)
        {
            try { ImFontGlyphRangesBuilder_AddChar(builder, c); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GlyphRangesBuilderAddChar failed: {ex.Message}"); }
        }

        public static void GlyphRangesBuilderAddText(IntPtr builder, string text, string textEnd = null)
        {
            try { ImFontGlyphRangesBuilder_AddText(builder, text, textEnd); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GlyphRangesBuilderAddText failed: {ex.Message}"); }
        }

        public static void GlyphRangesBuilderAddRanges(IntPtr builder, IntPtr ranges)
        {
            try { ImFontGlyphRangesBuilder_AddRanges(builder, ranges); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GlyphRangesBuilderAddRanges failed: {ex.Message}"); }
        }

        public static void GlyphRangesBuilderBuildRanges(IntPtr builder, IntPtr outRanges)
        {
            try { ImFontGlyphRangesBuilder_BuildRanges(builder, outRanges); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GlyphRangesBuilderBuildRanges failed: {ex.Message}"); }
        }

        public static IntPtr CreateFontAtlasCustomRect()
        {
            try { return ImFontAtlasCustomRect_ImFontAtlasCustomRect(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] CreateFontAtlasCustomRect failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static void DestroyFontAtlasCustomRect(IntPtr rect)
        {
            try { ImFontAtlasCustomRect_destroy(rect); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] DestroyFontAtlasCustomRect failed: {ex.Message}"); }
        }

        public static bool FontAtlasCustomRectIsPacked(IntPtr rect)
        {
            try { return ImFontAtlasCustomRect_IsPacked(rect); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasCustomRectIsPacked failed: {ex.Message}"); return false; }
        }

        public static IntPtr CreateFontAtlas()
        {
            try { return ImFontAtlas_ImFontAtlas(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] CreateFontAtlas failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static void DestroyFontAtlas(IntPtr atlas)
        {
            try { ImFontAtlas_destroy(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] DestroyFontAtlas failed: {ex.Message}"); }
        }

        // === FONT ATLAS ADD/REMOVE/MANIPULATION ===

        public static IntPtr FontAtlasAddFont(IntPtr atlas, IntPtr fontCfg)
        {
            try { return ImFontAtlas_AddFont(atlas, fontCfg); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasAddFont failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasAddFontDefault(IntPtr atlas, IntPtr fontCfg)
        {
            try { return ImFontAtlas_AddFontDefault(atlas, fontCfg); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasAddFontDefault failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasAddFontFromFileTTF(IntPtr atlas, string filename, float sizePixels, IntPtr fontCfg, IntPtr glyphRanges)
        {
            try { return ImFontAtlas_AddFontFromFileTTF(atlas, filename, sizePixels, fontCfg, glyphRanges); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasAddFontFromFileTTF failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasAddFontFromMemoryTTF(IntPtr atlas, IntPtr fontData, int fontDataSize, float sizePixels, IntPtr fontCfg, IntPtr glyphRanges)
        {
            try { return ImFontAtlas_AddFontFromMemoryTTF(atlas, fontData, fontDataSize, sizePixels, fontCfg, glyphRanges); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasAddFontFromMemoryTTF failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasAddFontFromMemoryCompressedTTF(IntPtr atlas, IntPtr compressedFontData, int dataSize, float sizePixels, IntPtr fontCfg, IntPtr glyphRanges)
        {
            try { return ImFontAtlas_AddFontFromMemoryCompressedTTF(atlas, compressedFontData, dataSize, sizePixels, fontCfg, glyphRanges); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasAddFontFromMemoryCompressedTTF failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasAddFontFromMemoryCompressedBase85TTF(IntPtr atlas, string compressedFontDataBase85, float sizePixels, IntPtr fontCfg, IntPtr glyphRanges)
        {
            try { return ImFontAtlas_AddFontFromMemoryCompressedBase85TTF(atlas, compressedFontDataBase85, sizePixels, fontCfg, glyphRanges); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasAddFontFromMemoryCompressedBase85TTF failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static void FontAtlasClearInputData(IntPtr atlas)
        {
            try { ImFontAtlas_ClearInputData(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasClearInputData failed: {ex.Message}"); }
        }

        public static void FontAtlasClearFonts(IntPtr atlas)
        {
            try { ImFontAtlas_ClearFonts(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasClearFonts failed: {ex.Message}"); }
        }

        public static void FontAtlasClearTexData(IntPtr atlas)
        {
            try { ImFontAtlas_ClearTexData(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasClearTexData failed: {ex.Message}"); }
        }

        public static void FontAtlasClear(IntPtr atlas)
        {
            try { ImFontAtlas_Clear(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasClear failed: {ex.Message}"); }
        }

        public static bool FontAtlasBuild(IntPtr atlas)
        {
            try { return ImFontAtlas_Build(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasBuild failed: {ex.Message}"); return false; }
        }

        // === FONT ATLAS TEXTURE DATA ===

        public static void FontAtlasGetTexDataAsAlpha8(IntPtr atlas, out IntPtr outPixels, out int width, out int height, out int bytesPerPixel)
        {
            try { ImFontAtlas_GetTexDataAsAlpha8(atlas, out outPixels, out width, out height, out bytesPerPixel); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] FontAtlasGetTexDataAsAlpha8 failed: {ex.Message}");
                outPixels = IntPtr.Zero; width = height = bytesPerPixel = 0;
            }
        }

        public static void FontAtlasGetTexDataAsRGBA32(IntPtr atlas, out IntPtr outPixels, out int width, out int height, out int bytesPerPixel)
        {
            try { ImFontAtlas_GetTexDataAsRGBA32(atlas, out outPixels, out width, out height, out bytesPerPixel); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] FontAtlasGetTexDataAsRGBA32 failed: {ex.Message}");
                outPixels = IntPtr.Zero; width = height = bytesPerPixel = 0;
            }
        }

        public static bool FontAtlasIsBuilt(IntPtr atlas)
        {
            try { return ImFontAtlas_IsBuilt(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasIsBuilt failed: {ex.Message}"); return false; }
        }

        public static void FontAtlasSetTexID(IntPtr atlas, IntPtr texID)
        {
            try { ImFontAtlas_SetTexID(atlas, texID); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasSetTexID failed: {ex.Message}"); }
        }

        // === FONT ATLAS GLYPH RANGES ===

        public static IntPtr FontAtlasGetGlyphRangesDefault(IntPtr atlas)
        {
            try { return ImFontAtlas_GetGlyphRangesDefault(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasGetGlyphRangesDefault failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasGetGlyphRangesGreek(IntPtr atlas)
        {
            try { return ImFontAtlas_GetGlyphRangesGreek(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasGetGlyphRangesGreek failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasGetGlyphRangesKorean(IntPtr atlas)
        {
            try { return ImFontAtlas_GetGlyphRangesKorean(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasGetGlyphRangesKorean failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasGetGlyphRangesJapanese(IntPtr atlas)
        {
            try { return ImFontAtlas_GetGlyphRangesJapanese(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasGetGlyphRangesJapanese failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasGetGlyphRangesChineseFull(IntPtr atlas)
        {
            try { return ImFontAtlas_GetGlyphRangesChineseFull(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasGetGlyphRangesChineseFull failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasGetGlyphRangesChineseSimplifiedCommon(IntPtr atlas)
        {
            try { return ImFontAtlas_GetGlyphRangesChineseSimplifiedCommon(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasGetGlyphRangesChineseSimplifiedCommon failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasGetGlyphRangesCyrillic(IntPtr atlas)
        {
            try { return ImFontAtlas_GetGlyphRangesCyrillic(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasGetGlyphRangesCyrillic failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasGetGlyphRangesThai(IntPtr atlas)
        {
            try { return ImFontAtlas_GetGlyphRangesThai(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasGetGlyphRangesThai failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static IntPtr FontAtlasGetGlyphRangesVietnamese(IntPtr atlas)
        {
            try { return ImFontAtlas_GetGlyphRangesVietnamese(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasGetGlyphRangesVietnamese failed: {ex.Message}"); return IntPtr.Zero; }
        }

        // === CUSTOM RECT ===

        public static int FontAtlasAddCustomRectRegular(IntPtr atlas, int width, int height)
        {
            try { return ImFontAtlas_AddCustomRectRegular(atlas, width, height); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasAddCustomRectRegular failed: {ex.Message}"); return -1; }
        }

        public static int FontAtlasAddCustomRectFontGlyph(IntPtr atlas, IntPtr font, ushort id, int width, int height, float advanceX, ImVec2 offset)
        {
            try { return ImFontAtlas_AddCustomRectFontGlyph(atlas, font, id, width, height, advanceX, offset); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasAddCustomRectFontGlyph failed: {ex.Message}"); return -1; }
        }

        public static IntPtr FontAtlasGetCustomRectByIndex(IntPtr atlas, int index)
        {
            try { return ImFontAtlas_GetCustomRectByIndex(atlas, index); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] FontAtlasGetCustomRectByIndex failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static void FontAtlasCalcCustomRectUV(IntPtr atlas, IntPtr rect, out ImVec2 uvMin, out ImVec2 uvMax)
        {
            try { ImFontAtlas_CalcCustomRectUV(atlas, rect, out uvMin, out uvMax); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] FontAtlasCalcCustomRectUV failed: {ex.Message}");
                uvMin = uvMax = default;
            }
        }

        /// <summary>
        /// Create a new ImGuiContext, optionally sharing a font atlas. Returns the new context pointer.
        /// </summary>
        public static IntPtr CreateContext(IntPtr sharedFontAtlas = default)
        {
            try { return ImGuiContext_ImGuiContext(sharedFontAtlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] CreateContext failed: {ex.Message}"); return IntPtr.Zero; }
        }

        /// <summary>
        /// Calculate the font size for a given ImGuiWindow (pass window native pointer).
        /// </summary>
        public static float CalcWindowFontSize(IntPtr window)
        {
            try { return ImGuiWindow_CalcFontSize(window); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] CalcWindowFontSize failed: {ex.Message}"); return 0f; }
        }

        /// <summary>
        /// Set the current ImFont to use (for advanced usage).
        /// </summary>
        public static void SetCurrentFont(IntPtr font)
        {
            try { igSetCurrentFont(font); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SetCurrentFont failed: {ex.Message}"); }
        }

        /// <summary>
        /// Get the default font pointer (ImFont*).
        /// </summary>
        public static IntPtr GetDefaultFont()
        {
            try { return igGetDefaultFont(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetDefaultFont failed: {ex.Message}"); return IntPtr.Zero; }
        }

        /// <summary>
        /// Push the password font onto the font stack (for password fields).
        /// </summary>
        public static void PushPasswordFont()
        {
            try { igPushPasswordFont(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] PushPasswordFont failed: {ex.Message}"); }
        }

        /// <summary>
        /// Show the font atlas debug UI for a given atlas pointer.
        /// </summary>
        public static void ShowFontAtlas(IntPtr atlas)
        {
            try { igShowFontAtlas(atlas); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] ShowFontAtlas failed: {ex.Message}"); }
        }

        // Dockspace
        public static void DockSpaceOverViewport(ImGuiDockNodeFlags flags = ImGuiDockNodeFlags.None)
        {
            try
            {
                igDockSpaceOverViewport(0, IntPtr.Zero, (int)flags, IntPtr.Zero);
            }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] DockSpaceOverViewport failed: {ex.Message}"); }
        }

        public static unsafe Vector2 CalcTextSize(
        string text,
        bool hideTextAfterDoubleHash = false,
        float wrapWidth = -1.0f)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            // Convert .NET string to UTF-8 bytes
            byte[] utf8 = Encoding.UTF8.GetBytes(text);

            fixed (byte* textBegin = utf8)
            {
                byte* textEnd = textBegin + utf8.Length;
                ImVec2 size;
                igCalcTextSize(out size, textBegin, textEnd, hideTextAfterDoubleHash, wrapWidth);
                return new Vector2(size.x, size.y);
            }
        }

        /// <summary>
        /// Render text with a bullet. Supports formatting via string.Format, e.g. ImGui.BulletText("FPS: {0}", fps)
        /// </summary>
        public static void BulletText(string text, params object[] args)
        {
            try
            {
                string formatted = (args != null && args.Length > 0) ? string.Format(text, args) : text;
                igBulletText(formatted);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] BulletText failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get Dear ImGui version string.
        /// </summary>
        public static string GetVersion()
        {
            try
            {
                IntPtr ptr = igGetVersion();
                return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] GetVersion failed: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Set the width of a column at the given index.
        /// </summary>
        public static void SetColumnWidth(int columnIndex, float width)
        {
            try { igSetColumnWidth(columnIndex, width); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SetColumnWidth failed: {ex.Message}"); }
        }

        /// <summary>
        /// Show the "About" window. Pass a ref bool controlling the open/close state.
        /// </summary>
        public static void ShowAboutWindow(ref bool open)
        {
            try { igShowAboutWindow(ref open); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] ShowAboutWindow failed: {ex.Message}"); }
        }

        /// <summary>
        /// Show the Debug Log window. Pass a ref bool controlling the open/close state.
        /// </summary>
        public static void ShowDebugLogWindow(ref bool open)
        {
            try { igShowDebugLogWindow(ref open); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] ShowDebugLogWindow failed: {ex.Message}"); }
        }

        /// <summary>
        /// Show the style editor for the given ImGuiStyle pointer.
        /// </summary>
        public static void ShowStyleEditor(IntPtr style)
        {
            try { igShowStyleEditor(style); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] ShowStyleEditor failed: {ex.Message}"); }
        }

        /// <summary>
        /// Show the metrics window; pass a bool that gets set to false when closed.
        /// </summary>
        public static void ShowMetricsWindow(ref bool open)
        {
            try { igShowMetricsWindow(ref open); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] ShowMetricsWindow failed: {ex.Message}"); }
        }

        /// <summary>
        /// Show the ID stack inspection tool window; pass a bool that gets set to false when closed.
        /// </summary>
        public static void ShowIDStackToolWindow(ref bool open)
        {
            try { igShowIDStackToolWindow(ref open); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] ShowIDStackToolWindow failed: {ex.Message}"); }
        }

        /// <summary>
        /// Show the full ImGui demo window; pass a bool that gets set to false when closed.
        /// </summary>
        public static void ShowDemoWindow(ref bool open)
        {
            try { igShowDemoWindow(ref open); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] ShowDemoWindow failed: {ex.Message}"); }
        }

        /// <summary>
        /// Get a pointer to the native ImGuiStyle struct.
        /// </summary>
        public static IntPtr GetStyle()
        {
            try { return igGetStyle(); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] GetStylePtr failed: {ex.Message}");
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Returns the horizontal scrolling offset for the current window.
        /// </summary>
        public static float GetScrollX()
        {
            try { return igGetScrollX(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetScrollX failed: {ex.Message}"); return 0; }
        }

        /// <summary>
        /// Returns the vertical scrolling offset for the current window.
        /// </summary>
        public static float GetScrollY()
        {
            try { return igGetScrollY(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetScrollY failed: {ex.Message}"); return 0; }
        }

        /// <summary>
        /// Returns the maximum horizontal scroll value for the current window.
        /// </summary>
        public static float GetScrollMaxX()
        {
            try { return igGetScrollMaxX(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetScrollMaxX failed: {ex.Message}"); return 0; }
        }

        /// <summary>
        /// Returns the maximum vertical scroll value for the current window.
        /// </summary>
        public static float GetScrollMaxY()
        {
            try { return igGetScrollMaxY(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetScrollMaxY failed: {ex.Message}"); return 0; }
        }

        public static bool BeginComboPopup(uint popupId, ImRect bb, int flags = 0)
        {
            try { return igBeginComboPopup(popupId, bb, flags); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] BeginComboPopup failed: {ex.Message}"); return false; }
        }

        public static bool BeginComboPreview()
        {
            try { return igBeginComboPreview(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] BeginComboPreview failed: {ex.Message}"); return false; }
        }

        public static void EndComboPreview()
        {
            try { igEndComboPreview(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] EndComboPreview failed: {ex.Message}"); }
        }

        public static bool BeginCombo(string label, string previewValue, int flags = 0)
        {
            try { return igBeginCombo(label, previewValue, flags); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] BeginCombo failed: {ex.Message}"); return false; }
        }

        public static void EndCombo()
        {
            try { igEndCombo(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] EndCombo failed: {ex.Message}"); }
        }

        public static bool Combo(string label, ref int currentItem, string[] items, int popupMaxHeightInItems = -1)
        {
            if (items == null || items.Length == 0) return false;
            try { return igCombo_Str_arr(label, ref currentItem, items, items.Length, popupMaxHeightInItems); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Combo(string[]) failed: {ex.Message}"); return false; }
        }

        public static bool Combo(string label, ref int currentItem, string itemsSeparatedByZeros, int popupMaxHeightInItems = -1)
        {
            try { return igCombo_Str(label, ref currentItem, itemsSeparatedByZeros, popupMaxHeightInItems); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Combo(itemsSeparatedByZeros) failed: {ex.Message}"); return false; }
        }

        public static bool Combo(string label, ref int currentItem, ComboItemGetter getter, IntPtr userData, int itemsCount, int popupMaxHeightInItems = -1)
        {
            try { return igCombo_FnStrPtr(label, ref currentItem, getter, userData, itemsCount, popupMaxHeightInItems); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] Combo(getter) failed: {ex.Message}"); return false; }
        }

        /// <summary>
        /// Returns a pointer to the current window's ImDrawList (for custom rendering).
        /// </summary>
        public static IntPtr GetWindowDrawList()
        {
            try { return igGetWindowDrawList(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] GetWindowDrawList failed: {ex.Message}"); return IntPtr.Zero; }
        }

        /// <summary>
        /// Returns true if the window is hovered.
        /// </summary>
        public static bool IsWindowHovered(int flags = 0)
        {
            try { return igIsWindowHovered(flags); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] IsWindowHovered failed: {ex.Message}"); return false; }
        }

        /// <summary>
        /// Returns the current mouse position (global coordinates).
        /// </summary>
        public static Vector2 GetMousePos()
        {
            try
            {
                igGetMousePos(out ImVec2 pos);
                return new Vector2(pos.x, pos.y);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] GetMousePos failed: {ex.Message}");
                return Vector2.Zero;
            }
        }

        /// <summary>
        /// Returns the mouse position at the time the last popup was opened.
        /// </summary>
        public static Vector2 GetMousePosOnOpeningCurrentPopup()
        {
            try
            {
                igGetMousePosOnOpeningCurrentPopup(out ImVec2 pos);
                return new Vector2(pos.x, pos.y);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] GetMousePosOnOpeningCurrentPopup failed: {ex.Message}");
                return Vector2.Zero;
            }
        }
        /// <summary>
        /// Returns the cursor position in screen coordinates.
        /// </summary>
        public static Vector2 GetCursorScreenPos()
        {
            try
            {
                igGetCursorScreenPos(out ImVec2 pos);
                return new Vector2(pos.x, pos.y);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] GetCursorScreenPos failed: {ex.Message}");
                return Vector2.Zero;
            }
        }

        /// <summary>
        /// Sets the cursor position in screen coordinates.
        /// </summary>
        public static void SetCursorScreenPos(Vector2 pos)
        {
            try { igSetCursorScreenPos(new ImVec2(pos.X, pos.Y)); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SetCursorScreenPos failed: {ex.Message}"); }
        }

        public static void DrawListAddRectFilled(
            IntPtr drawList,
            Vector2 pMin,
            Vector2 pMax,
            uint col,
            float rounding = 0.0f,
            int flags = 0)
        {
            igDrawList_AddRectFilled(
                drawList,
                new ImVec2(pMin.X, pMin.Y),
                new ImVec2(pMax.X, pMax.Y),
                col,
                rounding,
                flags
            );
        }

        public static bool BeginTable(string strId, int columns, ImGuiTableFlags flags = 0, Vector2 outerSize = default, float innerWidth = 0.0f)
        {
            try { return igBeginTable(strId, columns, flags, new ImVec2(outerSize.X, outerSize.Y), innerWidth); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] BeginTable failed: {ex.Message}"); return false; }
        }

        public static void EndTable()
        {
            try { igEndTable(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] EndTable failed: {ex.Message}"); }
        }

        public static void TableNextRow(ImGuiTableRowFlags rowFlags = 0, float minRowHeight = 0.0f)
        {
            try { igTableNextRow(rowFlags, minRowHeight); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableNextRow failed: {ex.Message}"); }
        }

        public static bool TableNextColumn()
        {
            try { return igTableNextColumn(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableNextColumn failed: {ex.Message}"); return false; }
        }

        public static bool TableSetColumnIndex(int columnN)
        {
            try { return igTableSetColumnIndex(columnN); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableSetColumnIndex failed: {ex.Message}"); return false; }
        }

        public static void TableSetupColumn(string label, ImGuiTableColumnFlags flags = 0, float initWidthOrWeight = 0.0f, uint userId = 0)
        {
            try { igTableSetupColumn(label, flags, initWidthOrWeight, userId); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableSetupColumn failed: {ex.Message}"); }
        }

        public static void TableSetupScrollFreeze(int cols, int rows)
        {
            try { igTableSetupScrollFreeze(cols, rows); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableSetupScrollFreeze failed: {ex.Message}"); }
        }

        public static void TableHeader(string label)
        {
            try { igTableHeader(label); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableHeader failed: {ex.Message}"); }
        }

        public static void TableHeadersRow()
        {
            try { igTableHeadersRow(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableHeadersRow failed: {ex.Message}"); }
        }

        public static void TableAngledHeadersRow()
        {
            try { igTableAngledHeadersRow(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableAngledHeadersRow failed: {ex.Message}"); }
        }

        public static IntPtr TableGetSortSpecs()
        {
            try { return igTableGetSortSpecs(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableGetSortSpecs failed: {ex.Message}"); return IntPtr.Zero; }
        }

        public static int TableGetColumnCount()
        {
            try { return igTableGetColumnCount(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableGetColumnCount failed: {ex.Message}"); return 0; }
        }

        public static int TableGetColumnIndex()
        {
            try { return igTableGetColumnIndex(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableGetColumnIndex failed: {ex.Message}"); return 0; }
        }

        public static int TableGetRowIndex()
        {
            try { return igTableGetRowIndex(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableGetRowIndex failed: {ex.Message}"); return 0; }
        }

        public static string TableGetColumnName(int columnN)
        {
            try
            {
                IntPtr ptr = igTableGetColumnName_Int(columnN);
                return Marshal.PtrToStringAnsi(ptr);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] TableGetColumnName failed: {ex.Message}");
                return string.Empty;
            }
        }

        public static ImGuiTableColumnFlags TableGetColumnFlags(int columnN)
        {
            try { return igTableGetColumnFlags(columnN); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableGetColumnFlags failed: {ex.Message}"); return 0; }
        }

        public static void TableSetColumnEnabled(int columnN, bool v)
        {
            try { igTableSetColumnEnabled(columnN, v); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableSetColumnEnabled failed: {ex.Message}"); }
        }

        public static int TableGetHoveredColumn()
        {
            try { return igTableGetHoveredColumn(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableGetHoveredColumn failed: {ex.Message}"); return -1; }
        }

        public static void TableSetBgColor(ImGuiTableBgTarget target, uint color, int columnN = -1)
        {
            try { igTableSetBgColor(target, color, columnN); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] TableSetBgColor failed: {ex.Message}"); }
        }

        public static bool InputTextWithHint(
            string label,
            string hint,
            ref string text,
            uint maxLength = 256,
            ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
            ImGuiInputTextCallback callback = null,
            IntPtr userData = default)
        {
            try
            {
                var buffer = new byte[maxLength];
                var textBytes = Encoding.UTF8.GetBytes(text ?? "");
                var copyLength = Math.Min(textBytes.Length, (int)maxLength - 1);
                Array.Copy(textBytes, buffer, copyLength);

                bool result;
                if (callback != null)
                {
                    result = igInputTextWithHint(label, hint, buffer, (UIntPtr)maxLength, (int)flags, callback, userData) != 0;
                }
                else
                {
                    result = igInputTextWithHint(label, hint, buffer, (UIntPtr)maxLength, (int)flags, null, IntPtr.Zero) != 0;
                }

                if (result)
                {
                    var nullIndex = Array.IndexOf(buffer, (byte)0);
                    text = nullIndex >= 0 ? Encoding.UTF8.GetString(buffer, 0, nullIndex) : Encoding.UTF8.GetString(buffer);
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] InputTextWithHint failed: {ex.Message}");
                return false;
            }
        }
        public static bool InputFloat2(string label, ref Vector2 value, string format = "%.3f", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        {
            float[] vals = { value.X, value.Y };
            bool changed = igInputFloat2(label, vals, format, (int)flags) != 0;
            if (changed)
                value = new Vector2(vals[0], vals[1]);
            return changed;
        }

        public static bool InputFloat3(string label, ref Vector3 value, string format = "%.3f", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        {
            float[] vals = { value.X, value.Y, value.Z };
            bool changed = igInputFloat3(label, vals, format, (int)flags) != 0;
            if (changed)
                value = new Vector3(vals[0], vals[1], vals[2]);
            return changed;
        }

        public static bool InputFloat4(string label, ref Vector4 value, string format = "%.3f", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        {
            float[] vals = { value.X, value.Y, value.Z, value.W };
            bool changed = igInputFloat4(label, vals, format, (int)flags) != 0;
            if (changed)
                value = new Vector4(vals[0], vals[1], vals[2], vals[3]);
            return changed;
        }

        public static void SetNextWindowBgAlpha(float alpha)
        {
            try { igSetNextWindowBgAlpha(alpha); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SetNextWindowBgAlpha failed: {ex.Message}"); }
        }

        public static bool ColorButton(string id, Vector4 col, ImGuiColorEditFlags flags, Vector2 size)
        {
            try
            {
                return igColorButton(
                    id,
                    new ImVec4(col.X, col.Y, col.Z, col.W),
                    (int)flags,
                    new ImVec2(size.X, size.Y)
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] ColorButton failed: {ex.Message}");
                return false;
            }
        }

        public static void SetColorEditOptions(ImGuiColorEditFlags flags)
        {
            try { igSetColorEditOptions((int)flags); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] SetColorEditOptions failed: {ex.Message}"); }
        }

        /// <summary>
        /// Formatted TreeNode: uses C# formatting and calls the simple TreeNode under the hood.
        /// </summary>
        public static bool TreeNode(string id, string fmt, params object[] args)
        {
            try
            {
                string text = string.Format(fmt, args);
                return TreeNode(text);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] TreeNode(fmt) failed: {ex.Message}");
                return false;
            }
        }

        public static Vector4 ColorConvertU32ToFloat4(uint col)
        {
            try
            {
                igColorConvertU32ToFloat4(out ImVec4 c, col);
                return new Vector4(c.x, c.y, c.z, c.w);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] ColorConvertU32ToFloat4 failed: {ex.Message}");
                return Vector4.Zero;
            }
        }

        public static uint ColorConvertFloat4ToU32(Vector4 c)
        {
            try { return igColorConvertFloat4ToU32(new ImVec4(c.X, c.Y, c.Z, c.W)); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] ColorConvertFloat4ToU32 failed: {ex.Message}"); return 0u; }
        }

        public static bool ListBox(string label, ref int currentItem, string[] items, int heightInItems = -1)
        {
            try
            {
                return igListBox_Str_arr(label, ref currentItem, items, items.Length, heightInItems);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] ListBox_str_arr failed: {ex.Message}");
                return false;
            }
        }

        public static bool ListBox(string label, ref int currentItem, ImGuiListBoxItemGetter getter, IntPtr userData, int itemCount, int heightInItems = -1)
        {
            try
            {
                return igListBox_FnStrPtr(label, ref currentItem, getter, userData, itemCount, heightInItems);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] ListBox_fn failed: {ex.Message}");
                return false;
            }
        }

        public static void PlotLines(string label, float[] values, int offset = 0, string overlay = null, float min = float.NaN, float max = float.NaN, Vector2 size = default, int stride = sizeof(float))
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            fixed (float* v = values)
            {
                igPlotLines_FloatPtr(
                    label,
                    v, values.Length, offset,
                    overlay, min, max,
                    new ImVec2(size.X, size.Y),
                    stride
                );
            }
        }

        public static void PlotLines(string label, ImGuiPlotGetter getter, IntPtr data, int count, int offset = 0, string overlay = null, float min = float.NaN, float max = float.NaN, Vector2 size = default)
        {
            try
            {
                igPlotLines_FnFloatPtr(
                    label, getter, data,
                    count, offset,
                    overlay, min, max,
                    new ImVec2(size.X, size.Y)
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] PlotLines_fn failed: {ex.Message}");
            }
        }

        public static void PlotHistogram(string label, float[] values, int offset = 0, string overlay = null, float min = float.NaN, float max = float.NaN, Vector2 size = default, int stride = sizeof(float))
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            fixed (float* v = values)
            {
                igPlotHistogram_FloatPtr(
                    label,
                    v, values.Length, offset,
                    overlay, min, max,
                    new ImVec2(size.X, size.Y),
                    stride
                );
            }
        }

        public static void PlotHistogram(string label, ImGuiPlotGetter getter, IntPtr data, int count, int offset = 0, string overlay = null, float min = float.NaN, float max = float.NaN, Vector2 size = default)
        {
            try
            {
                igPlotHistogram_FnFloatPtr(
                    label, getter, data,
                    count, offset,
                    overlay, min, max,
                    new ImVec2(size.X, size.Y)
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ImGui] PlotHistogram_fn failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Begin a group. All subsequent widgets will be inside this group until EndGroup is called.
        /// </summary>
        public static void BeginGroup()
        {
            try { igBeginGroup(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] BeginGroup failed: {ex.Message}"); }
        }

        /// <summary>
        /// End the current group started by BeginGroup.
        /// </summary>
        public static void EndGroup()
        {
            try { igEndGroup(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ImGui] EndGroup failed: {ex.Message}"); }
        }
    }
}