using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.ReactiveUI;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;


namespace SceneEditor.Views.Panels;

/// <summary>
/// Modern behavior class for drag and drop operations
/// </summary>
public class DragDropBehavior
{
    private bool _isDragging;
    private Point _startPoint;

    public static readonly AttachedProperty<bool> EnableDragDropProperty =
        AvaloniaProperty.RegisterAttached<DragDropBehavior, Control, bool>("EnableDragDrop");

    public static void SetEnableDragDrop(Control element, bool value)
    {
        element.SetValue(EnableDragDropProperty, value);
        if (value)
        {
            AttachDragDropHandlers(element);
        }
    }

    public static bool GetEnableDragDrop(Control element)
    {
        return element.GetValue(EnableDragDropProperty);
    }

    private static void AttachDragDropHandlers(Control element)
    {
        var behavior = new DragDropBehavior();

        element.PointerPressed += behavior.OnPointerPressed;
        element.PointerMoved += behavior.OnPointerMoved;
        element.PointerReleased += behavior.OnPointerReleased;
        element.AddHandler(DragDrop.DragOverEvent, behavior.OnDragOver, RoutingStrategies.Bubble);
        element.AddHandler(DragDrop.DropEvent, behavior.OnDrop, RoutingStrategies.Bubble);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(sender as Control).Properties.IsLeftButtonPressed)
        {
            _startPoint = e.GetPosition(sender as Control);
        }
    }

    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging && e.GetCurrentPoint(sender as Control).Properties.IsLeftButtonPressed)
        {
            var currentPoint = e.GetPosition(sender as Control);
            var distance = Math.Abs(currentPoint.X - _startPoint.X) + Math.Abs(currentPoint.Y - _startPoint.Y);

            if (distance > 10) // Start drag threshold
            {
                _isDragging = true;
                var control = sender as Control;
                var dataObject = new DataObject();
                dataObject.Set("sourceControl", control);

                await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Move);
            }
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Move;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        // Handle drop logic here
        if (e.Data.Get("sourceControl") is Control sourceControl)
        {
            // Implement hierarchy rearrangement logic
        }
    }
}

/// <summary>
/// Modern search behavior with debouncing
/// </summary>
public class SearchBehavior
{
    private static readonly AttachedProperty<string> SearchTextProperty =
        AvaloniaProperty.RegisterAttached<SearchBehavior, TextBox, string>("SearchText");

    private static readonly AttachedProperty<IDisposable> SubscriptionProperty =
        AvaloniaProperty.RegisterAttached<SearchBehavior, TextBox, IDisposable>("Subscription");

    public static void SetSearchText(TextBox element, string value)
    {
        element.SetValue(SearchTextProperty, value);
    }

    public static string GetSearchText(TextBox element)
    {
        return element.GetValue(SearchTextProperty);
    }

    static SearchBehavior()
    {
        SearchTextProperty.Changed.Subscribe(OnSearchTextChanged);
    }

    private static void OnSearchTextChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Sender is TextBox textBox)
        {
            var oldSubscription = textBox.GetValue(SubscriptionProperty);
            oldSubscription?.Dispose();

            var subscription = Observable.FromEventPattern<TextChangedEventArgs>(
                    h => textBox.TextChanged += h,
                    h => textBox.TextChanged -= h)
                .Select(_ => textBox.Text)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(text =>
                {
                    if (textBox.DataContext is ISearchable searchable)
                    {
                        searchable.ApplyFilter(text);
                    }
                });

            textBox.SetValue(SubscriptionProperty, subscription);
        }
    }
}

/// <summary>
/// Interface for searchable view models
/// </summary>
public interface ISearchable
{
    void ApplyFilter(string? filterText);
}

/// <summary>
/// Modern context menu behavior
/// </summary>
public class ContextMenuBehavior
{
    public static readonly AttachedProperty<bool> EnableContextMenuProperty =
        AvaloniaProperty.RegisterAttached<ContextMenuBehavior, Control, bool>("EnableContextMenu");

    public static void SetEnableContextMenu(Control element, bool value)
    {
        element.SetValue(EnableContextMenuProperty, value);
        if (value)
        {
            element.ContextRequested += OnContextRequested;
        }
    }

    public static bool GetEnableContextMenu(Control element)
    {
        return element.GetValue(EnableContextMenuProperty);
    }

    private static void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is Control control && control.ContextFlyout is MenuFlyout menu)
        {
            // Ensure the item is selected before showing context menu
            if (control.DataContext != null)
            {
                var parent = control.GetValue(Visual.VisualParentProperty);
                while (parent != null)
                {
                    if (parent is TreeView treeView)
                    {
                        treeView.SelectedItem = control.DataContext;
                        break;
                    }
                    if (parent is ListBox listBox)
                    {
                        listBox.SelectedItem = control.DataContext;
                        break;
                    }
                    parent = parent.GetValue(Visual.VisualParentProperty);
                }
            }
        }
    }
}

/// <summary>
/// Modern loading state behavior
/// </summary>
public class LoadingStateBehavior
{
    public static readonly AttachedProperty<bool> IsLoadingProperty =
        AvaloniaProperty.RegisterAttached<LoadingStateBehavior, Control, bool>("IsLoading");

    public static readonly AttachedProperty<string> LoadingTextProperty =
        AvaloniaProperty.RegisterAttached<LoadingStateBehavior, Control, string>("LoadingText", "Loading...");

    public static void SetIsLoading(Control element, bool value)
    {
        element.SetValue(IsLoadingProperty, value);
        UpdateLoadingState(element, value);
    }

    public static bool GetIsLoading(Control element)
    {
        return element.GetValue(IsLoadingProperty);
    }

    public static void SetLoadingText(Control element, string value)
    {
        element.SetValue(LoadingTextProperty, value);
    }

    public static string GetLoadingText(Control element)
    {
        return element.GetValue(LoadingTextProperty);
    }

    private static void UpdateLoadingState(Control element, bool isLoading)
    {
        if (isLoading)
        {
            var loadingOverlay = new Border
            {
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Black, 0.5),
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new ProgressBar { IsIndeterminate = true, Width = 200 },
                        new TextBlock
                        {
                            Text = GetLoadingText(element),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 10, 0, 0)
                        }
                    }
                }
            };

            // Add loading overlay (implementation depends on container type)
        }
    }
}

/// <summary>
/// Modern validation behavior
/// </summary>
public class ValidationBehavior
{
    public static readonly AttachedProperty<bool> EnableValidationProperty =
        AvaloniaProperty.RegisterAttached<ValidationBehavior, TextBox, bool>("EnableValidation");

    public static readonly AttachedProperty<Func<string?, string?>> ValidatorProperty =
        AvaloniaProperty.RegisterAttached<ValidationBehavior, TextBox, Func<string?, string?>>("Validator");

    public static void SetEnableValidation(TextBox element, bool value)
    {
        element.SetValue(EnableValidationProperty, value);
        if (value)
        {
            element.TextChanged += OnTextChanged;
            element.LostFocus += OnLostFocus;
        }
    }

    public static bool GetEnableValidation(TextBox element)
    {
        return element.GetValue(EnableValidationProperty);
    }

    public static void SetValidator(TextBox element, Func<string?, string?> value)
    {
        element.SetValue(ValidatorProperty, value);
    }

    public static Func<string?, string?> GetValidator(TextBox element)
    {
        return element.GetValue(ValidatorProperty);
    }

    private static void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            ValidateTextBox(textBox);
        }
    }

    private static void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            ValidateTextBox(textBox);
        }
    }

    private static void ValidateTextBox(TextBox textBox)
    {
        var validator = GetValidator(textBox);
        if (validator != null)
        {
            var error = validator(textBox.Text);
            if (!string.IsNullOrEmpty(error))
            {
                textBox.Classes.Add("error");
                ToolTip.SetTip(textBox, error);
            }
            else
            {
                textBox.Classes.Remove("error");
                ToolTip.SetTip(textBox, null);
            }
        }
    }
}

/// <summary>
/// Example usage in a ViewModel base class
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged, ISearchable
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public abstract void ApplyFilter(string? filterText);
}

/// <summary>
/// Modern async command implementation
/// </summary>
public class AsyncCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    public AsyncCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute(parameter);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}