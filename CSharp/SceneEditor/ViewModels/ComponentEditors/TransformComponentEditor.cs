using ReactiveUI;
using System;
using System.Linq;
using System.Text.Json;
using WanderSpire.Scripting;

namespace SceneEditor.ViewModels.ComponentEditors;

/// <summary>
/// Component editor for TransformComponent
/// </summary>
public class TransformComponentEditor : ComponentEditorViewModel
{
    private float _positionX;
    private float _positionY;
    private float _rotation;
    private float _scaleX = 1f;
    private float _scaleY = 1f;
    private float _pivotX = 0.5f;
    private float _pivotY = 0.5f;
    private bool _lockX;
    private bool _lockY;
    private bool _lockRotation;
    private bool _lockScaleX;
    private bool _lockScaleY;
    private bool _freezeTransform;

    public override string DisplayName => "Transform";
    public override string Description => "Controls the position, rotation, and scale of the entity";

    public float PositionX
    {
        get => _positionX;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _positionX, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public float PositionY
    {
        get => _positionY;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _positionY, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public float Rotation
    {
        get => _rotation;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _rotation, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public float RotationDegrees
    {
        get => _rotation * 180f / MathF.PI;
        set
        {
            var radians = value * MathF.PI / 180f;
            if (Math.Abs(_rotation - radians) > 0.001f)
            {
                _rotation = radians;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(Rotation));
                SaveToEntity();
            }
        }
    }

    public float ScaleX
    {
        get => _scaleX;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _scaleX, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public float ScaleY
    {
        get => _scaleY;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _scaleY, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public float PivotX
    {
        get => _pivotX;
        set
        {
            var newValue = Math.Clamp(value, 0f, 1f);
            var oldValue = this.RaiseAndSetIfChanged(ref _pivotX, newValue);
            if (oldValue != newValue)
                SaveToEntity();
        }
    }

    public float PivotY
    {
        get => _pivotY;
        set
        {
            var newValue = Math.Clamp(value, 0f, 1f);
            var oldValue = this.RaiseAndSetIfChanged(ref _pivotY, newValue);
            if (oldValue != newValue)
                SaveToEntity();
        }
    }

    public bool LockX
    {
        get => _lockX;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _lockX, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public bool LockY
    {
        get => _lockY;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _lockY, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public bool LockRotation
    {
        get => _lockRotation;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _lockRotation, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public bool LockScaleX
    {
        get => _lockScaleX;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _lockScaleX, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public bool LockScaleY
    {
        get => _lockScaleY;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _lockScaleY, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public bool FreezeTransform
    {
        get => _freezeTransform;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _freezeTransform, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public TransformComponentEditor(Entity entity) : base("TransformComponent", entity)
    {
    }

    protected override void LoadFromJson(string json)
    {
        var element = ParseJson(json);
        if (element.ValueKind == JsonValueKind.Undefined)
            return;

        // Load position
        if (element.TryGetProperty("localPosition", out var positionProp) &&
            positionProp.ValueKind == JsonValueKind.Array)
        {
            var posArray = positionProp.EnumerateArray().ToArray();
            if (posArray.Length >= 2)
            {
                _positionX = posArray[0].GetSingle();
                _positionY = posArray[1].GetSingle();
            }
        }

        // Load rotation
        _rotation = GetJsonProperty(element, "localRotation", 0f);

        // Load scale
        if (element.TryGetProperty("localScale", out var scaleProp) &&
            scaleProp.ValueKind == JsonValueKind.Array)
        {
            var scaleArray = scaleProp.EnumerateArray().ToArray();
            if (scaleArray.Length >= 2)
            {
                _scaleX = scaleArray[0].GetSingle();
                _scaleY = scaleArray[1].GetSingle();
            }
        }

        // Load pivot
        if (element.TryGetProperty("pivot", out var pivotProp) &&
            pivotProp.ValueKind == JsonValueKind.Array)
        {
            var pivotArray = pivotProp.EnumerateArray().ToArray();
            if (pivotArray.Length >= 2)
            {
                _pivotX = pivotArray[0].GetSingle();
                _pivotY = pivotArray[1].GetSingle();
            }
        }

        // Load lock flags
        _lockX = GetJsonProperty(element, "lockX", false);
        _lockY = GetJsonProperty(element, "lockY", false);
        _lockRotation = GetJsonProperty(element, "lockRotation", false);
        _lockScaleX = GetJsonProperty(element, "lockScaleX", false);
        _lockScaleY = GetJsonProperty(element, "lockScaleY", false);
        _freezeTransform = GetJsonProperty(element, "freezeTransform", false);

        // Notify property changes
        this.RaisePropertyChanged(nameof(PositionX));
        this.RaisePropertyChanged(nameof(PositionY));
        this.RaisePropertyChanged(nameof(Rotation));
        this.RaisePropertyChanged(nameof(RotationDegrees));
        this.RaisePropertyChanged(nameof(ScaleX));
        this.RaisePropertyChanged(nameof(ScaleY));
        this.RaisePropertyChanged(nameof(PivotX));
        this.RaisePropertyChanged(nameof(PivotY));
        this.RaisePropertyChanged(nameof(LockX));
        this.RaisePropertyChanged(nameof(LockY));
        this.RaisePropertyChanged(nameof(LockRotation));
        this.RaisePropertyChanged(nameof(LockScaleX));
        this.RaisePropertyChanged(nameof(LockScaleY));
        this.RaisePropertyChanged(nameof(FreezeTransform));
    }

    protected override string SaveToJson()
    {
        return JsonSerializer.Serialize(new
        {
            localPosition = new[] { _positionX, _positionY },
            localRotation = _rotation,
            localScale = new[] { _scaleX, _scaleY },
            worldPosition = new[] { _positionX, _positionY }, // Simplified - would be calculated
            worldRotation = _rotation,
            worldScale = new[] { _scaleX, _scaleY },
            isDirty = true,
            freezeTransform = _freezeTransform,
            pivot = new[] { _pivotX, _pivotY },
            lockX = _lockX,
            lockY = _lockY,
            lockRotation = _lockRotation,
            lockScaleX = _lockScaleX,
            lockScaleY = _lockScaleY
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}

/// <summary>
/// Component editor for SpriteComponent
/// </summary>
public class SpriteComponentEditor : ComponentEditorViewModel
{
    private string _atlasName = string.Empty;
    private string _frameName = string.Empty;

    public override string DisplayName => "Sprite";
    public override string Description => "Controls the sprite texture and frame";

    public string AtlasName
    {
        get => _atlasName;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _atlasName, value ?? string.Empty);
            if (oldValue != (value ?? string.Empty))
                SaveToEntity();
        }
    }

    public string FrameName
    {
        get => _frameName;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _frameName, value ?? string.Empty);
            if (oldValue != (value ?? string.Empty))
                SaveToEntity();
        }
    }

    public SpriteComponentEditor(Entity entity) : base("SpriteComponent", entity)
    {
    }

    protected override void LoadFromJson(string json)
    {
        var element = ParseJson(json);
        if (element.ValueKind == JsonValueKind.Undefined)
            return;

        _atlasName = GetJsonProperty(element, "atlasName", string.Empty);
        _frameName = GetJsonProperty(element, "frameName", string.Empty);

        this.RaisePropertyChanged(nameof(AtlasName));
        this.RaisePropertyChanged(nameof(FrameName));
    }

    protected override string SaveToJson()
    {
        return JsonSerializer.Serialize(new
        {
            atlasName = _atlasName,
            frameName = _frameName
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}

/// <summary>
/// Component editor for TagComponent
/// </summary>
public class TagComponentEditor : ComponentEditorViewModel
{
    private string _tag = string.Empty;

    public override string DisplayName => "Tag";
    public override string Description => "Entity name and identification";

    public string Tag
    {
        get => _tag;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _tag, value ?? string.Empty);
            if (oldValue != (value ?? string.Empty))
                SaveToEntity();
        }
    }

    public TagComponentEditor(Entity entity) : base("TagComponent", entity)
    {
    }

    protected override void LoadFromJson(string json)
    {
        var element = ParseJson(json);
        if (element.ValueKind == JsonValueKind.Undefined)
            return;

        _tag = GetJsonProperty(element, "tag", string.Empty);
        this.RaisePropertyChanged(nameof(Tag));
    }

    protected override string SaveToJson()
    {
        return JsonSerializer.Serialize(new
        {
            tag = _tag
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}

/// <summary>
/// Component editor for GridPositionComponent
/// </summary>
public class GridPositionComponentEditor : ComponentEditorViewModel
{
    private int _tileX;
    private int _tileY;

    public override string DisplayName => "Grid Position";
    public override string Description => "Position on the tile grid";

    public int TileX
    {
        get => _tileX;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _tileX, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public int TileY
    {
        get => _tileY;
        set
        {
            var oldValue = this.RaiseAndSetIfChanged(ref _tileY, value);
            if (oldValue != value)
                SaveToEntity();
        }
    }

    public GridPositionComponentEditor(Entity entity) : base("GridPositionComponent", entity)
    {
    }

    protected override void LoadFromJson(string json)
    {
        var element = ParseJson(json);
        if (element.ValueKind == JsonValueKind.Undefined)
            return;

        // Try to load from tile array first
        if (element.TryGetProperty("tile", out var tileProp) &&
            tileProp.ValueKind == JsonValueKind.Array)
        {
            var tileArray = tileProp.EnumerateArray().ToArray();
            if (tileArray.Length >= 2)
            {
                _tileX = tileArray[0].GetInt32();
                _tileY = tileArray[1].GetInt32();
            }
        }
        // Try to load from tileObj
        else if (element.TryGetProperty("tileObj", out var tileObjProp))
        {
            _tileX = GetJsonProperty(tileObjProp, "x", 0);
            _tileY = GetJsonProperty(tileObjProp, "y", 0);
        }

        this.RaisePropertyChanged(nameof(TileX));
        this.RaisePropertyChanged(nameof(TileY));
    }

    protected override string SaveToJson()
    {
        return JsonSerializer.Serialize(new
        {
            tile = new[] { _tileX, _tileY },
            tileObj = new { x = _tileX, y = _tileY }
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}

// Placeholder editors for other components
public class SpriteAnimationComponentEditor : GenericComponentEditor
{
    public override string DisplayName => "Sprite Animation";
    public SpriteAnimationComponentEditor(Entity entity) : base("SpriteAnimationComponent", entity) { }
}

public class PlayerTagComponentEditor : GenericComponentEditor
{
    public override string DisplayName => "Player Tag";
    public PlayerTagComponentEditor(Entity entity) : base("PlayerTagComponent", entity) { }
}

public class ObstacleComponentEditor : GenericComponentEditor
{
    public override string DisplayName => "Obstacle";
    public ObstacleComponentEditor(Entity entity) : base("ObstacleComponent", entity) { }
}

public class FacingComponentEditor : GenericComponentEditor
{
    public override string DisplayName => "Facing";
    public FacingComponentEditor(Entity entity) : base("FacingComponent", entity) { }
}

public class AnimationStateComponentEditor : GenericComponentEditor
{
    public override string DisplayName => "Animation State";
    public AnimationStateComponentEditor(Entity entity) : base("AnimationStateComponent", entity) { }
}
