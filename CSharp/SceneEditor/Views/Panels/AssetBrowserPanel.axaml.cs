using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SceneEditor.Models;
using SceneEditor.ViewModels;
using System;

namespace SceneEditor.Views.Panels
{
    public partial class AssetBrowserPanel : UserControl
    {
        private DateTime _lastClickTime = DateTime.MinValue;
        private AssetItem? _lastClickedAsset = null;
        private const double DoubleClickThreshold = 500; // milliseconds

        public AssetBrowserPanel()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            // Pointer
            this.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            this.AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);

            // Drag-and-drop
            this.AddHandler(DragDrop.DropEvent, OnDrop, RoutingStrategies.Bubble);
            this.AddHandler(DragDrop.DragOverEvent, OnDragOver, RoutingStrategies.Bubble);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is AssetBrowserViewModel vm)
                vm.Initialize();
        }

        #region Pointer handling
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var hitAsset = GetAssetFromPointerEvent(e);

            if (hitAsset != null && DataContext is AssetBrowserViewModel vm)
            {
                // single-click = selection
                vm.SelectedAsset = hitAsset;

                // double-click test
                var now = DateTime.Now;
                var delta = (now - _lastClickTime).TotalMilliseconds;

                if (_lastClickedAsset == hitAsset && delta < DoubleClickThreshold)
                {
                    HandleDoubleClick(hitAsset, vm);
                    e.Handled = true;
                    _lastClickTime = DateTime.MinValue;
                    _lastClickedAsset = null;
                }
                else
                {
                    _lastClickTime = now;
                    _lastClickedAsset = hitAsset;
                }
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) { }
        #endregion

        #region Drag-and-drop
        private void OnDragOver(object? sender, DragEventArgs e)
        {
            var dragged = e.Data.Get("AssetItem") as AssetItem;
            var target = GetAssetFromDragEvent(e);

            e.DragEffects = (dragged != null && CanDropAsset(dragged, target))
                          ? DragDropEffects.Move
                          : DragDropEffects.None;
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            if (DataContext is not AssetBrowserViewModel vm)
                return;

            var dragged = e.Data.Get("AssetItem") as AssetItem;
            var target = GetAssetFromDragEvent(e);

            if (dragged != null)
                _ = vm.HandleAssetDropAsync(dragged, target);
        }
        #endregion

        #region Helpers
        private static AssetItem? GetAssetFromPointerEvent(PointerEventArgs e)
        {
            for (var src = e.Source as Control; src != null; src = src.Parent as Control)
            {
                if (src.DataContext is AssetItem a)
                    return a;
            }
            return null;
        }

        private static AssetItem? GetAssetFromDragEvent(DragEventArgs e)
        {
            for (var tgt = e.Source as Control; tgt != null; tgt = tgt.Parent as Control)
            {
                if (tgt.DataContext is AssetItem a)
                    return a;
            }
            return null;
        }

        private static bool CanDropAsset(AssetItem dragged, AssetItem? target)
        {
            if (dragged == target) return false;
            if (target is { Type: not AssetType.Folder }) return false;

            // prevent folder-in-its-own-child
            if (dragged.Type == AssetType.Folder && target != null)
            {
                for (var cur = target.Parent; cur != null; cur = cur.Parent)
                    if (cur == dragged) return false;
            }
            return true;
        }

        private static void HandleDoubleClick(AssetItem asset, AssetBrowserViewModel vm)
        {
            try
            {
                if (asset.Type == AssetType.Folder)
                    asset.IsExpanded = !asset.IsExpanded;
                else
                    _ = vm.OpenAssetCommand.Execute(asset);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AssetBrowserPanel] double-click: {ex}");
            }
        }
        #endregion

        #region Keyboard
        protected override void OnKeyDown(KeyEventArgs e)
        {
            HandleViewModeShortcuts(e);                       // Ctrl + 1/2/3

            if (e.Handled || DataContext is not AssetBrowserViewModel vm)
            {
                if (!e.Handled) base.OnKeyDown(e);
                return;
            }

            try
            {
                switch (e.Key)
                {
                    case Key.F5:
                        _ = vm.RefreshCommand.Execute();
                        e.Handled = true;
                        break;

                    case Key.Delete when vm.SelectedAsset != null:
                        _ = vm.DeleteAssetCommand.Execute(vm.SelectedAsset);
                        e.Handled = true;
                        break;

                    case Key.F2 when vm.SelectedAsset != null:
                        _ = vm.RenameAssetCommand.Execute(vm.SelectedAsset);
                        e.Handled = true;
                        break;

                    case Key.Enter when vm.SelectedAsset != null:
                        _ = vm.OpenAssetCommand.Execute(vm.SelectedAsset);
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AssetBrowserPanel] key-press: {ex}");
            }

            if (!e.Handled)
                base.OnKeyDown(e);
        }

        private void HandleViewModeShortcuts(KeyEventArgs e)
        {
            if (DataContext is not AssetBrowserViewModel vm) return;
            if (e.KeyModifiers != KeyModifiers.Control) return;

            switch (e.Key)
            {
                case Key.D1: vm.SetViewModeCommand.Execute("Tree"); e.Handled = true; break;
                case Key.D2: vm.SetViewModeCommand.Execute("Grid"); e.Handled = true; break;
                case Key.D3: vm.SetViewModeCommand.Execute("List"); e.Handled = true; break;
            }
        }
        #endregion

        #region Search box focus / cleanup
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (this.FindControl<TextBox>("SearchTextBox") is { } search)
                search.KeyDown += OnSearchBoxKeyDown;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (this.FindControl<TextBox>("SearchTextBox") is { } search)
                search.KeyDown -= OnSearchBoxKeyDown;
        }

        private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape ||
                sender is not TextBox tb ||
                DataContext is not AssetBrowserViewModel vm) return;

            vm.SearchText = string.Empty;
            tb.Clear();
            e.Handled = true;
        }
        #endregion
    }
}
