using ReactiveUI;
using System;
using WanderSpire.Scripting;

namespace SceneEditor.Models
{
    /// <summary>
    /// Represents a tilemap layer
    /// </summary>
    public class TilemapLayer : ReactiveObject
    {
        private string _name = string.Empty;
        private bool _isVisible = true;
        private bool _isLocked = false;
        private float _opacity = 1.0f;
        private int _sortOrder = 0;

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public EntityId EntityId { get; set; }

        public bool IsVisible
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public bool IsLocked
        {
            get => _isLocked;
            set => this.RaiseAndSetIfChanged(ref _isLocked, value);
        }

        public float Opacity
        {
            get => _opacity;
            set => this.RaiseAndSetIfChanged(ref _opacity, Math.Clamp(value, 0f, 1f));
        }

        public int SortOrder
        {
            get => _sortOrder;
            set => this.RaiseAndSetIfChanged(ref _sortOrder, value);
        }
    }
}