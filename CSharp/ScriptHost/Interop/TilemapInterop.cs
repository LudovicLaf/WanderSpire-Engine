// TilemapInterop.cs - Tilemap and Tile-related Functionality
using System;
using System.Runtime.InteropServices;

namespace WanderSpire.Scripting
{
    /// <summary>
    /// Tilemap and tile-related functionality interop
    /// </summary>
    public static class TilemapInterop
    {
        private const string DLL = "EngineCore";

        #region Tilemap API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern EntityId Tilemap_Create(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern EntityId Tilemap_CreateLayer(
            IntPtr ctx, EntityId tilemap, [MarshalAs(UnmanagedType.LPStr)] string layerName);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Tilemap_SetTile(
            IntPtr ctx, EntityId tilemapLayer, int tileX, int tileY, int tileId);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Tilemap_GetTile(
            IntPtr ctx, EntityId tilemapLayer, int tileX, int tileY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Tilemap_FloodFill(
            IntPtr ctx, EntityId tilemapLayer, int startX, int startY, int newTileId);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Tilemap_GetBounds(
            IntPtr ctx, EntityId tilemapLayer,
            out int outMinX, out int outMinY, out int outMaxX, out int outMaxY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Tilemap_CountTilesInRegion(
            IntPtr ctx, EntityId tilemapLayer, int minX, int minY, int maxX, int maxY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Tilemap_FindTilePositions(
            IntPtr ctx, EntityId tilemapLayer, int tileId, [Out] int[] outPositions, int maxPositions);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Tilemap_ReplaceTiles(
            IntPtr ctx, EntityId tilemapLayer, int oldTileId, int newTileId,
            int minX, int minY, int maxX, int maxY);

        #endregion

        #region Tile Palette API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePalette_Create(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string paletteName,
            [MarshalAs(UnmanagedType.LPStr)] string atlasPath, int tileWidth, int tileHeight);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilePalette_SetActive(IntPtr ctx, int paletteId);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePalette_GetActive(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePalette_AddTile(
            IntPtr ctx, int paletteId, int tileId,
            [MarshalAs(UnmanagedType.LPStr)] string tileName,
            [MarshalAs(UnmanagedType.LPStr)] string assetPath,
            int atlasX, int atlasY, int walkable, int collisionType);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePalette_GetInfo(
            IntPtr ctx, int paletteId,
            [Out] byte[] outName, int nameBufferSize,
            [Out] byte[] outAtlasPath, int atlasPathBufferSize,
            out int outTileWidth, out int outTileHeight, out int outColumns);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePalette_GetTileCount(IntPtr ctx, int paletteId);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePalette_GetTileInfo(
            IntPtr ctx, int paletteId, int tileIndex, out int outTileId,
            [Out] byte[] outTileName, int nameBufferSize,
            out int outAtlasX, out int outAtlasY, out int outWalkable, out int outCollisionType);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePalette_Load(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string palettePath);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePalette_Save(
            IntPtr ctx, int paletteId, [MarshalAs(UnmanagedType.LPStr)] string palettePath);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePalette_GetSelectedTile(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilePalette_SetSelectedTile(IntPtr ctx, int tileId);

        #endregion

        #region Tile Brush API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileBrush_SetType(IntPtr ctx, int brushType);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileBrush_SetSize(IntPtr ctx, int size);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileBrush_SetBlendMode(IntPtr ctx, int blendMode);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileBrush_SetRandomization(IntPtr ctx, int enabled, float strength);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileBrush_SetOpacity(IntPtr ctx, float opacity);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TileBrush_GetSettings(
            IntPtr ctx, out int outType, out int outSize, out int outBlendMode,
            out float outOpacity, out int outRandomEnabled, out float outRandomStrength);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TileBrush_LoadPattern(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string patternPath);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TileBrush_SavePattern(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string patternPath);

        #endregion

        #region Tile Painting API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilePaint_Begin(IntPtr ctx, EntityId tilemapLayer, int tileX, int tileY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilePaint_Continue(IntPtr ctx, EntityId tilemapLayer, int tileX, int tileY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilePaint_End(IntPtr ctx, EntityId tilemapLayer);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilePaint_PaintWithBrush(IntPtr ctx, EntityId tilemapLayer, int tileX, int tileY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePaint_GetPreview(
            IntPtr ctx, EntityId tilemapLayer, int tileX, int tileY,
            [Out] int[] outTilePositions, int maxPositions);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePaint_GetBrushPreview(
            IntPtr ctx, EntityId tilemapLayer, int tileX, int tileY,
            [Out] int[] outTilePositions, int maxPositions);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilePaint_PaintLine(
            IntPtr ctx, EntityId tilemapLayer, int startX, int startY, int endX, int endY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilePaint_PaintRectangle(
            IntPtr ctx, EntityId tilemapLayer, int minX, int minY, int maxX, int maxY, int filled);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilePaint_PaintCircle(
            IntPtr ctx, EntityId tilemapLayer, int centerX, int centerY, int radius, int filled);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilePaint_SampleTile(IntPtr ctx, EntityId tilemapLayer, int tileX, int tileY);

        #endregion

        #region Tile Layer Operations API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilemapLayer_GetAllInTilemap(
            IntPtr ctx, EntityId tilemap, [Out] uint[] outLayers, int maxCount);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilemapLayer_GetInfo(
            IntPtr ctx, EntityId layer,
            [Out] byte[] outName, int nameBufferSize,
            out int outVisible, out int outLocked, out float outOpacity, out int outSortOrder);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilemapLayer_SetVisible(IntPtr ctx, EntityId layer, int visible);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilemapLayer_SetLocked(IntPtr ctx, EntityId layer, int locked);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilemapLayer_SetOpacity(IntPtr ctx, EntityId layer, float opacity);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilemapLayer_SetSortOrder(IntPtr ctx, EntityId layer, int sortOrder);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TilemapLayer_Reorder(IntPtr ctx, EntityId layer, int newSortOrder);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TilemapLayer_GetPaintable(IntPtr ctx, [Out] uint[] outLayers, int maxCount);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileLayer_CopyRegion(
            IntPtr ctx, EntityId srcLayer, EntityId dstLayer,
            int srcMinX, int srcMinY, int srcMaxX, int srcMaxY, int dstX, int dstY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileLayer_CopyToClipboard(
            IntPtr ctx, EntityId layer, int minX, int minY, int maxX, int maxY);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileLayer_PasteFromClipboard(IntPtr ctx, EntityId layer, int x, int y);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileLayer_BlendLayers(
            IntPtr ctx, EntityId baseLayer, EntityId overlayLayer,
            int minX, int minY, int maxX, int maxY, float opacity);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileLayer_SetPalette(IntPtr ctx, EntityId tilemapLayer, int paletteId);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TileLayer_GetPalette(IntPtr ctx, EntityId tilemapLayer);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileLayer_RefreshDefinitions(IntPtr ctx, EntityId tilemapLayer);

        #endregion

        #region Auto-tiling API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AutoTile_CreateRuleSet(
            IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AutoTile_AddRule(
            IntPtr ctx, int ruleSetId, [In] int[] neighbors, int resultTileId, int priority);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AutoTile_SetEnabled(IntPtr ctx, int ruleSetId, int enabled);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AutoTile_ApplyToRegion(
            IntPtr ctx, EntityId tilemapLayer, int minX, int minY, int maxX, int maxY);

        #endregion

        #region Tile Definition API

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileDef_Register(
            IntPtr ctx, int tileId,
            [MarshalAs(UnmanagedType.LPStr)] string atlasName,
            [MarshalAs(UnmanagedType.LPStr)] string frameName,
            int walkable, int collisionType);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileDef_SetDefault(
            IntPtr ctx,
            [MarshalAs(UnmanagedType.LPStr)] string atlasName,
            [MarshalAs(UnmanagedType.LPStr)] string frameName);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TileDef_GetCount(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileDef_Clear(IntPtr ctx);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileDef_RegisterTile(
            IntPtr ctx, int tileId,
            [MarshalAs(UnmanagedType.LPStr)] string atlasName,
            [MarshalAs(UnmanagedType.LPStr)] string frameName,
            int walkable, int collisionType);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TileDef_GetTileInfo(
            IntPtr ctx, int tileId,
            [Out] byte[] outAtlasName, int atlasNameSize,
            [Out] byte[] outFrameName, int frameNameSize,
            out int outWalkable, out int outCollisionType);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TileDef_LoadFromPalette(IntPtr ctx, int paletteId);

        #endregion
    }
}