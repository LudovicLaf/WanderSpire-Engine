using Avalonia.Data.Converters;

namespace SceneEditor.Converters
{
    public static class ObjectConverters
    {
        public static readonly IValueConverter IsNull = new FuncValueConverter<object?, bool>(x => x is null);
        public static readonly IValueConverter IsNotNull = new FuncValueConverter<object?, bool>(x => x is not null);
    }
}